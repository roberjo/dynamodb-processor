using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Controllers;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Controllers;

public class QueryControllerTests
{
    private readonly Mock<IQueryExecutor> _mockQueryExecutor;
    private readonly Mock<ILogger<QueryController>> _mockLogger;
    private readonly QueryController _controller;

    public QueryControllerTests()
    {
        _mockQueryExecutor = new Mock<IQueryExecutor>();
        _mockLogger = new Mock<ILogger<QueryController>>();
        _controller = new QueryController(_mockQueryExecutor.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task QueryPaginated_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var expectedResponse = new PaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            HasMoreResults = false
        };

        _mockQueryExecutor.Setup(x => x.ExecuteQueryAsync(It.IsAny<QueryRequest>(), null))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryPaginated(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task QueryPaginated_WithContinuationToken_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var continuationToken = "eyJ1c2VySWQiOiJ0ZXN0LXVzZXIiLCJ0aW1lc3RhbXAiOjEyMzQ1Njc4OTB9";

        var expectedResponse = new PaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            HasMoreResults = true,
            ContinuationToken = "next-token"
        };

        _mockQueryExecutor.Setup(x => x.ExecuteQueryAsync(It.IsAny<QueryRequest>(), It.IsAny<Dictionary<string, AttributeValue>>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryPaginated(request, continuationToken: continuationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task QueryAll_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var expectedResponse = new PaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            HasMoreResults = false,
            TotalItems = 1
        };

        _mockQueryExecutor.Setup(x => x.ExecuteQueryWithPaginationAsync(It.IsAny<QueryRequest>(), It.IsAny<int>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryAll(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task QueryPaginated_WithInvalidContinuationToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var invalidToken = "invalid-token";

        // Act
        var result = await _controller.QueryPaginated(request, continuationToken: invalidToken);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryPaginated_WithThrottling_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        _mockQueryExecutor.Setup(x => x.ExecuteQueryAsync(It.IsAny<QueryRequest>(), null))
            .ThrowsAsync(new ProvisionedThroughputExceededException("Throttling"));

        // Act
        var result = await _controller.QueryPaginated(request);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = result as StatusCodeResult;
        statusResult?.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task QueryAll_WithMaxItems_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var maxItems = 100;

        var expectedResponse = new PaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    { "userId", new AttributeValue { S = "test-user" } },
                    { "data", new AttributeValue { S = "test-data" } }
                }
            },
            HasMoreResults = false,
            TotalItems = 1
        };

        _mockQueryExecutor.Setup(x => x.ExecuteQueryWithPaginationAsync(It.IsAny<QueryRequest>(), maxItems))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryAll(request, maxItems);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedResponse);
    }
} 