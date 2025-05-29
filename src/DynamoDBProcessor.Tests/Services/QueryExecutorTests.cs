using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Services;

public class QueryExecutorTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IMetricsService> _mockMetrics;
    private readonly Mock<ILogger<QueryExecutor>> _mockLogger;
    private readonly QueryExecutor _queryExecutor;

    public QueryExecutorTests()
    {
        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
        _mockCache = new Mock<IMemoryCache>();
        _mockMetrics = new Mock<IMetricsService>();
        _mockLogger = new Mock<ILogger<QueryExecutor>>();
        _queryExecutor = new QueryExecutor(_mockDynamoDb.Object, _mockCache.Object, _mockMetrics.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithValidRequest_ReturnsPaginatedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            TableName = "TestTable",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = "test-user" } }
            }
        };

        var expectedResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            LastEvaluatedKey = null
        };

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeFalse();
        result.LastEvaluatedKey.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithPagination_ReturnsPaginatedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            TableName = "TestTable",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = "test-user" } }
            }
        };

        var lastEvaluatedKey = new Dictionary<string, AttributeValue>
        {
            { "userId", new AttributeValue { S = "test-user" } },
            { "timestamp", new AttributeValue { N = "1234567890" } }
        };

        var expectedResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            LastEvaluatedKey = lastEvaluatedKey
        };

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request, lastEvaluatedKey);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeTrue();
        result.LastEvaluatedKey.Should().BeEquivalentTo(lastEvaluatedKey);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithCache_ReturnsCachedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            TableName = "TestTable",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = "test-user" } }
            }
        };

        var cachedResponse = new PaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "cached-data" } }
                }
            },
            HasMoreResults = false
        };

        object cachedValue = cachedResponse;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(true);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().BeEquivalentTo(cachedResponse);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<QueryRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithThrottling_RetriesAndSucceeds()
    {
        // Arrange
        var request = new QueryRequest
        {
            TableName = "TestTable",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = "test-user" } }
            }
        };

        var expectedResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            }
        };

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ThrowsAsync(new ProvisionedThroughputExceededException("Throttling"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<QueryRequest>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteQueryWithPaginationAsync_WithMaxItems_StopsAtLimit()
    {
        // Arrange
        var request = new QueryRequest
        {
            TableName = "TestTable",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = "test-user" } }
            }
        };

        var lastEvaluatedKey = new Dictionary<string, AttributeValue>
        {
            { "userId", new AttributeValue { S = "test-user" } },
            { "timestamp", new AttributeValue { N = "1234567890" } }
        };

        var response1 = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "data", new AttributeValue { S = "data1" } } },
                new() { { "data", new AttributeValue { S = "data2" } } }
            },
            LastEvaluatedKey = lastEvaluatedKey
        };

        var response2 = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "data", new AttributeValue { S = "data3" } } }
            }
        };

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(response1)
            .ReturnsAsync(response2);

        // Act
        var result = await _queryExecutor.ExecuteQueryWithPaginationAsync(request, maxItems: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.HasMoreResults.Should().BeTrue();
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<QueryRequest>(), default), Times.Once);
    }
} 