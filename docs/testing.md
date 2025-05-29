# Testing Guide

## Overview

This document outlines the testing strategy for the DynamoDB Processor application, including unit testing, integration testing, and performance testing approaches.

## Testing Architecture

### 1. Unit Testing

1. **Test Project Structure**
   ```csharp
   public class DynamoDBServiceTests
   {
       private readonly Mock<IAmazonDynamoDB> _dynamoDBMock;
       private readonly Mock<IMetricsService> _metricsServiceMock;
       private readonly Mock<ILogger<DynamoDBService>> _loggerMock;
       private readonly DynamoDBService _service;

       public DynamoDBServiceTests()
       {
           _dynamoDBMock = new Mock<IAmazonDynamoDB>();
           _metricsServiceMock = new Mock<IMetricsService>();
           _loggerMock = new Mock<ILogger<DynamoDBService>>();

           _service = new DynamoDBService(
               _dynamoDBMock.Object,
               _metricsServiceMock.Object,
               _loggerMock.Object);
       }

       [Fact]
       public async Task QueryRecordsAsync_WhenCacheHit_ReturnsCachedResponse()
       {
           // Arrange
           var request = new QueryRequest
           {
               TableName = "test-table",
               FilterExpression = "userId = :userId"
           };

           var cachedResponse = new QueryResponse
           {
               Items = new List<Dictionary<string, AttributeValue>>
               {
                   new Dictionary<string, AttributeValue>
                   {
                       { "userId", new AttributeValue { S = "test-user" } }
                   }
               }
           };

           // Act
           var result = await _service.QueryRecordsAsync(request);

           // Assert
           Assert.Equal(cachedResponse.Items.Count, result.Items.Count);
           _metricsServiceMock.Verify(
               x => x.RecordCountAsync("CacheHit", 1, It.IsAny<Dictionary<string, string>>()),
               Times.Once);
       }
   }
   ```

2. **Test Categories**
   ```csharp
   [Trait("Category", "Unit")]
   public class UnitTests { }

   [Trait("Category", "Integration")]
   public class IntegrationTests { }

   [Trait("Category", "Performance")]
   public class PerformanceTests { }
   ```

### 2. Integration Testing

1. **Test Setup**
   ```csharp
   public class IntegrationTestBase : IAsyncLifetime
   {
       protected IAmazonDynamoDB DynamoDB { get; private set; }
       protected string TableName { get; private set; }

       public async Task InitializeAsync()
       {
           DynamoDB = new AmazonDynamoDBClient();
           TableName = $"test-table-{Guid.NewGuid()}";

           await CreateTestTable();
       }

       public async Task DisposeAsync()
       {
           await DeleteTestTable();
       }

       private async Task CreateTestTable()
       {
           var request = new CreateTableRequest
           {
               TableName = TableName,
               KeySchema = new List<KeySchemaElement>
               {
                   new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                   new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
               },
               AttributeDefinitions = new List<AttributeDefinition>
               {
                   new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                   new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S }
               },
               ProvisionedThroughput = new ProvisionedThroughput
               {
                   ReadCapacityUnits = 5,
                   WriteCapacityUnits = 5
               }
           };

           await DynamoDB.CreateTableAsync(request);
           await WaitForTableToBecomeActive();
       }
   }
   ```

2. **API Tests**
   ```csharp
   public class ApiTests : IntegrationTestBase
   {
       private readonly HttpClient _client;

       public ApiTests()
       {
           var factory = new WebApplicationFactory<Program>();
           _client = factory.CreateClient();
       }

       [Fact]
       public async Task Query_WithValidRequest_ReturnsSuccess()
       {
           // Arrange
           var request = new
           {
               userId = "test-user",
               startDate = DateTime.UtcNow.AddDays(-1),
               endDate = DateTime.UtcNow
           };

           // Act
           var response = await _client.PostAsJsonAsync("/api/query", request);

           // Assert
           response.EnsureSuccessStatusCode();
           var result = await response.Content.ReadFromJsonAsync<QueryResponse>();
           Assert.NotNull(result);
       }
   }
   ```

### 3. Performance Testing

1. **Load Tests**
   ```csharp
   public class LoadTests : IntegrationTestBase
   {
       [Theory]
       [InlineData(100)]
       [InlineData(1000)]
       [InlineData(10000)]
       public async Task Query_UnderLoad_CompletesWithinThreshold(int requestCount)
       {
           // Arrange
           var requests = Enumerable.Range(0, requestCount)
               .Select(i => new QueryRequest
               {
                   TableName = TableName,
                   FilterExpression = $"userId = :userId",
                   ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                   {
                       { ":userId", new AttributeValue { S = $"user-{i}" } }
                   }
               })
               .ToList();

           // Act
           var stopwatch = Stopwatch.StartNew();
           var tasks = requests.Select(r => _service.QueryRecordsAsync(r));
           await Task.WhenAll(tasks);
           stopwatch.Stop();

           // Assert
           var averageTime = stopwatch.ElapsedMilliseconds / requestCount;
           Assert.True(averageTime < 100, $"Average query time {averageTime}ms exceeds threshold");
       }
   }
   ```

2. **Stress Tests**
   ```csharp
   public class StressTests : IntegrationTestBase
   {
       [Fact]
       public async Task Query_UnderStress_HandlesErrorsGracefully()
       {
           // Arrange
           var concurrentRequests = 1000;
           var errorCount = 0;

           // Act
           var tasks = Enumerable.Range(0, concurrentRequests)
               .Select(async i =>
               {
                   try
                   {
                       await _service.QueryRecordsAsync(new QueryRequest
                       {
                           TableName = TableName,
                           FilterExpression = $"userId = :userId",
                           ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                           {
                               { ":userId", new AttributeValue { S = $"user-{i}" } }
                           }
                       });
                   }
                   catch
                   {
                       Interlocked.Increment(ref errorCount);
                   }
               });

           await Task.WhenAll(tasks);

           // Assert
           var errorRate = (double)errorCount / concurrentRequests;
           Assert.True(errorRate < 0.01, $"Error rate {errorRate:P} exceeds threshold");
       }
   }
   ```

## Test Categories

### 1. Unit Tests

1. **Service Tests**
   - DynamoDB service
   - Cache service
   - Metrics service
   - Logging service

2. **Middleware Tests**
   - Authentication
   - Rate limiting
   - Error handling
   - Request/response logging

3. **Validation Tests**
   - Request validation
   - Response validation
   - Error validation

### 2. Integration Tests

1. **API Tests**
   - Endpoint availability
   - Request/response flow
   - Error handling
   - Authentication

2. **Database Tests**
   - Table operations
   - Query operations
   - Index operations
   - Error handling

3. **Cache Tests**
   - Cache operations
   - Cache invalidation
   - Cache consistency

### 3. Performance Tests

1. **Load Tests**
   - Concurrent requests
   - Response times
   - Resource usage
   - Error rates

2. **Stress Tests**
   - Maximum capacity
   - Error handling
   - Recovery
   - Resource limits

## Test Execution

### 1. Local Execution

1. **Unit Tests**
   ```powershell
   # Run all unit tests
   dotnet test src/DynamoDBProcessor.Tests

   # Run specific test
   dotnet test src/DynamoDBProcessor.Tests --filter "FullyQualifiedName=DynamoDBProcessor.Tests.Services.DynamoDBServiceTests.QueryRecordsAsync_WhenCacheHit_ReturnsCachedResponse"
   ```

2. **Integration Tests**
   ```powershell
   # Run all integration tests
   dotnet test src/DynamoDBProcessor.IntegrationTests

   # Run with specific environment
   $env:DYNAMODB_TABLE_NAME = "test-table"
   dotnet test src/DynamoDBProcessor.IntegrationTests
   ```

### 2. CI/CD Execution

1. **GitHub Actions**
   ```yaml
   name: Tests

   on:
     push:
       branches: [ main, develop ]
     pull_request:
       branches: [ main, develop ]

   jobs:
     test:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v2
         - name: Setup .NET
           uses: actions/setup-dotnet@v1
           with:
             dotnet-version: 8.0.x
         - name: Restore dependencies
           run: dotnet restore
         - name: Build
           run: dotnet build --no-restore
         - name: Test
           run: dotnet test --no-build --verbosity normal
   ```

2. **Test Reports**
   ```powershell
   # Generate coverage report
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

   # Generate HTML report
   reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report
   ```

## Test Data

### 1. Test Data Generation

1. **Data Factory**
   ```csharp
   public class TestDataFactory
   {
       public static QueryRequest CreateQueryRequest(
           string userId = null,
           DateTime? startDate = null,
           DateTime? endDate = null)
       {
           return new QueryRequest
           {
               TableName = "test-table",
               FilterExpression = "userId = :userId",
               ExpressionAttributeValues = new Dictionary<string, AttributeValue>
               {
                   { ":userId", new AttributeValue { S = userId ?? "test-user" } }
               }
           };
       }
   }
   ```

2. **Data Cleanup**
   ```csharp
   public class TestDataCleanup
   {
       public static async Task CleanupTestData(
           IAmazonDynamoDB dynamoDB,
           string tableName)
       {
           var items = await GetAllItems(dynamoDB, tableName);
           foreach (var item in items)
           {
               await dynamoDB.DeleteItemAsync(new DeleteItemRequest
               {
                   TableName = tableName,
                   Key = new Dictionary<string, AttributeValue>
                   {
                       { "PK", item["PK"] },
                       { "SK", item["SK"] }
                   }
               });
           }
       }
   }
   ```

### 2. Test Data Management

1. **Data Seeding**
   ```csharp
   public class TestDataSeeder
   {
       public static async Task SeedTestData(
           IAmazonDynamoDB dynamoDB,
           string tableName,
           int itemCount)
       {
           var items = Enumerable.Range(0, itemCount)
               .Select(i => new Dictionary<string, AttributeValue>
               {
                   { "PK", new AttributeValue { S = $"USER#{i}" } },
                   { "SK", new AttributeValue { S = $"RECORD#{i}" } },
                   { "userId", new AttributeValue { S = $"user-{i}" } },
                   { "timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
               });

           foreach (var item in items)
           {
               await dynamoDB.PutItemAsync(new PutItemRequest
               {
                   TableName = tableName,
                   Item = item
               });
           }
       }
   }
   ```

2. **Data Verification**
   ```csharp
   public class TestDataVerifier
   {
       public static async Task VerifyTestData(
           IAmazonDynamoDB dynamoDB,
           string tableName,
           int expectedCount)
       {
           var items = await GetAllItems(dynamoDB, tableName);
           Assert.Equal(expectedCount, items.Count);
       }
   }
   ```

## Best Practices

### 1. Test Organization

1. **Test Structure**
   - Arrange-Act-Assert pattern
   - Clear test names
   - Single assertion focus
   - Proper test isolation

2. **Test Categories**
   - Unit tests
   - Integration tests
   - Performance tests
   - Security tests

### 2. Test Maintenance

1. **Code Coverage**
   - Maintain > 80% coverage
   - Focus on critical paths
   - Regular coverage reports
   - Coverage goals

2. **Test Documentation**
   - Clear test descriptions
   - Setup instructions
   - Test data requirements
   - Expected results

### 3. Test Performance

1. **Test Optimization**
   - Parallel test execution
   - Efficient test data
   - Proper cleanup
   - Resource management

2. **Test Reliability**
   - Stable test environment
   - Proper error handling
   - Retry mechanisms
   - Timeout handling

## Resources

### 1. Documentation

- [xUnit Documentation](https://xunit.net/)
- [.NET Testing](https://docs.microsoft.com/dotnet/core/testing/)
- [AWS Testing](https://aws.amazon.com/testing/)

### 2. Tools

- [xUnit](https://xunit.net/)
- [Moq](https://github.com/moq/moq4)
- [Coverlet](https://github.com/coverlet-coverage/coverlet)

### 3. Support

- [Stack Overflow](https://stackoverflow.com/)
- [GitHub Issues](https://github.com/yourusername/dynamodb-processor/issues)
- [AWS Support](https://aws.amazon.com/support/) 