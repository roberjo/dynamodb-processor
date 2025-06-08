namespace DynamoDBProcessor.Configuration;

/// <summary>
/// Configuration class for various service limits
/// </summary>
public static class LimitsConfiguration
{
    // DynamoDB Limits
    public const int DynamoDBMaxItemSize = 400 * 1024; // 400KB
    public const int DynamoDBMaxBatchSize = 100; // 100 items per batch
    public const int DynamoDBMaxQuerySize = 1024 * 1024; // 1MB
    public const int DynamoDBMaxScanSize = 1024 * 1024; // 1MB

    // API Gateway Limits
    public const int ApiGatewayMaxPayloadSize = 10 * 1024 * 1024; // 10MB
    public const int ApiGatewayMaxResponseSize = 10 * 1024 * 1024; // 10MB
    public const int ApiGatewayTimeout = 30; // 30 seconds

    // Lambda Limits
    public const int LambdaMaxPayloadSize = 6 * 1024 * 1024; // 6MB
    public const int LambdaTimeout = 900; // 15 minutes
    public const int LambdaMinMemory = 128; // 128MB
    public const int LambdaMaxMemory = 10240; // 10GB

    // Application Limits
    public const int MaxPageSize = 1000;
    public const int MaxItemsPerQuery = 10000;
    public const int MaxConcurrentQueries = 50;
    public const int MaxRetries = 3;
    public const int BaseDelayMs = 100;
    public const int MaxCacheSize = 1000;
    public const int CacheExpirationMinutes = 5;
} 