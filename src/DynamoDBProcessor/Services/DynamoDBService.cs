using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Implementation of IDynamoDBService that handles DynamoDB operations.
/// Provides functionality to query audit records using DynamoDB's query API.
/// </summary>
public class DynamoDBService : IDynamoDBService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private const int PageSize = 100;

    /// <summary>
    /// Initializes a new instance of the DynamoDBService.
    /// </summary>
    /// <param name="dynamoDbClient">The AWS DynamoDB client</param>
    /// <param name="tableName">The name of the DynamoDB table to query</param>
    public DynamoDBService(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = tableName;
    }

    /// <summary>
    /// Queries audit records from DynamoDB based on the provided request parameters.
    /// Implements pagination and handles continuation tokens.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A QueryResponse containing the matching records and pagination information</returns>
    public async Task<QueryResponse> QueryRecordsAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        var response = new QueryResponse();
        var queryRequest = BuildQueryRequest(request);
        
        QueryResponse? dynamoResponse = null;
        do
        {
            // If we have a continuation token, use it to get the next page
            if (dynamoResponse?.LastEvaluatedKey != null)
            {
                queryRequest.ExclusiveStartKey = dynamoResponse.LastEvaluatedKey;
            }

            // Execute the query
            dynamoResponse = await _dynamoDbClient.QueryAsync(queryRequest, cancellationToken);
            
            // Map DynamoDB items to audit records
            foreach (var item in dynamoResponse.Items)
            {
                response.Records.Add(MapToAuditRecord(item));
            }

            // Update response metadata
            response.TotalRecords += dynamoResponse.Items.Count;
            response.HasMoreRecords = dynamoResponse.LastEvaluatedKey != null;
            
            // Generate continuation token if there are more records
            if (response.HasMoreRecords)
            {
                response.ContinuationToken = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        System.Text.Json.JsonSerializer.Serialize(dynamoResponse.LastEvaluatedKey)
                    )
                );
            }

        } while (dynamoResponse.LastEvaluatedKey != null && response.Records.Count < PageSize);

        return response;
    }

    /// <summary>
    /// Builds a DynamoDB QueryRequest based on the provided QueryRequest parameters.
    /// Uses GSI1 (Global Secondary Index 1) for efficient querying.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <returns>A DynamoDB QueryRequest configured for the audit table</returns>
    private QueryRequest BuildQueryRequest(QueryRequest request)
    {
        var startDate = request.StartDate.ToString("yyyy-MM-dd");
        var endDate = request.EndDate.ToString("yyyy-MM-dd");

        return new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GS1_PK = :userId AND GS1_SK BETWEEN :startDate AND :endDate",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue { S = $"#{request.UserId}" } },
                { ":startDate", new AttributeValue { S = $"{startDate}#" } },
                { ":endDate", new AttributeValue { S = $"{endDate}#" } }
            },
            Limit = PageSize
        };
    }

    /// <summary>
    /// Maps a DynamoDB item to an AuditRecord.
    /// Extracts and formats the necessary fields from the DynamoDB item.
    /// </summary>
    /// <param name="item">The DynamoDB item to map</param>
    /// <returns>An AuditRecord containing the mapped data</returns>
    private static AuditRecord MapToAuditRecord(Dictionary<string, AttributeValue> item)
    {
        var record = new AuditRecord
        {
            SystemId = item["PK"].S.Split('#')[0],
            UserId = item["GS1_PK"].S.TrimStart('#'),
            ResourceId = item["GSI2_PK"].S.TrimStart('#'),
            AuditId = item["SK"].S.Split('#')[1],
            Timestamp = DateTime.Parse(item["SK"].S.Split('#')[0])
        };

        // Map any additional attributes that aren't part of the key schema
        foreach (var kvp in item.Where(x => !x.Key.StartsWith("PK") && !x.Key.StartsWith("SK")))
        {
            record.AdditionalData[kvp.Key] = kvp.Value.S;
        }

        return record;
    }
} 