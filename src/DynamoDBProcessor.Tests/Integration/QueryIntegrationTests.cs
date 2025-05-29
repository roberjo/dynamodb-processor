using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Controllers;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Integration;

public class QueryIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Mock<IAmazonDynamoDB> _mockDynamoDb;
    private readonly Mock<IMetricsService> _mockMetrics;

    public QueryIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _mockDynamoDb = factory.MockDynamoDb;
        _mockMetrics = factory.MockMetrics;
    }

    [Fact]
    public async Task QueryPaginated_EndToEnd_ReturnsExpectedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            PageSize = 10
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
        var response = await _client.PostAsJsonAsync("/api/query/paginated", request);
        var result = await response.Content.ReadFromJsonAsync<DynamoPaginatedQueryResponse>();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.HasMoreResults.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAll_EndToEnd_ReturnsExpectedResponse()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var response1 = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "data", new AttributeValue { S = "data1" } } }
            },
            LastEvaluatedKey = new Dictionary<string, AttributeValue>
            {
                { "userId", new AttributeValue { S = "test-user" } },
                { "timestamp", new AttributeValue { N = "1234567890" } }
            }
        };

        var response2 = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new() { { "data", new AttributeValue { S = "data2" } } }
            }
        };

        _mockDynamoDb.SetupSequence(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(response1)
            .ReturnsAsync(response2);

        // Act
        var response = await _client.PostAsJsonAsync("/api/query/all", request);
        var result = await response.Content.ReadFromJsonAsync<DynamoPaginatedQueryResponse>();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task QueryPaginated_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest(); // Empty request

        // Act
        var response = await _client.PostAsJsonAsync("/api/query/paginated", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task QueryPaginated_WithThrottling_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ThrowsAsync(new ProvisionedThroughputExceededException("Throttling"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/query/paginated", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task QueryPaginated_WithCache_ReturnsCachedResponse()
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
                    { "data", new AttributeValue { S = "cached-data" } }
                }
            }
        };

        _mockDynamoDb.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act - First request
        var response1 = await _client.PostAsJsonAsync("/api/query/paginated", request);
        var result1 = await response1.Content.ReadFromJsonAsync<DynamoPaginatedQueryResponse>();

        // Act - Second request (should be cached)
        var response2 = await _client.PostAsJsonAsync("/api/query/paginated", request);
        var result2 = await response2.Content.ReadFromJsonAsync<DynamoPaginatedQueryResponse>();

        // Assert
        response1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        result1.Should().BeEquivalentTo(result2);
        _mockDynamoDb.Verify(x => x.QueryAsync(It.IsAny<QueryRequest>(), default), Times.Once);
    }
} 