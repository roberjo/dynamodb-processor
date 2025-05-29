using Microsoft.AspNetCore.Mvc;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentValidation;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;

namespace DynamoDBProcessor.Controllers;

/// <summary>
/// Controller for handling DynamoDB query operations.
/// Provides endpoints for querying records from the DynamoDB table.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IDynamoDBService _dynamoDbService;
    private readonly IValidator<QueryRequest> _validator;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ILogger<QueryController> _logger;

    /// <summary>
    /// Initializes a new instance of the QueryController.
    /// </summary>
    /// <param name="dynamoDbService">Service for DynamoDB operations</param>
    /// <param name="validator">Validator for query request validation</param>
    /// <param name="queryExecutor">Executor for executing queries</param>
    /// <param name="logger">Logger for logging</param>
    public QueryController(
        IDynamoDBService dynamoDbService,
        IValidator<QueryRequest> validator,
        IQueryExecutor queryExecutor,
        ILogger<QueryController> logger)
    {
        _dynamoDbService = dynamoDbService;
        _validator = validator;
        _queryExecutor = queryExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Handles POST requests to query DynamoDB records.
    /// Validates the request and returns matching records.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <returns>
    /// - 200 OK with query results if successful
    /// - 400 Bad Request if validation fails
    /// - 500 Internal Server Error if an exception occurs
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<QueryResponse>> Query([FromBody] QueryRequest request)
    {
        // Validate the request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            // Return validation errors in a structured format
            return BadRequest(new
            {
                Errors = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
            });
        }

        try
        {
            // Execute the query and return results
            var response = await _dynamoDbService.QueryRecordsAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log the exception here
            // Return a generic error message to avoid exposing internal details
            return StatusCode(500, new { Message = "An error occurred while processing your request." });
        }
    }

    [HttpPost("paginated")]
    public async Task<IActionResult> QueryPaginated(
        [FromBody] QueryRequest request,
        [FromQuery] int? pageSize = 1000,
        [FromQuery] string? continuationToken = null)
    {
        try
        {
            Dictionary<string, AttributeValue>? lastKey = null;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                try
                {
                    var tokenBytes = Convert.FromBase64String(continuationToken);
                    lastKey = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(tokenBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid continuation token");
                    return BadRequest("Invalid continuation token");
                }
            }

            var response = await _queryExecutor.ExecuteQueryAsync(request, lastKey);

            return Ok(new
            {
                Items = response.Items,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = response.HasMoreResults,
                TotalItems = response.TotalItems
            });
        }
        catch (ProvisionedThroughputExceededException ex)
        {
            _logger.LogError(ex, "DynamoDB throttling error");
            return StatusCode(429, "Service temporarily unavailable. Please try again later.");
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "Table or index not found");
            return NotFound("The requested table or index does not exist.");
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Invalid query parameters");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing paginated query");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpPost("all")]
    public async Task<IActionResult> QueryAll(
        [FromBody] QueryRequest request,
        [FromQuery] int? maxItems = 10000)
    {
        try
        {
            var response = await _queryExecutor.ExecuteQueryWithPaginationAsync(
                request,
                maxItems ?? 10000);

            return Ok(new
            {
                Items = response.Items,
                HasMoreResults = response.HasMoreResults,
                TotalItems = response.TotalItems
            });
        }
        catch (ProvisionedThroughputExceededException ex)
        {
            _logger.LogError(ex, "DynamoDB throttling error");
            return StatusCode(429, "Service temporarily unavailable. Please try again later.");
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "Table or index not found");
            return NotFound("The requested table or index does not exist.");
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Invalid query parameters");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query with pagination");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
} 