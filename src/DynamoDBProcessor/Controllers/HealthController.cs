using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace DynamoDBProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IAmazonDynamoDB dynamoDbClient, ILogger<HealthController> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint that returns 200 OK if the service is running.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check that verifies connectivity to DynamoDB.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        try
        {
            // Check DynamoDB connectivity
            var response = await _dynamoDbClient.ListTablesAsync();
            
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                DynamoDB = new
                {
                    Status = "Connected",
                    TableCount = response.TableNames.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = "Service dependencies are not healthy"
            });
        }
    }
} 