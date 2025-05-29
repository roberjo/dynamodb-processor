using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Implementation of IDynamoDBService that handles DynamoDB operations.
/// Provides functionality to query audit records using DynamoDB's query API.
/// </summary>
public class DynamoDBService : IDynamoDBService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ICacheService _cacheService;
    private readonly IMetricsService _metricsService;
    private readonly string _tableName;
    private readonly ILogger<DynamoDBService> _logger;
    private const int PageSize = 100;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the DynamoDBService.
    /// </summary>
    /// <param name="dynamoDbClient">The AWS DynamoDB client</param>
    /// <param name="cacheService">The caching service</param>
    /// <param name="metricsService">The metrics service</param>
    /// <param name="tableName">The name of the DynamoDB table to query</param>
    /// <param name="logger">The logger</param>
    public DynamoDBService(
        IAmazonDynamoDB dynamoDbClient,
        ICacheService cacheService,
        IMetricsService metricsService,
        string tableName,
        ILogger<DynamoDBService> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _cacheService = cacheService;
        _metricsService = metricsService;
        _tableName = tableName;
        _logger = logger;
    }

    /// <summary>
    /// Queries audit records from DynamoDB based on the provided request parameters.
    /// Implements pagination and handles continuation tokens.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A QueryResponse containing the matching records and pagination information</returns>
    public async Task<QueryResponse> QueryRecordsAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var dimensions = new Dictionary<string, string>
        {
            { "TableName", _tableName },
            { "UserId", request.UserId },
            { "SystemId", request.SystemId }
        };

        try
        {
            // Generate cache key based on request parameters
            var cacheKey = GenerateCacheKey(request);

            // Try to get from cache first
            var cachedResponse = await _cacheService.GetAsync<QueryResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for query with key: {CacheKey}", cacheKey);
                await _metricsService.RecordCountAsync("CacheHit", 1, dimensions);
                return cachedResponse;
            }

            _logger.LogInformation("Cache miss for query with key: {CacheKey}", cacheKey);
            await _metricsService.RecordCountAsync("CacheMiss", 1, dimensions);

            var response = new QueryResponse();
            var queryRequest = BuildQueryRequest(request);
            
            QueryResponse? dynamoResponse = null;
            do
            {
                // If we have a continuation token, use it to get the next page
                if (dynamoResponse?.LastEvaluatedKey != null)
                {
                    queryRequest.ExclusiveStartKey = dynamoResponse.LastEvaluatedKey;
                }

                // Execute the query
                dynamoResponse = await _dynamoDbClient.QueryAsync(queryRequest, cancellationToken);
                
                // Map DynamoDB items to audit records
                foreach (var item in dynamoResponse.Items)
                {
                    response.Records.Add(MapToAuditRecord(item));
                }

                // Update response metadata
                response.TotalRecords += dynamoResponse.Items.Count;
                response.HasMoreRecords = dynamoResponse.LastEvaluatedKey != null;
                
                // Generate continuation token if there are more records
                if (response.HasMoreRecords)
                {
                    response.ContinuationToken = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(
                            System.Text.Json.JsonSerializer.Serialize(dynamoResponse.LastEvaluatedKey)
                        )
                    );
                }

            } while (dynamoResponse.LastEvaluatedKey != null && response.Records.Count < PageSize);

            // Cache the response if it's not too large
            if (response.Records.Count <= PageSize)
            {
                await _cacheService.SetAsync(cacheKey, response, CacheExpiration);
            }

            // Record metrics
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            await _metricsService.RecordTimingAsync("QueryDuration", duration, dimensions);
            await _metricsService.RecordCountAsync("RecordsRetrieved", response.TotalRecords, dimensions);
            await _metricsService.RecordCountAsync("QuerySuccess", 1, dimensions);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying records");
            await _metricsService.RecordCountAsync("QueryError", 1, dimensions);
            throw;
        }
    }

    /// <summary>
    /// Builds a DynamoDB QueryRequest based on the provided QueryRequest parameters.
    /// Uses GSI1 (Global Secondary Index 1) for efficient querying.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <returns>A DynamoDB QueryRequest configured for the audit table</returns>
    private QueryRequest BuildQueryRequest(QueryRequest request)
    {
        var startDate = request.StartDate.ToString("yyyy-MM-dd");
        var endDate = request.EndDate.ToString("yyyy-MM-dd");

        return new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GS1_PK = :userId AND GS1_SK BETWEEN :startDate AND :endDate",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = $"#{request.UserId}" } },
                { ":startDate", new AttributeValue { S = $"{startDate}#" } },
                { ":endDate", new AttributeValue { S = $"{endDate}#" } }
            },
            Limit = PageSize
        };
    }

    /// <summary>
    /// Maps a DynamoDB item to an AuditRecord.
    /// Extracts and formats the necessary fields from the DynamoDB item.
    /// </summary>
    /// <param name="item">The DynamoDB item to map</param>
    /// <returns>An AuditRecord containing the mapped data</returns>
    private static AuditRecord MapToAuditRecord(Dictionary<string, AttributeValue> item)
    {
        var record = new AuditRecord
        {
            SystemId = item["PK"].S.Split('#')[0],
            UserId = item["GS1_PK"].S.TrimStart('#'),
            ResourceId = item["GSI2_PK"].S.TrimStart('#'),
            AuditId = item["SK"].S.Split('#')[1],
            Timestamp = DateTime.Parse(item["SK"].S.Split('#')[0])
        };

        // Map any additional attributes that aren't part of the key schema
        foreach (var kvp in item.Where(x => !x.Key.StartsWith("PK") && !x.Key.StartsWith("SK")))
        {
            record.AdditionalData[kvp.Key] = kvp.Value.S;
        }

        return record;
    }

    /// <summary>
    /// Generates a cache key based on the query request parameters.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <returns>A string representing the cache key</returns>
    private static string GenerateCacheKey(QueryRequest request)
    {
        return $"query_{request.UserId}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}_{request.SystemId}_{request.ResourceId}";
    }
} 