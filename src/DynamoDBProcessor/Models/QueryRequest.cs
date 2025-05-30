using System.Text.Json.Serialization;

namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents a request to query DynamoDB records
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// The user ID to query by
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// The system ID to query by
    /// </summary>
    [JsonPropertyName("systemId")]
    public string? SystemId { get; set; }

    /// <summary>
    /// The resource ID to filter by
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// The start date for filtering records
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end date for filtering records
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

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
} 