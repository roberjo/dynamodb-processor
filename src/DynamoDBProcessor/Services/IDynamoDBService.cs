using DynamoDBProcessor.Models;
using Amazon.DynamoDBv2.Model;

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
    Task<DynamoQueryResponse> QueryAsync(DynamoDBProcessor.Models.QueryRequest request);

    Task<DynamoPaginatedQueryResponse> QueryPaginatedAsync(DynamoDBProcessor.Models.QueryRequest request, Dictionary<string, AttributeValue>? lastEvaluatedKey = null);
} 