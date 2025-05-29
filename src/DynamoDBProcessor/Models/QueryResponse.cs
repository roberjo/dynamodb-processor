using System.Text.Json.Serialization;

namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents a response from a DynamoDB query operation
/// </summary>
public class DynamoQueryResponse
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
}

/// <summary>
/// Represents an individual audit record in the DynamoDB table.
/// Contains all the information about a single audit event.
/// </summary>
public class AuditRecord
{
    /// <summary>
    /// Identifier of the system where the audit event occurred.
    /// </summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user who performed the audited action.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the resource that was affected by the audited action.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// When the audited action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Unique identifier for this audit record.
    /// </summary>
    public string AuditId { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the audit event.
    /// Flexible dictionary to store any extra information.
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();
} 