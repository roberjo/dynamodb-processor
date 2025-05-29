using DynamoDBProcessor.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new MediaTypeApiVersionReader("x-api-version"));
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add Swagger documentation
builder.Services.AddSwaggerDocumentation();

// Add DynamoDB processor services
builder.Services.AddDynamoDBProcessorServices();

// Add AWS services
builder.Services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
builder.Services.AddAWSService<Amazon.CloudWatch.IAmazonCloudWatch>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DynamoDB Processor API V1");
        c.RoutePrefix = string.Empty; // Serve the Swagger UI at the app's root

        // Custom styling
        c.InjectStylesheet("/swagger-ui/custom.css");
        c.InjectJavascript("/swagger-ui/custom.js");

        // UI customization
        c.DocExpansion(DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.ShowCommonExtensions();
        c.EnableValidator();
        c.SupportedSubmitMethods(new[] { SubmitMethod.Get, SubmitMethod.Post });
        c.UseRequestInterceptor("(req) => { req.headers['x-custom-header'] = 'custom-value'; return req; }");
        c.UseResponseInterceptor("(res) => { console.log('Response:', res); return res; }");

        // OAuth2 configuration
        c.OAuthClientId("swagger-ui");
        c.OAuthClientSecret("swagger-ui-secret");
        c.OAuthRealm("swagger-ui");
        c.OAuthAppName("Swagger UI");
        c.OAuthScopeSeparator(" ");
        c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
        {
            { "audience", "https://api.example.com" }
        });
    });

    // Serve custom CSS and JS files
    app.UseStaticFiles();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run(); 