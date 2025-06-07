using System.Text;
using DynamoDBProcessor.Configuration;
using Microsoft.IO;

namespace DynamoDBProcessor.Middleware;

/// <summary>
/// Middleware to handle API Gateway and Lambda limits
/// </summary>
public class LimitsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LimitsMiddleware> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public LimitsMiddleware(RequestDelegate next, ILogger<LimitsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _streamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check request size
        if (context.Request.ContentLength > LimitsConfiguration.ApiGatewayMaxPayloadSize)
        {
            _logger.LogWarning("Request payload exceeds API Gateway limit of {Limit}MB", 
                LimitsConfiguration.ApiGatewayMaxPayloadSize / (1024 * 1024));
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { error = "Request payload too large" });
            return;
        }

        // Enable request body buffering for size checking
        context.Request.EnableBuffering();

        // Read and check request body size
        using var requestStream = _streamManager.GetStream();
        await context.Request.Body.CopyToAsync(requestStream);
        context.Request.Body.Position = 0;

        if (requestStream.Length > LimitsConfiguration.LambdaMaxPayloadSize)
        {
            _logger.LogWarning("Request payload exceeds Lambda limit of {Limit}MB", 
                LimitsConfiguration.LambdaMaxPayloadSize / (1024 * 1024));
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { error = "Request payload too large" });
            return;
        }

        // Check response size
        var originalBodyStream = context.Response.Body;
        using var responseStream = _streamManager.GetStream();
        context.Response.Body = responseStream;

        try
        {
            await _next(context);

            // Check response size
            if (responseStream.Length > LimitsConfiguration.ApiGatewayMaxResponseSize)
            {
                _logger.LogWarning("Response payload exceeds API Gateway limit of {Limit}MB", 
                    LimitsConfiguration.ApiGatewayMaxResponseSize / (1024 * 1024));
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Response payload too large" });
                return;
            }

            if (responseStream.Length > LimitsConfiguration.LambdaMaxPayloadSize)
            {
                _logger.LogWarning("Response payload exceeds Lambda limit of {Limit}MB", 
                    LimitsConfiguration.LambdaMaxPayloadSize / (1024 * 1024));
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Response payload too large" });
                return;
            }

            // Copy response to original stream
            responseStream.Position = 0;
            await responseStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
} 