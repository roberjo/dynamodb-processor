using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Implementation of IDynamoDBService that handles DynamoDB operations.
/// Provides functionality to query audit records using DynamoDB's query API.
/// </summary>
public class DynamoDBService : IDynamoDBService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DynamoDBService> _logger;
    private readonly IMetricsService _metricsService;

    /// <summary>
    /// Initializes a new instance of the DynamoDBService.
    /// </summary>
    /// <param name="dynamoDb">The AWS DynamoDB client</param>
    /// <param name="cacheService">The caching service</param>
    /// <param name="logger">The logger</param>
    /// <param name="metricsService">The metrics service</param>
    public DynamoDBService(
        IAmazonDynamoDB dynamoDb,
        ICacheService cacheService,
        ILogger<DynamoDBService> logger,
        IMetricsService metricsService)
    {
        _dynamoDb = dynamoDb;
        _cacheService = cacheService;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Queries audit records from DynamoDB based on the provided request parameters.
    /// Implements pagination and handles continuation tokens.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A QueryResponse containing the matching records and pagination information</returns>
    public async Task<DynamoQueryResponse> QueryRecordsAsync(DynamoQueryRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"query_{request.TableName}_{request.PartitionKeyValue}_{request.SortKeyValue}_{request.SortKeyOperator}_{request.FilterExpression}";
        
        try
        {
            // Try to get from cache first
            var cachedResponse = await _cacheService.GetAsync<DynamoQueryResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for query: {CacheKey}", cacheKey);
                _metricsService.RecordCacheHit("dynamodb_query");
                return cachedResponse;
            }

            _logger.LogInformation("Cache miss for query: {CacheKey}", cacheKey);
            _metricsService.RecordCacheMiss("dynamodb_query");

            // Convert our request to AWS SDK request
            var dynamoRequest = new QueryRequest
            {
                TableName = request.TableName,
                KeyConditionExpression = BuildKeyConditionExpression(request),
                FilterExpression = request.FilterExpression,
                ExpressionAttributeValues = ConvertToAttributeValues(request.ExpressionAttributeValues),
                ExpressionAttributeNames = request.ExpressionAttributeNames,
                Limit = request.Limit,
                ExclusiveStartKey = ConvertToAttributeValues(request.ExclusiveStartKey),
                ScanIndexForward = request.ScanIndexForward,
                ConsistentRead = request.ConsistentRead,
                ReturnConsumedCapacity = request.ReturnConsumedCapacity,
                ProjectionExpression = request.ProjectionExpression
            };

            var dynamoResponse = await _dynamoDb.QueryAsync(dynamoRequest, cancellationToken);

            // Convert AWS SDK response to our response type
            var response = new DynamoQueryResponse
            {
                Items = ConvertFromAttributeValues(dynamoResponse.Items),
                LastEvaluatedKey = ConvertFromAttributeValues(dynamoResponse.LastEvaluatedKey),
                Count = dynamoResponse.Count,
                ScannedCount = dynamoResponse.ScannedCount,
                ConsumedCapacity = dynamoResponse.ConsumedCapacity
            };

            // Cache the response
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying DynamoDB: {Message}", ex.Message);
            _metricsService.RecordError("dynamodb_query", ex);
            throw;
        }
    }

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

    private Dictionary<string, AttributeValue>? ConvertToAttributeValues(Dictionary<string, object>? values)
    {
        if (values == null) return null;

        var result = new Dictionary<string, AttributeValue>();
        foreach (var kvp in values)
        {
            result[kvp.Key] = ConvertToAttributeValue(kvp.Value);
        }
        return result;
    }

    private AttributeValue ConvertToAttributeValue(object value)
    {
        if (value == null)
        {
            return new AttributeValue { NULL = true };
        }

        return value switch
        {
            string s => new AttributeValue { S = s },
            int i => new AttributeValue { N = i.ToString() },
            long l => new AttributeValue { N = l.ToString() },
            double d => new AttributeValue { N = d.ToString() },
            decimal d => new AttributeValue { N = d.ToString() },
            bool b => new AttributeValue { BOOL = b },
            byte[] bytes => new AttributeValue { B = new MemoryStream(bytes) },
            List<string> ss => new AttributeValue { SS = ss },
            List<int> ns => new AttributeValue { NS = ns.Select(n => n.ToString()).ToList() },
            List<long> ns => new AttributeValue { NS = ns.Select(n => n.ToString()).ToList() },
            List<double> ns => new AttributeValue { NS = ns.Select(n => n.ToString()).ToList() },
            List<decimal> ns => new AttributeValue { NS = ns.Select(n => n.ToString()).ToList() },
            List<byte[]> bs => new AttributeValue { BS = bs.Select(b => new MemoryStream(b)).ToList() },
            Dictionary<string, object> m => new AttributeValue { M = ConvertToAttributeValues(m) },
            List<object> l => new AttributeValue { L = l.Select(ConvertToAttributeValue).ToList() },
            _ => new AttributeValue { S = value.ToString() }
        };
    }

    private List<Dictionary<string, object>> ConvertFromAttributeValues(List<Dictionary<string, AttributeValue>>? items)
    {
        if (items == null) return new List<Dictionary<string, object>>();

        return items.Select(item => ConvertFromAttributeValues(item)).ToList();
    }

    private Dictionary<string, object>? ConvertFromAttributeValues(Dictionary<string, AttributeValue>? item)
    {
        if (item == null) return null;

        var result = new Dictionary<string, object>();
        foreach (var kvp in item)
        {
            result[kvp.Key] = ConvertFromAttributeValue(kvp.Value);
        }
        return result;
    }

    private object ConvertFromAttributeValue(AttributeValue value)
    {
        if (value.NULL)
        {
            return null!;
        }

        if (value.S != null)
        {
            return value.S;
        }

        if (value.N != null)
        {
            if (decimal.TryParse(value.N, out decimal result))
            {
                return result;
            }
            return value.N;
        }

        if (value.BOOL.HasValue)
        {
            return value.BOOL.Value;
        }

        if (value.B != null)
        {
            return value.B.ToArray();
        }

        if (value.SS != null)
        {
            return value.SS;
        }

        if (value.NS != null)
        {
            return value.NS.Select(n => decimal.Parse(n)).ToList();
        }

        if (value.BS != null)
        {
            return value.BS.Select(b => b.ToArray()).ToList();
        }

        if (value.M != null)
        {
            return ConvertFromAttributeValues(value.M);
        }

        if (value.L != null)
        {
            return value.L.Select(ConvertFromAttributeValue).ToList();
        }

        return null!;
    }

    private void ValidateItemSize(Dictionary<string, AttributeValue> item)
    {
        const int MaxItemSize = 400 * 1024; // 400KB DynamoDB limit
        int currentSize = 0;

        foreach (var kvp in item)
        {
            // Add size of attribute name
            currentSize += System.Text.Encoding.UTF8.GetByteCount(kvp.Key);

            // Add size of attribute value
            currentSize += GetAttributeValueSize(kvp.Value);

            if (currentSize > MaxItemSize)
            {
                throw new DynamoDBProcessorException(
                    "Item size exceeds DynamoDB's 400KB limit",
                    "ITEM_SIZE_LIMIT_EXCEEDED",
                    "ValidationError");
            }
        }
    }

    private int GetAttributeValueSize(AttributeValue value)
    {
        if (value.NULL)
        {
            return 0;
        }

        if (value.S != null)
        {
            return System.Text.Encoding.UTF8.GetByteCount(value.S);
        }

        if (value.N != null)
        {
            return System.Text.Encoding.UTF8.GetByteCount(value.N);
        }

        if (value.B != null)
        {
            return (int)value.B.Length;
        }

        if (value.SS != null)
        {
            return value.SS.Sum(s => System.Text.Encoding.UTF8.GetByteCount(s));
        }

        if (value.NS != null)
        {
            return value.NS.Sum(n => System.Text.Encoding.UTF8.GetByteCount(n));
        }

        if (value.BS != null)
        {
            return value.BS.Sum(b => (int)b.Length);
        }

        if (value.M != null)
        {
            return value.M.Sum(kvp => 
                System.Text.Encoding.UTF8.GetByteCount(kvp.Key) + 
                GetAttributeValueSize(kvp.Value));
        }

        if (value.L != null)
        {
            return value.L.Sum(GetAttributeValueSize);
        }

        return 0;
    }
} 