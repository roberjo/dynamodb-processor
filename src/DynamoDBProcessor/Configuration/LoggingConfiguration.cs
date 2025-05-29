using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Amazon.CloudWatchLogs;
using Serilog.Sinks.AwsCloudWatch;

namespace DynamoDBProcessor.Configuration;

public static class LoggingConfiguration
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            var environment = context.HostingEnvironment.EnvironmentName;
            var applicationName = context.HostingEnvironment.ApplicationName;

            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ApplicationName", applicationName)
                .Enrich.WithProperty("Environment", environment);

            // Console sink for local development
            if (environment == "Development")
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                // CloudWatch sink for non-development environments
                var cloudWatchClient = services.GetRequiredService<IAmazonCloudWatchLogs>();
                configuration.WriteTo.AmazonCloudWatch(
                    logGroup: $"/aws/lambda/{applicationName}",
                    logStreamPrefix: $"{environment}-",
                    cloudWatchClient: cloudWatchClient,
                    textFormatter: new CompactJsonFormatter(),
                    options: new CloudWatchSinkOptions
                    {
                        BatchSizeLimit = 100,
                        QueueSizeLimit = 10000,
                        Period = TimeSpan.FromSeconds(5),
                        CreateLogGroup = true
                    });
            }
        });
    }
} 