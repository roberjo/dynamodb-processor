using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Middleware;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _mockLogger;
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _middleware = new ErrorHandlingMiddleware(_mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidRequest_ContinuesPipeline()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var wasNextCalled = false;

        async Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            await Task.CompletedTask;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        wasNextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithProvisionedThroughputExceeded_Returns429()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new ProvisionedThroughputExceededException("Throttling");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task InvokeAsync_WithResourceNotFoundException_Returns404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new ResourceNotFoundException("Resource not found");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_Returns400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new ValidationException("Invalid request");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithUnexpectedException_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Unexpected error");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test error");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsJsonResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new Exception("Test error");

        async Task Next(HttpContext ctx)
        {
            throw exception;
        }

        // Act
        await _middleware.InvokeAsync(context, Next);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }
} 