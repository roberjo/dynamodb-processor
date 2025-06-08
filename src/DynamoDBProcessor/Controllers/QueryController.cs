using Microsoft.AspNetCore.Mvc;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentValidation;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using QueryRequest = DynamoDBProcessor.Models.QueryRequest;
using DynamoQueryResponse = DynamoDBProcessor.Models.DynamoQueryResponse;
using Swashbuckle.AspNetCore.Filters;

namespace DynamoDBProcessor.Controllers;

/// <summary>
/// Response classes for the QueryController
/// </summary>
public class ValidationErrorResponse
{
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Controller for handling DynamoDB query operations.
/// Provides endpoints for querying records from the DynamoDB table.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class QueryController : ControllerBase
{
    private readonly IValidator<QueryRequest> _validator;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ILogger<QueryController> _logger;
    private readonly IMetricsService _metricsService;

    /// <summary>
    /// Initializes a new instance of the QueryController.
    /// </summary>
    /// <param name="validator">Validator for query request validation</param>
    /// <param name="queryExecutor">Executor for executing queries</param>
    /// <param name="logger">Logger for logging</param>
    /// <param name="metricsService">Service for recording metrics</param>
    public QueryController(
        IValidator<QueryRequest> validator,
        IQueryExecutor queryExecutor,
        ILogger<QueryController> logger,
        IMetricsService metricsService)
    {
        _validator = validator;
        _queryExecutor = queryExecutor;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Executes a paginated query against DynamoDB.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="pageSize">Maximum number of items per page (default: 1000)</param>
    /// <param name="continuationToken">Token for retrieving the next page of results</param>
    /// <returns>Paginated query results matching the criteria</returns>
    /// <response code="200">Returns the paginated query results</response>
    /// <response code="400">If the request is invalid or continuation token is malformed</response>
    /// <response code="404">If the table or index does not exist</response>
    /// <response code="429">If the request is throttled</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("paginated")]
    [ProducesResponseType(typeof(DynamoPaginatedQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryPaginated(
        [FromBody] QueryRequest request,
        [FromQuery] int? pageSize = 1000,
        [FromQuery] string? continuationToken = null)
    {
        try
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList();

                return BadRequest(new ValidationErrorResponse { Errors = errors });
            }

            // Parse continuation token if provided
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
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid continuation token"
                    });
                }
            }

            // Execute query
            var response = await _queryExecutor.ExecuteQueryAsync(request, lastKey);

            return Ok(response);
        }
        catch (ProvisionedThroughputExceededException ex)
        {
            _logger.LogError(ex, "DynamoDB throttling error");
            return StatusCode(429);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "DynamoDB resource not found");
            return NotFound(new ErrorResponse
            {
                Message = "The requested resource was not found."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while processing your request."
            });
        }
    }

    /// <summary>
    /// Queries all records from DynamoDB based on the provided request parameters
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="maxItems">Maximum number of items to return (default: 10000)</param>
    /// <returns>A response containing all matching records</returns>
    /// <response code="200">Returns all query results</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the table or index does not exist</response>
    /// <response code="429">If the request is throttled</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("all")]
    [ProducesResponseType(typeof(DynamoPaginatedQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryAll(
        [FromBody] QueryRequest request,
        [FromQuery] int? maxItems = 10000)
    {
        try
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList();

                return BadRequest(new ValidationErrorResponse { Errors = errors });
            }

            var response = await _queryExecutor.ExecuteQueryWithPaginationAsync(
                request,
                maxItems ?? 10000);

            return Ok(response);
        }
        catch (ProvisionedThroughputExceededException ex)
        {
            _logger.LogError(ex, "DynamoDB throttling error");
            return StatusCode(429);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "Table or index not found");
            return NotFound(new ErrorResponse
            {
                Message = "The requested table or index does not exist."
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Invalid query parameters");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query with pagination");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while processing your request."
            });
        }
    }
} 