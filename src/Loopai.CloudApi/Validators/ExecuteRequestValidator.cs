using FluentValidation;
using Loopai.CloudApi.DTOs;

namespace Loopai.CloudApi.Validators;

/// <summary>
/// Validator for ExecuteRequest.
/// </summary>
public class ExecuteRequestValidator : AbstractValidator<ExecuteRequest>
{
    public ExecuteRequestValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .When(x => x.Version.HasValue)
            .WithMessage("Version must be greater than 0");

        RuleFor(x => x.Input)
            .NotNull()
            .WithMessage("Input data is required");

        RuleFor(x => x.TimeoutMs)
            .InclusiveBetween(1, 60000)
            .When(x => x.TimeoutMs.HasValue)
            .WithMessage("Timeout must be between 1 and 60000 milliseconds");
    }
}
