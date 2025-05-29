using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using DynamoDBProcessor.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AwsCloudWatch;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.AwsCloudWatch(
        logGroup: "/dynamodb-processor/logs",
        logStreamPrefix: DateTime.UtcNow.ToString("yyyy/MM/dd"),
        region: Amazon.RegionEndpoint.USEast1)
    .CreateLogger();

builder.Host.UseSerilog();

// Add AWS services
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonCloudWatch>();

// Add services to the container
builder.Services.AddControllers()
    .AddFluentValidation(fv => 
    {
        fv.RegisterValidatorsFromAssemblyContaining<Program>();
        fv.DisableDataAnnotationsValidation = true;
    });

// Configure API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new MediaTypeApiVersionReader("x-api-version")
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DynamoDB Processor API",
        Version = "v1",
        Description = "API for processing DynamoDB records"
    });
    c.ExampleFilters();
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

// Register services
builder.Services.AddSingleton<IDynamoDBService, DynamoDBService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<IQueryExecutor, QueryExecutor>();

// Configure response caching
builder.Services.AddResponseCaching();

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DynamoDB Processor API v1");
    });
}

app.UseHttpsRedirection();
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run(); 