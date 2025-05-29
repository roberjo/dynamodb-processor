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
[ApiVersion("1.0")]
[Produces("application/json")]
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
    /// Executes a basic query against DynamoDB.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <returns>Query results matching the criteria</returns>
    /// <response code="200">Returns the query results</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QueryResponse>> Query([FromBody] QueryRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationErrorResponse
            {
                Errors = validationResult.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
            });
        }

        try
        {
            var response = await _dynamoDbService.QueryRecordsAsync(request);
            return Ok(response);
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
    [ProducesResponseType(typeof(PaginatedQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
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
            return StatusCode(429, new ErrorResponse
            {
                Message = "Service temporarily unavailable. Please try again later."
            });
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
            _logger.LogError(ex, "Error executing paginated query");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while processing your request."
            });
        }
    }

    /// <summary>
    /// Executes a query and retrieves all results up to the specified maximum.
    /// </summary>
    /// <param name="request">The query request containing search criteria</param>
    /// <param name="maxItems">Maximum number of items to retrieve (default: 10000)</param>
    /// <returns>All query results up to the maximum limit</returns>
    /// <response code="200">Returns all query results up to the maximum limit</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the table or index does not exist</response>
    /// <response code="429">If the request is throttled</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("all")]
    [ProducesResponseType(typeof(PaginatedQueryResponse), StatusCodes.Status200OK)]
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
            return StatusCode(429, new ErrorResponse
            {
                Message = "Service temporarily unavailable. Please try again later."
            });
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
} 