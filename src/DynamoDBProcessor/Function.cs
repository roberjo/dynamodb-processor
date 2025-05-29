using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DynamoDBProcessor.Services;
using DynamoDBProcessor.Validators;
using DynamoDBProcessor.Configuration;
using DynamoDBProcessor.Middleware;
using FluentValidation;
using Amazon.DynamoDBv2;
using Amazon.CloudWatchLogs;

namespace DynamoDBProcessor;

/// <summary>
/// Main Lambda function handler that bootstraps the ASP.NET Core application.
/// Inherits from APIGatewayProxyFunction to handle API Gateway HTTP requests.
/// </summary>
public class Function : APIGatewayProxyFunction
{
    /// <summary>
    /// Initializes the ASP.NET Core application with required services and middleware.
    /// This method is called when the Lambda function is invoked.
    /// </summary>
    /// <param name="builder">The web host builder used to configure the application</param>
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .ConfigureLogging() // Use our custom logging configuration
            .ConfigureServices((context, services) =>
            {
                // Add core ASP.NET Core services
                services.AddControllers(); // Adds MVC controllers
                services.AddEndpointsApiExplorer(); // Enables API endpoint discovery
                services.AddSwaggerGen(); // Adds Swagger/OpenAPI documentation
                
                // Register AWS services
                services.AddAWSService<IAmazonDynamoDB>();
                services.AddAWSService<IAmazonCloudWatchLogs>();
                
                // Register the DynamoDB service with configuration
                // This service handles all DynamoDB operations and is scoped per request
                services.AddScoped<IDynamoDBService>(sp =>
                {
                    var dynamoDbClient = sp.GetRequiredService<IAmazonDynamoDB>();
                    // Get table name from environment variables
                    var tableName = context.Configuration["DYNAMODB_TABLE_NAME"] 
                        ?? throw new InvalidOperationException("DYNAMODB_TABLE_NAME configuration is missing");
                    return new DynamoDBService(dynamoDbClient, tableName);
                });
                
                // Register FluentValidation validators for request validation
                services.AddScoped<IValidator<Models.QueryRequest>, QueryRequestValidator>();
            })
            .Configure(app =>
            {
                // Enable Swagger UI in development environment
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Configure middleware pipeline
                app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
                app.UseRouting(); // Enable endpoint routing
                app.UseAuthorization(); // Enable authorization middleware
                
                // Add our custom middleware
                app.UseMiddleware<RequestResponseLoggingMiddleware>();
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers(); // Map controller endpoints
                });
            });
    }
} 