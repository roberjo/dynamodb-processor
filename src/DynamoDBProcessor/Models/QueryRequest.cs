using System.Text.Json.Serialization;

namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents a request to query DynamoDB records
/// </summary>
public class DynamoQueryRequest
{
    /// <summary>
    /// The name of the table to query
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The partition key value to query
    /// </summary>
    [JsonPropertyName("partitionKeyValue")]
    public string PartitionKeyValue { get; set; } = string.Empty;

    /// <summary>
    /// Optional sort key value to query
    /// </summary>
    [JsonPropertyName("sortKeyValue")]
    public string? SortKeyValue { get; set; }

    /// <summary>
    /// Optional sort key comparison operator (e.g., "begins_with", "between", ">", "<", etc.)
    /// </summary>
    [JsonPropertyName("sortKeyOperator")]
    public string? SortKeyOperator { get; set; }

    /// <summary>
    /// Optional filter expression to apply to the query results
    /// </summary>
    [JsonPropertyName("filterExpression")]
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Optional expression attribute values for the filter expression
    /// </summary>
    [JsonPropertyName("expressionAttributeValues")]
    public Dictionary<string, object>? ExpressionAttributeValues { get; set; }

    /// <summary>
    /// Optional limit on the number of items to return
    /// </summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// Optional exclusive start key for pagination
    /// </summary>
    [JsonPropertyName("exclusiveStartKey")]
    public Dictionary<string, object>? ExclusiveStartKey { get; set; }

    /// <summary>
    /// Optional flag to scan index forward (default is true)
    /// </summary>
    [JsonPropertyName("scanIndexForward")]
    public bool? ScanIndexForward { get; set; }

    /// <summary>
    /// Optional flag to enable consistent reads (default is false)
    /// </summary>
    [JsonPropertyName("consistentRead")]
    public bool? ConsistentRead { get; set; }

    /// <summary>
    /// Optional flag to return consumed capacity information
    /// </summary>
    [JsonPropertyName("returnConsumedCapacity")]
    public string? ReturnConsumedCapacity { get; set; }

    /// <summary>
    /// Optional projection expression to specify which attributes to return
    /// </summary>
    [JsonPropertyName("projectionExpression")]
    public string? ProjectionExpression { get; set; }

    /// <summary>
    /// Optional expression attribute names for the projection expression
    /// </summary>
    [JsonPropertyName("expressionAttributeNames")]
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }
} 