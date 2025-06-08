# Query Request Handling

## Overview

The DynamoDB Processor handles flexible query requests where fields like `userId` and `systemId` can be null. The system is designed to build appropriate queries based on the available fields while maintaining efficient data access patterns. The processor implements pagination to handle large result sets and prevent hitting DynamoDB's 1MB data limit.

## Service Limits

The system implements comprehensive handling of various service limits to ensure reliable operation:

### DynamoDB Limits
- **Item Size**: 400KB per item
- **Query Response**: 1MB per query
- **Batch Operations**: 100 items per batch
- **Scan Size**: 1MB per scan
- **Throughput**: Handled through exponential backoff

### API Gateway Limits
- **Request Payload**: 10MB
- **Response Payload**: 10MB
- **Timeout**: 30 seconds

### Lambda Limits
- **Payload Size**: 6MB (request/response)
- **Timeout**: 15 minutes
- **Memory**: 128MB - 10GB configurable

### Application Limits
- **Max Page Size**: 1000 items
- **Max Items Per Query**: 10000 items
- **Max Concurrent Queries**: 50
- **Max Retries**: 3
- **Base Delay**: 100ms
- **Max Cache Size**: 1000 items
- **Cache Expiration**: 5 minutes

## Limit Handling Implementation

### Request Size Validation
```csharp
// LimitsMiddleware.cs
if (context.Request.ContentLength > LimitsConfiguration.ApiGatewayMaxPayloadSize)
{
    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
    await context.Response.WriteAsJsonAsync(new { error = "Request payload too large" });
    return;
}
```

### Response Size Validation
```csharp
// QueryExecutor.cs
var responseSize = CalculateResponseSize(response);
if (responseSize > LimitsConfiguration.DynamoDBMaxQuerySize)
{
    _logger.LogWarning(
        "Query response size {Size}MB exceeds DynamoDB limit of {Limit}MB",
        responseSize / (1024 * 1024),
        LimitsConfiguration.DynamoDBMaxQuerySize / (1024 * 1024));
    throw new DynamoDBProcessorException(
        "Query response too large",
        "RESPONSE_SIZE_LIMIT_EXCEEDED",
        "ValidationError");
}
```

### Pagination with Size Checks
```csharp
// QueryExecutor.cs
public async Task<DynamoPaginatedQueryResponse> ExecuteQueryWithPaginationAsync(
    QueryRequest request,
    int maxItems = 10000)
{
    maxItems = Math.Min(maxItems, LimitsConfiguration.MaxItemsPerQuery);
    int totalSize = 0;

    do
    {
        var response = await ExecuteQueryAsync(request, lastEvaluatedKey);
        var newItemsSize = CalculateItemsSize(response.Items);
        
        // Check Lambda payload limit
        if (totalSize + newItemsSize > LimitsConfiguration.LambdaMaxPayloadSize)
        {
            break;
        }

        // Process items...
    } while (lastEvaluatedKey != null);
}
```

## Best Practices

1. **Request Handling**
   - Validate request size before processing
   - Use streaming for large payloads
   - Implement proper error responses
   - Use rate limiting (100 requests per minute per IP)

2. **Response Handling**
   - Monitor response sizes
   - Implement pagination for large results
   - Use compression when appropriate
   - Cache responses for 5 minutes

3. **Performance Optimization**
   - Cache frequently accessed data
   - Use efficient memory management
   - Implement proper cleanup
   - Use exponential backoff for retries

4. **Error Handling**
   - Provide clear error messages
   - Log limit-related issues
   - Implement retry logic with backoff
   - Handle throttling gracefully

## Configuration

All limits are centralized in `LimitsConfiguration.cs`:

```csharp
public static class LimitsConfiguration
{
    // DynamoDB Limits
    public const int DynamoDBMaxItemSize = 400 * 1024; // 400KB
    public const int DynamoDBMaxBatchSize = 100;
    public const int DynamoDBMaxQuerySize = 1024 * 1024; // 1MB
    public const int DynamoDBMaxScanSize = 1024 * 1024; // 1MB

    // API Gateway Limits
    public const int ApiGatewayMaxPayloadSize = 10 * 1024 * 1024; // 10MB
    public const int ApiGatewayMaxResponseSize = 10 * 1024 * 1024; // 10MB
    public const int ApiGatewayTimeout = 30; // 30 seconds

    // Lambda Limits
    public const int LambdaMaxPayloadSize = 6 * 1024 * 1024; // 6MB
    public const int LambdaTimeout = 900; // 15 minutes
    public const int LambdaMinMemory = 128; // 128MB
    public const int LambdaMaxMemory = 10240; // 10GB

    // Application Limits
    public const int MaxPageSize = 1000;
    public const int MaxItemsPerQuery = 10000;
    public const int MaxConcurrentQueries = 50;
    public const int MaxRetries = 3;
    public const int BaseDelayMs = 100;
    public const int MaxCacheSize = 1000;
    public const int CacheExpirationMinutes = 5;
}
```

## Request Validation

```csharp
public class QueryRequestValidator : AbstractValidator<QueryRequest>
{
    public QueryRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.SystemId))
            .WithMessage("Either UserId or SystemId must be provided");

        RuleFor(x => x.SystemId)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.UserId))
            .WithMessage("Either UserId or SystemId must be provided");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .When(x => x.EndDate != null)
            .WithMessage("StartDate is required when EndDate is provided");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .When(x => x.StartDate != null)
            .WithMessage("EndDate is required when StartDate is provided")
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate != null)
            .WithMessage("EndDate must be after StartDate");
    }
}
```

## Query Building

The system uses a flexible query builder that constructs appropriate DynamoDB queries based on the available fields:

```csharp
public class QueryBuilder
{
    private string BuildKeyConditionExpression(DynamoQueryRequest request)
    {
        var conditions = new List<string>();
        
        // Add partition key condition
        conditions.Add($"#pk = :pk");
        
        // Add sort key condition if provided
        if (!string.IsNullOrEmpty(request.SortKeyValue))
        {
            if (string.IsNullOrEmpty(request.SortKeyOperator))
            {
                conditions.Add("#sk = :sk");
            }
            else
            {
                conditions.Add($"#sk {request.SortKeyOperator} :sk");
            }
        }

        return string.Join(" AND ", conditions);
    }
}
```

## Error Handling

The system implements comprehensive error handling for limit-related issues:

1. **Request Size Errors**
   - Returns 413 Payload Too Large
   - Provides clear error message
   - Logs the incident
   - Records metrics

2. **Response Size Errors**
   - Returns 500 Internal Server Error
   - Implements pagination
   - Logs the incident
   - Records metrics

3. **DynamoDB Errors**
   - Handles throttling with retries
   - Implements exponential backoff
   - Logs detailed error information
   - Records metrics

4. **Rate Limiting**
   - Global rate limit: 100 requests per minute
   - IP-based rate limiting
   - Custom rules for localhost
   - Stack blocked requests

## Monitoring

The system tracks various metrics related to limit handling:

1. **Size Metrics**
   - Request sizes
   - Response sizes
   - Cache hit/miss rates
   - Throttling events

2. **Error Metrics**
   - Limit exceeded errors
   - Throttling events
   - Retry attempts
   - Validation errors

3. **Performance Metrics**
   - Query execution times
   - Pagination efficiency
   - Cache effectiveness
   - Memory usage

4. **CloudWatch Integration**
   - Custom metrics
   - Environment-specific namespaces
   - Detailed logging
   - Performance monitoring 