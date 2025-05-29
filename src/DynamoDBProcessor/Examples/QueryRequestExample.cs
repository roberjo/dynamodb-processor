using Amazon.DynamoDBv2.Model;
using Swashbuckle.AspNetCore.Filters;
using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Examples;

public class QueryRequestExample : IExamplesProvider<Amazon.DynamoDBv2.Model.QueryRequest>
{
    public Amazon.DynamoDBv2.Model.QueryRequest GetExamples()
    {
        return new Amazon.DynamoDBv2.Model.QueryRequest
        {
            TableName = "UserRecords",
            IndexName = "UserSystemIndex",
            KeyConditionExpression = "userId = :userId AND systemId = :systemId",
            FilterExpression = "createdAt BETWEEN :startDate AND :endDate",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new AttributeValue { S = "user123" },
                [":systemId"] = new AttributeValue { S = "system456" },
                [":startDate"] = new AttributeValue { S = "2024-01-01T00:00:00Z" },
                [":endDate"] = new AttributeValue { S = "2024-12-31T23:59:59Z" }
            },
            ScanIndexForward = true,
            Limit = 100
        };
    }
}

public class PaginatedQueryResponseExample : IExamplesProvider<DynamoPaginatedQueryResponse>
{
    public DynamoPaginatedQueryResponse GetExamples()
    {
        return new DynamoPaginatedQueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    ["userId"] = new AttributeValue { S = "user123" },
                    ["systemId"] = new AttributeValue { S = "system456" },
                    ["createdAt"] = new AttributeValue { S = "2024-01-15T10:30:00Z" },
                    ["status"] = new AttributeValue { S = "active" },
                    ["data"] = new AttributeValue { S = "{\"key\":\"value\"}" }
                },
                new()
                {
                    ["userId"] = new AttributeValue { S = "user123" },
                    ["systemId"] = new AttributeValue { S = "system456" },
                    ["createdAt"] = new AttributeValue { S = "2024-01-16T15:45:00Z" },
                    ["status"] = new AttributeValue { S = "pending" },
                    ["data"] = new AttributeValue { S = "{\"key\":\"value2\"}" }
                }
            },
            LastEvaluatedKey = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = "user123" },
                ["systemId"] = new AttributeValue { S = "system456" },
                ["createdAt"] = new AttributeValue { S = "2024-01-16T15:45:00Z" }
            },
            HasMoreResults = true,
            TotalItems = 2,
            ContinuationToken = "eyJ1c2VySWQiOiJ1c2VyMTIzIiwic3lzdGVtSWQiOiJzeXN0ZW00NTYiLCJjcmVhdGVkQXQiOiIyMDI0LTAxLTE2VDE1OjQ1OjAwWiJ9"
        };
    }
}

public class ErrorResponseExample : IExamplesProvider<ErrorResponse>
{
    public ErrorResponse GetExamples()
    {
        return new ErrorResponse
        {
            Message = "The requested table or index does not exist."
        };
    }
}

public class ValidationErrorResponseExample : IExamplesProvider<ValidationErrorResponse>
{
    public ValidationErrorResponse GetExamples()
    {
        return new ValidationErrorResponse
        {
            Errors = new List<ValidationError>
            {
                new()
                {
                    Field = "userId",
                    Message = "User ID is required when system ID is not provided."
                },
                new()
                {
                    Field = "startDate",
                    Message = "Start date must be before end date."
                }
            }
        };
    }
}

public class DynamoQueryRequestExample : IExamplesProvider<DynamoQueryRequest>
{
    public DynamoQueryRequest GetExamples()
    {
        return new DynamoQueryRequest
        {
            TableName = "MyTable",
            PartitionKeyValue = "user123",
            SortKeyValue = "2024-01-01",
            SortKeyOperator = "begins_with",
            FilterExpression = "attribute_exists(#status) AND #status = :status",
            ExpressionAttributeValues = new Dictionary<string, object>
            {
                { ":status", "active" }
            },
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#status", "status" }
            },
            Limit = 100,
            ScanIndexForward = true,
            ConsistentRead = false,
            ReturnConsumedCapacity = "TOTAL",
            ProjectionExpression = "#id, #name, #status"
        };
    }
}

public class DynamoQueryResponseExample : IExamplesProvider<DynamoQueryResponse>
{
    public DynamoQueryResponse GetExamples()
    {
        return new DynamoQueryResponse
        {
            Items = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "id", "item1" },
                    { "name", "Item One" },
                    { "status", "active" }
                },
                new Dictionary<string, object>
                {
                    { "id", "item2" },
                    { "name", "Item Two" },
                    { "status", "active" }
                }
            },
            LastEvaluatedKey = new Dictionary<string, object>
            {
                { "PK", "user123" },
                { "SK", "2024-01-01#item2" }
            },
            Count = 2,
            ScannedCount = 2,
            ConsumedCapacity = new
            {
                TableName = "MyTable",
                CapacityUnits = 0.5
            }
        };
    }
} 