using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace DynamoDBProcessor.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DynamoDB Processor API",
                Version = "v1",
                Description = @"
# DynamoDB Processor API

This API provides endpoints for querying and processing DynamoDB records with support for pagination and flexible query patterns.

## Features
- Basic querying with flexible field combinations
- Paginated queries with continuation tokens
- Bulk querying with configurable limits
- Caching support for improved performance
- Comprehensive error handling
- Rate limiting and throttling protection

## Authentication
The API supports multiple authentication methods:
- API Key authentication
- JWT Bearer token authentication
- AWS IAM authentication

## Rate Limiting
- Default: 100 requests per minute
- Burst: 200 requests per minute
- Custom limits available for enterprise customers",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@example.com",
                    Url = new Uri("https://example.com/support")
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
            c.IncludeXmlComments(xmlPath);

            // Add security definitions
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key authentication",
                Name = "X-API-Key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityDefinition("AWS4-HMAC-SHA256", new OpenApiSecurityScheme
            {
                Description = "AWS IAM authentication using AWS Signature Version 4",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "AWS4-HMAC-SHA256"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "AWS4-HMAC-SHA256"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add example requests/responses
            c.ExampleFilters();

            // Add custom operation filters
            c.OperationFilter<AddRequiredHeaderParameter>();
            c.OperationFilter<AddRateLimitHeaders>();
        });

        // Add example filters
        services.AddSwaggerExamplesFromAssemblyOf<QueryRequestExample>();

        return services;
    }
}

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "DynamoDB Processor API",
                    Version = description.ApiVersion.ToString(),
                    Description = "API for processing and querying DynamoDB records",
                    Contact = new OpenApiContact
                    {
                        Name = "API Support",
                        Email = "support@example.com",
                        Url = new Uri("https://example.com/support")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

            // Add version-specific security requirements
            if (description.ApiVersion.MajorVersion >= 2)
            {
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "AWS4-HMAC-SHA256"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            }
        }

        // Add custom UI styling
        options.EnableAnnotations();
        options.DocumentFilter<AddCustomHeaderFilter>();
        options.DocumentFilter<AddCustomFooterFilter>();
    }
}

public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Request-ID",
            In = ParameterLocation.Header,
            Description = "Unique request identifier for tracking",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
    }
}

public class AddRateLimitHeaders : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.Add("429", new OpenApiResponse
        {
            Description = "Too Many Requests",
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["X-RateLimit-Limit"] = new OpenApiHeader
                {
                    Description = "The maximum number of requests per minute",
                    Schema = new OpenApiSchema { Type = "integer" }
                },
                ["X-RateLimit-Remaining"] = new OpenApiHeader
                {
                    Description = "The number of requests remaining in the current rate limit window",
                    Schema = new OpenApiSchema { Type = "integer" }
                },
                ["X-RateLimit-Reset"] = new OpenApiHeader
                {
                    Description = "The time at which the current rate limit window resets",
                    Schema = new OpenApiSchema { Type = "string", Format = "date-time" }
                }
            }
        });
    }
}

public class AddCustomHeaderFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Extensions.Add("x-custom-header", new Microsoft.OpenApi.Any.OpenApiString(
            "DynamoDB Processor API Documentation"));
    }
}

public class AddCustomFooterFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Extensions.Add("x-custom-footer", new Microsoft.OpenApi.Any.OpenApiString(
            "© 2024 DynamoDB Processor. All rights reserved."));
    }
} 