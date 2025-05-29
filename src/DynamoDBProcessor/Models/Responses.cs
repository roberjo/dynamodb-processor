using Amazon.DynamoDBv2.Model;

namespace DynamoDBProcessor.Models;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ValidationErrorResponse
{
    public List<ValidationError> Errors { get; set; } = new();
} 