using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Controllers;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;
using ValidationErrorResponse = DynamoDBProcessor.Models.ValidationErrorResponse;
using ErrorResponse = DynamoDBProcessor.Models.ErrorResponse;
using System.Text.Json;

namespace DynamoDBProcessor.Tests.Controllers;

public class QueryControllerTests
{
    private readonly Mock<IValidator<QueryRequest>> _mockValidator;
    private readonly Mock<IQueryExecutor> _mockQueryExecutor;
    private readonly Mock<ILogger<QueryController>> _mockLogger;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly QueryController _controller;

    public QueryControllerTests()
    {
        _mockValidator = new Mock<IValidator<QueryRequest>>();
        _mockQueryExecutor = new Mock<IQueryExecutor>();
        _mockLogger = new Mock<ILogger<QueryController>>();
        _mockMetricsService = new Mock<IMetricsService>();
        
        _controller = new QueryController(
            _mockValidator.Object,
            _mockQueryExecutor.Object,
            _mockLogger.Object,
            _mockMetricsService.Object);
    }

    [Fact]
    public async Task QueryPaginated_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        var expectedResponse = new DynamoPaginatedQueryResponse
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
    public async Task QueryPaginated_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest();
        var validationFailures = new List<ValidationFailure>
        {
            new("UserId", "Either UserId or SystemId must be provided")
        };

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.QueryPaginated(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult?.Value as ValidationErrorResponse;
        response?.Errors.Should().HaveCount(1);
        response?.Errors[0].Field.Should().Be("UserId");
    }

    [Fact]
    public async Task QueryPaginated_WithContinuationToken_ReturnsOkResult()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        // Create a proper continuation token with AttributeValue objects
        var lastEvaluatedKey = new Dictionary<string, AttributeValue>
        {
            { "userId", new AttributeValue { S = "test-user" } },
            { "timestamp", new AttributeValue { N = "1234567890" } }
        };
        var tokenBytes = JsonSerializer.SerializeToUtf8Bytes(lastEvaluatedKey);
        var continuationToken = Convert.ToBase64String(tokenBytes);

        var expectedResponse = new DynamoPaginatedQueryResponse
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
            ContinuationToken = "next-token",
            TotalItems = 1
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

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        var expectedResponse = new DynamoPaginatedQueryResponse
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

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        var invalidToken = "invalid-token";

        // Act
        var result = await _controller.QueryPaginated(request, continuationToken: invalidToken);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult?.Value as ErrorResponse;
        response?.Message.Should().Be("Invalid continuation token");
    }

    [Fact]
    public async Task QueryPaginated_WithThrottling_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

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

        _mockValidator.Setup(x => x.ValidateAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        var maxItems = 100;

        var expectedResponse = new DynamoPaginatedQueryResponse
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