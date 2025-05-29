using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Interface defining the contract for DynamoDB operations.
/// Provides methods for querying audit records from DynamoDB.
/// </summary>
public interface IDynamoDBService
{
    /// <summary>
    /// Queries audit records from DynamoDB based on the provided request parameters.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A QueryResponse containing the matching records and pagination information</returns>
    Task<QueryResponse> QueryRecordsAsync(QueryRequest request, CancellationToken cancellationToken = default);
} 