# Query Request Handling

## Overview

The DynamoDB Processor handles flexible query requests where fields like `userId` and `systemId` can be null. The system is designed to build appropriate queries based on the available fields while maintaining efficient data access patterns.

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
    public QueryRequest BuildQuery(QueryRequest request)
    {
        var queryRequest = new QueryRequest
        {
            TableName = _tableName,
            IndexName = DetermineIndex(request),
            KeyConditionExpression = BuildKeyCondition(request),
            FilterExpression = BuildFilterExpression(request),
            ExpressionAttributeValues = BuildExpressionAttributeValues(request)
        };

        return queryRequest;
    }

    private string DetermineIndex(QueryRequest request)
    {
        if (!string.IsNullOrEmpty(request.UserId) && !string.IsNullOrEmpty(request.SystemId))
            return "UserSystemIndex";
        else if (!string.IsNullOrEmpty(request.UserId))
            return "UserIndex";
        else
            return "SystemIndex";
    }

    private string BuildKeyCondition(QueryRequest request)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(request.UserId))
            conditions.Add("userId = :userId");

        if (!string.IsNullOrEmpty(request.SystemId))
            conditions.Add("systemId = :systemId");

        return string.Join(" AND ", conditions);
    }

    private string BuildFilterExpression(QueryRequest request)
    {
        var filters = new List<string>();

        if (request.StartDate.HasValue)
            filters.Add("timestamp >= :startDate");

        if (request.EndDate.HasValue)
            filters.Add("timestamp <= :endDate");

        if (!string.IsNullOrEmpty(request.ResourceId))
            filters.Add("resourceId = :resourceId");

        return filters.Any() ? string.Join(" AND ", filters) : null;
    }

    private Dictionary<string, AttributeValue> BuildExpressionAttributeValues(QueryRequest request)
    {
        var values = new Dictionary<string, AttributeValue>();

        if (!string.IsNullOrEmpty(request.UserId))
            values[":userId"] = new AttributeValue { S = request.UserId };

        if (!string.IsNullOrEmpty(request.SystemId))
            values[":systemId"] = new AttributeValue { S = request.SystemId };

        if (request.StartDate.HasValue)
            values[":startDate"] = new AttributeValue { S = request.StartDate.Value.ToString("o") };

        if (request.EndDate.HasValue)
            values[":endDate"] = new AttributeValue { S = request.EndDate.Value.ToString("o") };

        if (!string.IsNullOrEmpty(request.ResourceId))
            values[":resourceId"] = new AttributeValue { S = request.ResourceId };

        return values;
    }
}
```

## Query Execution

The query execution process includes caching and error handling:

```csharp
public class QueryExecutor
{
    private readonly IAmazonDynamoDB _dynamoDB;
    private readonly IMemoryCache _cache;
    private readonly IMetricsService _metrics;
    private readonly ILogger<QueryExecutor> _logger;

    public async Task<QueryResponse> ExecuteQueryAsync(QueryRequest request)
    {
        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out QueryResponse cachedResponse))
        {
            _metrics.RecordCountAsync("CacheHit", 1);
            return cachedResponse;
        }

        try
        {
            var queryRequest = _queryBuilder.BuildQuery(request);
            var response = await _dynamoDB.QueryAsync(queryRequest);
            
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
            _metrics.RecordCountAsync("CacheMiss", 1);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            _metrics.RecordCountAsync("QueryError", 1);
            throw;
        }
    }

    private string GenerateCacheKey(QueryRequest request)
    {
        var keyParts = new List<string>
        {
            request.UserId ?? "null",
            request.SystemId ?? "null",
            request.StartDate?.ToString("o") ?? "null",
            request.EndDate?.ToString("o") ?? "null",
            request.ResourceId ?? "null"
        };

        return string.Join("|", keyParts);
    }
}
```

## Supported Query Patterns

The system supports the following query patterns:

1. **User ID Only**
   ```json
   {
     "userId": "user123"
   }
   ```

2. **System ID Only**
   ```json
   {
     "systemId": "system456"
   }
   ```

3. **User ID + System ID**
   ```json
   {
     "userId": "user123",
     "systemId": "system456"
   }
   ```

4. **User ID + Date Range**
   ```json
   {
     "userId": "user123",
     "startDate": "2024-01-01T00:00:00Z",
     "endDate": "2024-01-31T23:59:59Z"
   }
   ```

5. **System ID + Date Range**
   ```json
   {
     "systemId": "system456",
     "startDate": "2024-01-01T00:00:00Z",
     "endDate": "2024-01-31T23:59:59Z"
   }
   ```

6. **All Fields**
   ```json
   {
     "userId": "user123",
     "systemId": "system456",
     "startDate": "2024-01-01T00:00:00Z",
     "endDate": "2024-01-31T23:59:59Z",
     "resourceId": "resource789"
   }
   ```

## Performance Considerations

1. **Index Selection**
   - Uses appropriate GSI based on query fields
   - Optimizes for most common query patterns
   - Minimizes scan operations

2. **Caching Strategy**
   - Caches query results for 5 minutes
   - Uses composite cache keys
   - Implements cache invalidation

3. **Error Handling**
   - Validates request before execution
   - Handles DynamoDB errors gracefully
   - Provides meaningful error messages

4. **Monitoring**
   - Tracks query performance
   - Monitors cache hit/miss rates
   - Records error rates

## Best Practices

1. **Query Design**
   - Use appropriate indexes
   - Minimize filter expressions
   - Optimize key conditions

2. **Caching**
   - Use appropriate TTL
   - Implement cache invalidation
   - Monitor cache performance

3. **Error Handling**
   - Validate input thoroughly
   - Handle errors gracefully
   - Provide clear error messages

4. **Monitoring**
   - Track query patterns
   - Monitor performance
   - Alert on issues 