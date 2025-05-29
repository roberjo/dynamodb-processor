using Amazon.DynamoDBv2.Model;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using Xunit;
using FluentAssertions;

namespace DynamoDBProcessor.Tests.Services;

public class QueryBuilderTests
{
    private readonly QueryBuilder _queryBuilder;

    public QueryBuilderTests()
    {
        _queryBuilder = new QueryBuilder();
    }

    [Fact]
    public void BuildQuery_WithUserIdOnly_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.TableName.Should().Be("DynamoDBProcessor");
        result.IndexName.Should().Be("UserIdIndex");
        result.KeyConditionExpression.Should().Be("userId = :userId");
        result.ExpressionAttributeValues.Should().ContainKey(":userId");
        result.ExpressionAttributeValues[":userId"].S.Should().Be("test-user");
    }

    [Fact]
    public void BuildQuery_WithSystemIdOnly_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            SystemId = "test-system"
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.TableName.Should().Be("DynamoDBProcessor");
        result.IndexName.Should().Be("SystemIdIndex");
        result.KeyConditionExpression.Should().Be("systemId = :systemId");
        result.ExpressionAttributeValues.Should().ContainKey(":systemId");
        result.ExpressionAttributeValues[":systemId"].S.Should().Be("test-system");
    }

    [Fact]
    public void BuildQuery_WithUserIdAndSystemId_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            SystemId = "test-system"
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.TableName.Should().Be("DynamoDBProcessor");
        result.KeyConditionExpression.Should().Be("userId = :userId AND systemId = :systemId");
        result.ExpressionAttributeValues.Should().ContainKey(":userId");
        result.ExpressionAttributeValues.Should().ContainKey(":systemId");
        result.ExpressionAttributeValues[":userId"].S.Should().Be("test-user");
        result.ExpressionAttributeValues[":systemId"].S.Should().Be("test-system");
    }

    [Fact]
    public void BuildQuery_WithDateRange_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.TableName.Should().Be("DynamoDBProcessor");
        result.KeyConditionExpression.Should().Be("userId = :userId");
        result.FilterExpression.Should().Be("timestamp BETWEEN :startDate AND :endDate");
        result.ExpressionAttributeValues.Should().ContainKey(":startDate");
        result.ExpressionAttributeValues.Should().ContainKey(":endDate");
    }

    [Fact]
    public void BuildQuery_WithInvalidRequest_ThrowsArgumentException()
    {
        // Arrange
        var request = new QueryRequest();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _queryBuilder.BuildQuery(request));
    }

    [Fact]
    public void BuildQuery_WithPageSize_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            PageSize = 50
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.Limit.Should().Be(50);
    }

    [Fact]
    public void BuildQuery_WithLastEvaluatedKey_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user"
        };

        var lastEvaluatedKey = new Dictionary<string, AttributeValue>
        {
            { "userId", new AttributeValue { S = "test-user" } },
            { "timestamp", new AttributeValue { N = "1234567890" } }
        };

        // Act
        var result = _queryBuilder.BuildQuery(request, lastEvaluatedKey);

        // Assert
        result.Should().NotBeNull();
        result.ExclusiveStartKey.Should().BeEquivalentTo(lastEvaluatedKey);
    }

    [Fact]
    public void BuildQuery_WithProjectionExpression_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            ProjectionExpression = "userId, systemId, timestamp"
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.ProjectionExpression.Should().Be("userId, systemId, timestamp");
    }
} 