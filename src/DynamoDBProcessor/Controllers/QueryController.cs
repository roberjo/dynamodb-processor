using Microsoft.AspNetCore.Mvc;
using DynamoDBProcessor.Models;
using DynamoDBProcessor.Services;
using FluentValidation;

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

    /// <summary>
    /// Initializes a new instance of the QueryController.
    /// </summary>
    /// <param name="dynamoDbService">Service for DynamoDB operations</param>
    /// <param name="validator">Validator for query request validation</param>
    public QueryController(
        IDynamoDBService dynamoDbService,
        IValidator<QueryRequest> validator)
    {
        _dynamoDbService = dynamoDbService;
        _validator = validator;
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
} 