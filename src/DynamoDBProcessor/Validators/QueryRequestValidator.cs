using FluentValidation;
using DynamoDBProcessor.Models;

namespace DynamoDBProcessor.Validators;

/// <summary>
/// Validator for QueryRequest objects using FluentValidation.
/// Ensures that all query parameters meet the required criteria.
/// </summary>
public class QueryRequestValidator : AbstractValidator<QueryRequest>
{
    public QueryRequestValidator()
    {
        // Validate UserId: required and not too long
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(100);

        // Validate SystemId: required and not too long
        RuleFor(x => x.SystemId)
            .NotEmpty()
            .MaximumLength(100);

        // Validate ResourceId: required and not too long
        RuleFor(x => x.ResourceId)
            .NotEmpty()
            .MaximumLength(100);

        // Validate StartDate:
        // - Required
        // - Must be before or equal to EndDate
        // - Cannot be more than 1 year in the past
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .LessThanOrEqualTo(x => x.EndDate)
            .Must(date => date.Date >= DateTime.UtcNow.AddYears(-1).Date)
            .WithMessage("Start date cannot be more than 1 year in the past");

        // Validate EndDate:
        // - Required
        // - Must be after or equal to StartDate
        // - Cannot be in the future
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.StartDate)
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("End date cannot be in the future");
    }
} 