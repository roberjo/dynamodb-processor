using System.Runtime.Serialization;

namespace DynamoDBProcessor.Exceptions;

/// <summary>
/// Base exception class for DynamoDBProcessor application.
/// </summary>
[Serializable]
public class DynamoDBProcessorException : Exception
{
    public string ErrorCode { get; }
    public string ErrorType { get; }

    public DynamoDBProcessorException(string message, string errorCode, string errorType) 
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorType = errorType;
    }

    public DynamoDBProcessorException(string message, string errorCode, string errorType, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorType = errorType;
    }

    protected DynamoDBProcessorException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode)) ?? "UNKNOWN_ERROR";
        ErrorType = info.GetString(nameof(ErrorType)) ?? "UNKNOWN";
    }

    [Obsolete("This method is obsolete. Use SerializationInfo.AddValue instead.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(ErrorType), ErrorType);
    }
}

/// <summary>
/// Exception thrown when a validation error occurs.
/// </summary>
public class ValidationException : DynamoDBProcessorException
{
    public ValidationException(string message) 
        : base(message, "VALIDATION_ERROR", "ValidationError")
    {
    }
}

/// <summary>
/// Exception thrown when a DynamoDB operation fails.
/// </summary>
public class DynamoDBOperationException : DynamoDBProcessorException
{
    public DynamoDBOperationException(string message, Exception innerException) 
        : base(message, "DYNAMODB_ERROR", "DynamoDBError", innerException)
    {
    }
} 