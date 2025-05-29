using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Services;
using DynamoDBProcessor.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Services;

public class DynamoDBServiceTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDbClient;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<ILogger<DynamoDBService>> _mockLogger;
    private readonly DynamoDBService _service;
    private const string TableName = "TestTable";

    public DynamoDBServiceTests()
    {
        _mockDynamoDbClient = new Mock<IAmazonDynamoDB>();
        _mockCacheService = new Mock<ICacheService>();
        _mockMetricsService = new Mock<IMetricsService>();
        _mockLogger = new Mock<ILogger<DynamoDBService>>();

        _service = new DynamoDBService(
            _mockDynamoDbClient.Object,
            _mockCacheService.Object,
            _mockMetricsService.Object,
            TableName,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task QueryRecordsAsync_WhenCacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        var expectedResponse = new QueryResponse
        {
            Records = new List<AuditRecord>
            {
                new() { UserId = "testUser", SystemId = "testSystem" }
            },
            TotalRecords = 1
        };

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResponse>(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.QueryRecordsAsync(request);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockDynamoDbClient.Verify(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMetricsService.Verify(x => x.RecordCountAsync("CacheHit", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task QueryRecordsAsync_WhenCacheMiss_QueriesDynamoDB()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        var dynamoResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "PK", new AttributeValue { S = "testSystem#" } },
                    { "SK", new AttributeValue { S = $"{DateTime.UtcNow:yyyy-MM-dd}#testId" } },
                    { "GS1_PK", new AttributeValue { S = "#testUser" } },
                    { "GSI2_PK", new AttributeValue { S = "#testResource" } }
                }
            }
        };

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResponse>(It.IsAny<string>()))
            .ReturnsAsync((QueryResponse)null);

        _mockDynamoDbClient
            .Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamoResponse);

        // Act
        var result = await _service.QueryRecordsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Records.Should().HaveCount(1);
        result.TotalRecords.Should().Be(1);
        _mockMetricsService.Verify(x => x.RecordCountAsync("CacheMiss", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
        _mockMetricsService.Verify(x => x.RecordCountAsync("QuerySuccess", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task QueryRecordsAsync_WhenDynamoDBError_ThrowsException()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResponse>(It.IsAny<string>()))
            .ReturnsAsync((QueryResponse)null);

        _mockDynamoDbClient
            .Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonDynamoDBException("Test error"));

        // Act & Assert
        await Assert.ThrowsAsync<AmazonDynamoDBException>(() => _service.QueryRecordsAsync(request));
        _mockMetricsService.Verify(x => x.RecordCountAsync("QueryError", 1, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }
} 