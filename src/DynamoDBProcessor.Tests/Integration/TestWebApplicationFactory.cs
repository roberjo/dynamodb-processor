using Amazon.DynamoDBv2;
using DynamoDBProcessor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DynamoDBProcessor.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IAmazonDynamoDB> MockDynamoDb { get; }
    public Mock<IMetricsService> MockMetrics { get; }

    public TestWebApplicationFactory()
    {
        MockDynamoDb = new Mock<IAmazonDynamoDB>();
        MockMetrics = new Mock<IMetricsService>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DynamoDB client registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAmazonDynamoDB));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove the existing metrics service registration
            var metricsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMetricsService));

            if (metricsDescriptor != null)
            {
                services.Remove(metricsDescriptor);
            }

            // Add our mocked services
            services.AddSingleton(MockDynamoDb.Object);
            services.AddSingleton(MockMetrics.Object);
        });
    }
} 