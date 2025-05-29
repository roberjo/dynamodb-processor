using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Interface for DynamoDB operations
/// </summary>
public interface IDynamoDBService
{
    /// <summary>
    /// Queries records from DynamoDB based on the provided request parameters
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A DynamoQueryResponse containing the matching records and pagination information</returns>
    Task<DynamoQueryResponse> QueryRecordsAsync(DynamoQueryRequest request, CancellationToken cancellationToken = default);
} 