using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;

namespace DynamoDBProcessor.Services;

public interface IQueryExecutor
{
    Task<DynamoPaginatedQueryResponse> ExecuteQueryAsync(
        QueryRequest request,
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null);
    
    Task<DynamoPaginatedQueryResponse> ExecuteQueryWithPaginationAsync(
        QueryRequest request,
        int maxItems = 10000);
}

public class QueryExecutor : IQueryExecutor
{
    private readonly IAmazonDynamoDB _dynamoDB;
    private readonly IMemoryCache _cache;
    private readonly IMetricsService _metrics;
    private readonly ILogger<QueryExecutor> _logger;
    private readonly QueryBuilder _queryBuilder;
    private const int MaxPageSize = 1000;
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 100;

    private readonly AsyncRetryPolicy<DynamoPaginatedQueryResponse> _retryPolicy;

    public QueryExecutor(
        IAmazonDynamoDB dynamoDB,
        IMemoryCache cache,
        IMetricsService metrics,
        ILogger<QueryExecutor> logger,
        QueryBuilder queryBuilder)
    {
        _dynamoDB = dynamoDB;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
        _queryBuilder = queryBuilder;

        _retryPolicy = Policy<DynamoPaginatedQueryResponse>
            .Handle<ProvisionedThroughputExceededException>()
            .WaitAndRetryAsync(
                MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(
                    BaseDelayMs * Math.Pow(2, retryAttempt - 1)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {Delay}ms for throttled query",
                        retryCount,
                        timeSpan.TotalMilliseconds);
                    _metrics.RecordCountAsync("QueryThrottledRetry", 1);
                });
    }

    public async Task<DynamoPaginatedQueryResponse> ExecuteQueryAsync(
        QueryRequest request,
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null)
    {
        var cacheKey = GenerateCacheKey(request, lastEvaluatedKey);
        
        if (_cache.TryGetValue(cacheKey, out DynamoPaginatedQueryResponse? cachedResponse))
        {
            _metrics.RecordCountAsync("CacheHit", 1);
            return cachedResponse!;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var queryRequest = _queryBuilder.BuildQuery(request);
                queryRequest.Limit = MaxPageSize;
                
                if (lastEvaluatedKey != null)
                {
                    queryRequest.ExclusiveStartKey = lastEvaluatedKey;
                }

                var response = await _dynamoDB.QueryAsync(queryRequest);
                
                var paginatedResponse = new DynamoPaginatedQueryResponse
                {
                    Items = response.Items,
                    LastEvaluatedKey = response.LastEvaluatedKey,
                    HasMoreResults = response.LastEvaluatedKey != null,
                    TotalItems = response.Items.Count,
                    ContinuationToken = response.LastEvaluatedKey != null
                        ? Convert.ToBase64String(
                            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                                response.LastEvaluatedKey))
                        : null
                };

                _cache.Set(cacheKey, paginatedResponse, TimeSpan.FromMinutes(5));
                _metrics.RecordCountAsync("CacheMiss", 1);
                
                return paginatedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query");
                _metrics.RecordCountAsync("QueryError", 1);
                throw;
            }
        });
    }

    public async Task<DynamoPaginatedQueryResponse> ExecuteQueryWithPaginationAsync(
        QueryRequest request,
        int maxItems = 10000)
    {
        var allItems = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastEvaluatedKey = null;
        int totalItems = 0;

        do
        {
            var response = await ExecuteQueryAsync(request, lastEvaluatedKey);
            allItems.AddRange(response.Items);
            lastEvaluatedKey = response.LastEvaluatedKey;
            totalItems += response.Items.Count;

            if (totalItems >= maxItems)
            {
                break;
            }

            if (lastEvaluatedKey != null)
            {
                await Task.Delay(100); // Prevent throttling
            }
        } while (lastEvaluatedKey != null);

        return new DynamoPaginatedQueryResponse
        {
            Items = allItems,
            LastEvaluatedKey = lastEvaluatedKey,
            HasMoreResults = lastEvaluatedKey != null,
            TotalItems = totalItems,
            ContinuationToken = lastEvaluatedKey != null
                ? Convert.ToBase64String(
                    System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                        lastEvaluatedKey))
                : null
        };
    }

    private string GenerateCacheKey(
        QueryRequest request,
        Dictionary<string, AttributeValue>? lastEvaluatedKey)
    {
        var keyParts = new List<string>
        {
            request.UserId ?? "null",
            request.SystemId ?? "null",
            request.StartDate?.ToString("o") ?? "null",
            request.EndDate?.ToString("o") ?? "null",
            request.ResourceId ?? "null"
        };

        if (lastEvaluatedKey != null)
        {
            foreach (var key in lastEvaluatedKey.OrderBy(k => k.Key))
            {
                keyParts.Add($"{key.Key}:{key.Value.S ?? key.Value.N ?? "null"}");
            }
        }

        return string.Join("|", keyParts);
    }
} 