using System.Text.Json.Serialization;

namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents a paginated response from a DynamoDB query operation
/// </summary>
public class DynamoPaginatedQueryResponse
{
    /// <summary>
    /// The items returned by the query
    /// </summary>
    [JsonPropertyName("items")]
    public List<Dictionary<string, object>> Items { get; set; } = new();

    /// <summary>
    /// The last evaluated key for pagination
    /// </summary>
    [JsonPropertyName("lastEvaluatedKey")]
    public Dictionary<string, object>? LastEvaluatedKey { get; set; }

    /// <summary>
    /// The number of items returned
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The number of items scanned
    /// </summary>
    [JsonPropertyName("scannedCount")]
    public int ScannedCount { get; set; }

    /// <summary>
    /// The consumed capacity information
    /// </summary>
    [JsonPropertyName("consumedCapacity")]
    public object? ConsumedCapacity { get; set; }

    /// <summary>
    /// Indicates whether there are more items to fetch
    /// </summary>
    [JsonPropertyName("hasMoreItems")]
    public bool HasMoreItems { get; set; }

    /// <summary>
    /// The next page token for pagination
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
} 