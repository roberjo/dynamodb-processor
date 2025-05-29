using Amazon.DynamoDBv2;
using DynamoDBProcessor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DynamoDBProcessor.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDynamoDBProcessorServices(
        this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IDynamoDBService, DynamoDBService>();
        services.AddSingleton<IQueryExecutor, QueryExecutor>();
        services.AddSingleton<IMetricsService, MetricsService>();

        return services;
    }
} 