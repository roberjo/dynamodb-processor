using System.Text;
using Microsoft.IO;
using DynamoDBProcessor.Exceptions;

namespace DynamoDBProcessor.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _streamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-ID"] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method
        });

        try
        {
            // Log request
            var requestBody = await ReadRequestBodyAsync(context.Request);
            _logger.LogInformation("Request: {Method} {Path} {Body}", 
                context.Request.Method, 
                context.Request.Path, 
                requestBody);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = _streamManager.GetStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Log response
            var response = await ReadResponseBodyAsync(context.Response);
            _logger.LogInformation("Response: {StatusCode} {Body}", 
                context.Response.StatusCode, 
                response);

            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (DynamoDBProcessorException ex)
        {
            _logger.LogError(ex, "Application error occurred: {ErrorCode} {ErrorType}", 
                ex.ErrorCode, 
                ex.ErrorType);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                ErrorCode = ex.ErrorCode,
                ErrorType = ex.ErrorType,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                ErrorCode = "INTERNAL_ERROR",
                ErrorType = "InternalError",
                Message = "An unexpected error occurred"
            });
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return text;
    }
} 