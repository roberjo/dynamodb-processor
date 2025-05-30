using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.Model;

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
    public List<Dictionary<string, AttributeValue>> Items { get; set; } = new();

    /// <summary>
    /// The last evaluated key for pagination
    /// </summary>
    [JsonPropertyName("lastEvaluatedKey")]
    public Dictionary<string, AttributeValue>? LastEvaluatedKey { get; set; }

    /// <summary>
    /// Whether there are more results available
    /// </summary>
    [JsonPropertyName("hasMoreResults")]
    public bool HasMoreResults { get; set; }

    /// <summary>
    /// The total number of items returned
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// A base64-encoded continuation token for the next page of results
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
} 