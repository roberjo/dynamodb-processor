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
        RuleFor(x => x.UserId)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.SystemId))
            .WithMessage("Either UserId or SystemId must be provided");

        RuleFor(x => x.SystemId)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.UserId))
            .WithMessage("Either UserId or SystemId must be provided");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .When(x => x.EndDate != null)
            .WithMessage("StartDate is required when EndDate is provided");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .When(x => x.StartDate != null)
            .WithMessage("EndDate is required when StartDate is provided")
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate != null)
            .WithMessage("EndDate must be after StartDate");
    }
} 