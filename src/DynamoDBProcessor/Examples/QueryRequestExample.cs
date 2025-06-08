using Amazon.DynamoDBv2.Model;
using Swashbuckle.AspNetCore.Filters;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Controllers;

namespace DynamoDBProcessor.Examples;

public class QueryRequestExample : IExamplesProvider<DynamoDBProcessor.Models.QueryRequest>
{
    public DynamoDBProcessor.Models.QueryRequest GetExamples()
    {
        return new DynamoDBProcessor.Models.QueryRequest
        {
            UserId = "user123",
            SystemId = "system456",
            ResourceId = "resource789",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
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

public class ErrorResponseExample : IExamplesProvider<DynamoDBProcessor.Controllers.ErrorResponse>
{
    public DynamoDBProcessor.Controllers.ErrorResponse GetExamples()
    {
        return new DynamoDBProcessor.Controllers.ErrorResponse
        {
            Message = "The requested table or index does not exist."
        };
    }
}

public class ValidationErrorResponseExample : IExamplesProvider<DynamoDBProcessor.Controllers.ValidationErrorResponse>
{
    public DynamoDBProcessor.Controllers.ValidationErrorResponse GetExamples()
    {
        return new DynamoDBProcessor.Controllers.ValidationErrorResponse
        {
            Errors = new List<DynamoDBProcessor.Controllers.ValidationError>
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