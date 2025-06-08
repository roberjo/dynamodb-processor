using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;

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
            TableName = "TestTable"
        };

        // Create test data in DynamoDB
        await CreateTestData();

        // Act
        var response = await _client.PostAsJsonAsync("/api/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DynamoQueryResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
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
            TableName = "TestTable"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Query_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            TableName = "TestTable",
            Limit = 5
        };

        await CreateTestData();

        // Act
        var response = await _client.PostAsJsonAsync("/api/query/paginated", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DynamoPaginatedQueryResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Query_WithRateLimitExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "testUser",
            TableName = "TestTable"
        };

        // Act - Make multiple requests in quick succession
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 100; i++) // Assuming rate limit is less than 100 requests
        {
            tasks.Add(_client.PostAsJsonAsync("/api/query", request));
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
            { "GSI2_PK", new AttributeValue { S = "#testResource" } },
            { "userId", new AttributeValue { S = "testUser" } },
            { "data", new AttributeValue { S = "test-data" } }
        };

        var putRequest = new PutItemRequest
        {
            TableName = tableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(putRequest);
    }
} 