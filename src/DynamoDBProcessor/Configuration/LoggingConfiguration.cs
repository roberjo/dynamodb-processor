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

            // Console sink for all environments (simplified)
            configuration.WriteTo.Console(new CompactJsonFormatter());
        });
    }
} 