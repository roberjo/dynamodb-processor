using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;

namespace DynamoDBProcessor.Tests.Services;

public class QueryExecutorTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IMetricsService> _mockMetrics;
    private readonly Mock<ILogger<QueryExecutor>> _mockLogger;
    private readonly QueryBuilder _queryBuilder;
    private readonly QueryExecutor _queryExecutor;

    public QueryExecutorTests()
    {
        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
        _mockCache = new Mock<IMemoryCache>();
        _mockMetrics = new Mock<IMetricsService>();
        _mockLogger = new Mock<ILogger<QueryExecutor>>();
        _queryBuilder = new QueryBuilder();
        
        _queryExecutor = new QueryExecutor(
            _mockDynamoDb.Object,
            _mockCache.Object,
            _mockMetrics.Object,
            _mockLogger.Object,
            _queryBuilder);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithValidRequest_ReturnsPaginatedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
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

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeFalse();
        result.LastEvaluatedKey.Should().BeNull();
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithPagination_ReturnsPaginatedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
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

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request, lastEvaluatedKey);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeTrue();
        result.LastEvaluatedKey.Should().BeEquivalentTo(lastEvaluatedKey);
        result.TotalItems.Should().Be(1);
        result.ContinuationToken.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithCache_ReturnsCachedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var cachedResponse = new DynamoPaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "cached-data" } }
                }
            },
            HasMoreResults = false,
            TotalItems = 1
        };

        object cachedValue = cachedResponse;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(true);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().BeEquivalentTo(cachedResponse);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default), Times.Never);
        _mockMetrics.Verify(x => x.RecordCountAsync("CacheHit", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithThrottling_RetriesAndSucceeds()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
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

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ThrowsAsync(new ProvisionedThroughputExceededException("Throttling"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _queryExecutor.ExecuteQueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default), Times.Exactly(2));
        _mockMetrics.Verify(x => x.RecordCountAsync("QueryThrottledRetry", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryWithPaginationAsync_WithMaxItems_StopsAtLimit()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
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

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(response1)
            .ReturnsAsync(response2);

        // Act
        var result = await _queryExecutor.ExecuteQueryWithPaginationAsync(request, maxItems: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.HasMoreResults.Should().BeTrue();
        result.TotalItems.Should().Be(2);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default), Times.Once);
    }
} 