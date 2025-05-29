using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.IntegrationTests.Controllers;

public class QueryControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IAmazonDynamoDB _dynamoDbClient;

    public QueryControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _dynamoDbClient = new AmazonDynamoDBClient();
    }

    [Fact]
    public async Task Query_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        // Create test data in DynamoDB
        await CreateTestData();

        // Act
        var response = await _client.PostAsync("/api/query", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<QueryResponse>(content);
        result.Should().NotBeNull();
        result!.Records.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Query_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "", // Invalid: empty user ID
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        // Act
        var response = await _client.PostAsync("/api/query",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Query_WithRateLimitExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            SystemId = "testSystem"
        };

        // Act - Make multiple requests in quick succession
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 100; i++) // Assuming rate limit is less than 100 requests
        {
            tasks.Add(_client.PostAsync("/api/query",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    private async Task CreateTestData()
    {
        var tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME") ?? "TestTable";
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = "testSystem#" } },
            { "SK", new AttributeValue { S = $"{DateTime.UtcNow:yyyy-MM-dd}#testId" } },
            { "GS1_PK", new AttributeValue { S = "#testUser" } },
            { "GSI2_PK", new AttributeValue { S = "#testResource" } }
        };

        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(request);
    }
} 