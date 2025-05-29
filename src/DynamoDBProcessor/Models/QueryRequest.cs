using System.ComponentModel.DataAnnotations;

namespace DynamoDBProcessor.Models;

/// <summary>
/// Represents a request to query audit records from DynamoDB.
/// Contains the necessary parameters to filter and paginate the results.
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// The unique identifier of the user whose audit records are being queried.
    /// Required field that maps to the partition key in DynamoDB.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The start date for filtering audit records.
    /// Required field that helps define the time range for the query.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date for filtering audit records.
    /// Required field that helps define the time range for the query.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The identifier of the system being audited.
    /// Required field that helps filter records by system.
    /// </summary>
    [Required]
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the resource being audited.
    /// Required field that helps filter records by resource.
    /// </summary>
    [Required]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Optional token used for pagination of query results.
    /// When present, indicates the starting point for the next page of results.
    /// </summary>
    public string? ContinuationToken { get; set; }
} 