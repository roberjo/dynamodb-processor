using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;

namespace DynamoDBProcessor.Tests.Performance;

public class QueryPerformanceTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IMetricsService> _mockMetrics;
    private readonly Mock<ILogger<QueryExecutor>> _mockLogger;
    private readonly QueryBuilder _queryBuilder;
    private readonly QueryExecutor _queryExecutor;

    public QueryPerformanceTests()
    {
        _mockDynamoDb = new Mock<IAmazonDynamoDB>();
        _mockCache = new Mock<IMemoryCache>();
        _mockMetrics = new Mock<IMetricsService>();
        _mockLogger = new Mock<ILogger<QueryExecutor>>();
        _queryBuilder = new QueryBuilder();
        
        // Setup cache mock to handle CreateEntry method (which Set extension method uses)
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());
        
        _queryExecutor = new QueryExecutor(_mockDynamoDb.Object, _mockCache.Object, _mockMetrics.Object, _mockLogger.Object, _queryBuilder);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task ExecuteQueryWithPaginationAsync_WithLargeDataset_CompletesWithinTimeLimit(int totalItems)
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var pageSize = 100;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var responses = new List<QueryResponse>();

        for (int i = 0; i < totalPages; i++)
        {
            var items = new List<Dictionary<string, AttributeValue>>();
            var itemsInPage = Math.Min(pageSize, totalItems - (i * pageSize));

            for (int j = 0; j < itemsInPage; j++)
            {
                items.Add(new Dictionary<string, AttributeValue>
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = $"data-{i * pageSize + j}" } }
                });
            }

            var response = new QueryResponse
            {
                Items = items,
                LastEvaluatedKey = i < totalPages - 1
                    ? new Dictionary<string, AttributeValue>
                    {
                        { "userId", new AttributeValue { S = "test-user" } },
                        { "timestamp", new AttributeValue { N = (i * pageSize).ToString() } }
                    }
                    : null
            };

            responses.Add(response);
        }

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(responses[0])
            .ReturnsAsync(responses[1])
            .ReturnsAsync(responses[2])
            .ReturnsAsync(responses[3])
            .ReturnsAsync(responses[4]);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _queryExecutor.ExecuteQueryWithPaginationAsync(request, totalItems);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(totalItems);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithCache_IsFasterThanWithoutCache()
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

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        object cachedValue = new DynamoPaginatedQueryResponse
        {
            Items = expectedResponse.Items,
            HasMoreResults = false
        };

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(true);

        // Act - Without cache
        var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
        await _queryExecutor.ExecuteQueryAsync(request);
        stopwatch1.Stop();

        // Act - With cache
        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        await _queryExecutor.ExecuteQueryAsync(request);
        stopwatch2.Stop();

        // Assert
        stopwatch2.ElapsedMilliseconds.Should().BeLessThan(stopwatch1.ElapsedMilliseconds);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithConcurrentRequests_HandlesLoad()
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

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var tasks = new List<Task>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_queryExecutor.ExecuteQueryAsync(request));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // 10 seconds max
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default), Times.Exactly(100));
    }
} 