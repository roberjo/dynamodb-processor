using Amazon.DynamoDBv2.Model;
using Swashbuckle.AspNetCore.Filters;

namespace DynamoDBProcessor.Examples;

public class QueryRequestExample : IExamplesProvider<QueryRequest>
{
    public QueryRequest GetExamples()
    {
        return new QueryRequest
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

public class PaginatedQueryResponseExample : IExamplesProvider<PaginatedQueryResponse>
{
    public PaginatedQueryResponse GetExamples()
    {
        return new PaginatedQueryResponse
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