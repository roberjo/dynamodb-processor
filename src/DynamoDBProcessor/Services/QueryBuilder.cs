using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Services;

public class QueryBuilder
{
    private readonly string _tableName;

    public QueryBuilder(string tableName = "DynamoDBProcessor")
    {
        _tableName = tableName;
    }

    public QueryRequest BuildQuery(QueryRequest request)
    {
        var queryRequest = new QueryRequest
        {
            TableName = _tableName,
            IndexName = DetermineIndex(request),
            KeyConditionExpression = BuildKeyCondition(request),
            FilterExpression = BuildFilterExpression(request),
            ExpressionAttributeValues = BuildExpressionAttributeValues(request)
        };

        return queryRequest;
    }

    private string DetermineIndex(QueryRequest request)
    {
        if (!string.IsNullOrEmpty(request.UserId) && !string.IsNullOrEmpty(request.SystemId))
            return "UserSystemIndex";
        else if (!string.IsNullOrEmpty(request.UserId))
            return "UserIndex";
        else
            return "SystemIndex";
    }

    private string BuildKeyCondition(QueryRequest request)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(request.UserId))
            conditions.Add("userId = :userId");

        if (!string.IsNullOrEmpty(request.SystemId))
            conditions.Add("systemId = :systemId");

        return string.Join(" AND ", conditions);
    }

    private string? BuildFilterExpression(QueryRequest request)
    {
        var filters = new List<string>();

        if (request.StartDate.HasValue)
            filters.Add("timestamp >= :startDate");

        if (request.EndDate.HasValue)
            filters.Add("timestamp <= :endDate");

        if (!string.IsNullOrEmpty(request.ResourceId))
            filters.Add("resourceId = :resourceId");

        return filters.Any() ? string.Join(" AND ", filters) : null;
    }

    private Dictionary<string, AttributeValue> BuildExpressionAttributeValues(QueryRequest request)
    {
        var values = new Dictionary<string, AttributeValue>();

        if (!string.IsNullOrEmpty(request.UserId))
            values[":userId"] = new AttributeValue { S = request.UserId };

        if (!string.IsNullOrEmpty(request.SystemId))
            values[":systemId"] = new AttributeValue { S = request.SystemId };

        if (request.StartDate.HasValue)
            values[":startDate"] = new AttributeValue { S = request.StartDate.Value.ToString("o") };

        if (request.EndDate.HasValue)
            values[":endDate"] = new AttributeValue { S = request.EndDate.Value.ToString("o") };

        if (!string.IsNullOrEmpty(request.ResourceId))
            values[":resourceId"] = new AttributeValue { S = request.ResourceId };

        return values;
    }
} 