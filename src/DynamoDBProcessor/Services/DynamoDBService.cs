using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Exceptions;
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
    private readonly ILogger<DynamoDBService> _logger;
    private readonly QueryBuilder _queryBuilder;

    /// <summary>
    /// Initializes a new instance of the DynamoDBService.
    /// </summary>
    /// <param name="dynamoDb">The AWS DynamoDB client</param>
    /// <param name="logger">The logger</param>
    public DynamoDBService(
        IAmazonDynamoDB dynamoDb,
        ILogger<DynamoDBService> logger)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        _queryBuilder = new QueryBuilder();
    }

    /// <summary>
    /// Queries audit records from DynamoDB based on the provided request parameters.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <returns>A DynamoQueryResponse containing the matching records</returns>
    public async Task<DynamoQueryResponse> QueryAsync(DynamoDBProcessor.Models.QueryRequest request)
    {        
        try
        {
            // Convert our request to AWS SDK request
            var queryRequest = _queryBuilder.BuildQuery(request);

            var dynamoResponse = await _dynamoDb.QueryAsync(queryRequest);

            // Convert AWS SDK response to our response type
            var response = new DynamoQueryResponse
            {
                Items = ConvertFromAttributeValues(dynamoResponse.Items),
                LastEvaluatedKey = ConvertFromAttributeValues(dynamoResponse.LastEvaluatedKey),
                Count = dynamoResponse.Count,
                ScannedCount = dynamoResponse.ScannedCount,
                ConsumedCapacity = dynamoResponse.ConsumedCapacity
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying DynamoDB: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<DynamoPaginatedQueryResponse> QueryPaginatedAsync(DynamoDBProcessor.Models.QueryRequest request, Dictionary<string, AttributeValue>? lastEvaluatedKey = null)
    {
        var queryRequest = _queryBuilder.BuildQuery(request);
        if (lastEvaluatedKey != null)
        {
            queryRequest.ExclusiveStartKey = lastEvaluatedKey;
        }

        var response = await _dynamoDb.QueryAsync(queryRequest);

        return new DynamoPaginatedQueryResponse
        {
            Items = response.Items,
            LastEvaluatedKey = response.LastEvaluatedKey,
            HasMoreResults = response.LastEvaluatedKey != null,
            TotalItems = response.Count,
            ContinuationToken = response.LastEvaluatedKey != null ? Convert.ToBase64String(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(response.LastEvaluatedKey)) : null
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

        if (value.BOOL != null)
        {
            return value.BOOL;
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