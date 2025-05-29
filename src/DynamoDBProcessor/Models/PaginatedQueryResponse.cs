using Amazon.DynamoDBv2.Model;

namespace DynamoDBProcessor.Models;

public class PaginatedQueryResponse
{
    public List<Dictionary<string, AttributeValue>> Items { get; set; } = new();
    public Dictionary<string, AttributeValue>? LastEvaluatedKey { get; set; }
    public bool HasMoreResults { get; set; }
    public int TotalItems { get; set; }
    public string? ContinuationToken { get; set; }
} 