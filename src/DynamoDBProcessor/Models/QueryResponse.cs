namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents the response from a DynamoDB query operation.
/// Contains the query results and pagination information.
/// </summary>
public class QueryResponse
{
    /// <summary>
    /// List of audit records matching the query criteria.
    /// </summary>
    public List<AuditRecord> Records { get; set; } = new();

    /// <summary>
    /// Token used for retrieving the next page of results.
    /// Null if there are no more records to fetch.
    /// </summary>
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Indicates whether there are more records available beyond the current page.
    /// </summary>
    public bool HasMoreRecords { get; set; }

    /// <summary>
    /// Total number of records matching the query criteria.
    /// </summary>
    public int TotalRecords { get; set; }
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