using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;

namespace DynamoDBProcessor.Tests.Services;

public class DynamoDBServiceTests
{
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDbClient;
    private readonly Mock<ILogger<DynamoDBService>> _mockLogger;
    private readonly DynamoDBService _service;

    public DynamoDBServiceTests()
    {
        _mockDynamoDbClient = new Mock<IAmazonDynamoDB>();
        _mockLogger = new Mock<ILogger<DynamoDBService>>();

        _service = new DynamoDBService(
            _mockDynamoDbClient.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task QueryAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser"
        };

        var dynamoResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "testUser" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            Count = 1,
            ScannedCount = 1
        };

        _mockDynamoDbClient
            .Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(dynamoResponse);

        // Act
        var result = await _service.QueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Count.Should().Be(1);
        result.ScannedCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryPaginatedAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser"
        };

        var lastEvaluatedKey = new Dictionary<string, AttributeValue>
        {
            { "userId", new AttributeValue { S = "testUser" } },
            { "timestamp", new AttributeValue { N = "1234567890" } }
        };

        var dynamoResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "testUser" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            LastEvaluatedKey = lastEvaluatedKey,
            Count = 1
        };

        _mockDynamoDbClient
            .Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ReturnsAsync(dynamoResponse);

        // Act
        var result = await _service.QueryPaginatedAsync(request, lastEvaluatedKey);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeTrue();
        result.TotalItems.Should().Be(1);
        result.ContinuationToken.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WhenDynamoDBError_ThrowsException()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser"
        };

        _mockDynamoDbClient
            .Setup(x => x.QueryAsync(It.IsAny<Amazon.DynamoDBv2.Model.QueryRequest>(), default))
            .ThrowsAsync(new AmazonDynamoDBException("Test error"));

        // Act & Assert
        await Assert.ThrowsAsync<AmazonDynamoDBException>(() => _service.QueryAsync(request));
    }
} 