using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DynamoDBProcessor.Services;
using DynamoDBProcessor.Validators;
using FluentValidation;
using Amazon.DynamoDBv2;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatch;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.Logging;

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
            .ConfigureServices((context, services) =>
            {
                // Add core ASP.NET Core services
                services.AddControllers();
                services.AddEndpointsApiExplorer();
                
                // Configure Swagger/OpenAPI
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "DynamoDB Query Processor API",
                        Version = "v1",
                        Description = "API for querying audit records from DynamoDB",
                        Contact = new OpenApiContact
                        {
                            Name = "API Support",
                            Email = "support@example.com"
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        }
                    });

                    // Add XML comments
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath);

                    // Add security definitions
                    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "X-API-Key",
                        Description = "API Key for authentication"
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });
                
                // Register AWS services
                services.AddAWSService<IAmazonDynamoDB>();
                services.AddAWSService<IAmazonCloudWatchLogs>();
                services.AddAWSService<IAmazonCloudWatch>();
                
                // Add memory cache
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
                
                // Register metrics service
                services.AddSingleton<IMetricsService>(sp =>
                {
                    var cloudWatchClient = sp.GetRequiredService<IAmazonCloudWatch>();
                    var logger = sp.GetRequiredService<ILogger<CloudWatchMetricsService>>();
                    var environment = context.HostingEnvironment.EnvironmentName;
                    return new CloudWatchMetricsService(cloudWatchClient, logger, $"DynamoDBProcessor/{environment}");
                });
                
                // Register the DynamoDB service
                services.AddScoped<IDynamoDBService>(sp =>
                {
                    var dynamoDbClient = sp.GetRequiredService<IAmazonDynamoDB>();
                    var logger = sp.GetRequiredService<ILogger<DynamoDBService>>();
                    return new DynamoDBService(dynamoDbClient, logger);
                });
                
                // Register FluentValidation validators
                services.AddScoped<IValidator<Models.QueryRequest>, QueryRequestValidator>();
            })
            .Configure(app =>
            {
                var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                
                // Enable Swagger UI in development environment
                if (env.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DynamoDB Query Processor API V1");
                        c.RoutePrefix = "swagger";
                        c.DocumentTitle = "DynamoDB Query Processor API Documentation";
                        c.DefaultModelsExpandDepth(-1); // Hide models section by default
                    });
                }

                // Configure middleware pipeline
                app.UseHttpsRedirection();
                
                app.UseRouting();
                app.UseAuthorization();
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
    }
} 