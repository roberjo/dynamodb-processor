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
        result.IndexName.Should().Be("UserIndex");
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
        result.IndexName.Should().Be("SystemIndex");
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
        result.IndexName.Should().Be("UserSystemIndex");
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
        result.IndexName.Should().Be("UserIndex");
        result.KeyConditionExpression.Should().Be("userId = :userId");
        result.FilterExpression.Should().Be("timestamp >= :startDate AND timestamp <= :endDate");
        result.ExpressionAttributeValues.Should().ContainKey(":startDate");
        result.ExpressionAttributeValues.Should().ContainKey(":endDate");
    }

    [Fact]
    public void BuildQuery_WithResourceId_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            ResourceId = "test-resource"
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.TableName.Should().Be("DynamoDBProcessor");
        result.IndexName.Should().Be("UserIndex");
        result.KeyConditionExpression.Should().Be("userId = :userId");
        result.FilterExpression.Should().Be("resourceId = :resourceId");
        result.ExpressionAttributeValues.Should().ContainKey(":resourceId");
        result.ExpressionAttributeValues[":resourceId"].S.Should().Be("test-resource");
    }

    [Fact]
    public void BuildQuery_WithLimit_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            Limit = 50
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.Limit.Should().Be(50);
    }

    [Fact]
    public void BuildQuery_WithExclusiveStartKey_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            ExclusiveStartKey = new Dictionary<string, object>
            {
                { "userId", "test-user" },
                { "timestamp", 1234567890 }
            }
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.ExclusiveStartKey.Should().NotBeNull();
        result.ExclusiveStartKey.Should().ContainKey("userId");
        result.ExclusiveStartKey.Should().ContainKey("timestamp");
    }

    [Fact]
    public void BuildQuery_WithScanIndexForward_ReturnsCorrectQuery()
    {
        // Arrange
        var request = new QueryRequest
        {
            UserId = "test-user",
            ScanIndexForward = false
        };

        // Act
        var result = _queryBuilder.BuildQuery(request);

        // Assert
        result.Should().NotBeNull();
        result.ScanIndexForward.Should().BeFalse();
    }
} 