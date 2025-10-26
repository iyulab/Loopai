using FluentValidation;
using Loopai.CloudApi.DTOs;

namespace Loopai.CloudApi.Validators;

/// <summary>
/// Validator for CreateTaskRequest.
/// </summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Task name is required")
            .MaximumLength(200)
            .WithMessage("Task name must not exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9_\-\.]+$")
            .WithMessage("Task name must contain only alphanumeric characters, underscores, hyphens, and periods");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Task description is required")
            .MaximumLength(5000)
            .WithMessage("Task description must not exceed 5000 characters");

        RuleFor(x => x.InputSchema)
            .NotNull()
            .WithMessage("Input schema is required");

        RuleFor(x => x.OutputSchema)
            .NotNull()
            .WithMessage("Output schema is required");

        RuleFor(x => x.AccuracyTarget)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Accuracy target must be between 0.0 and 1.0");

        RuleFor(x => x.LatencyTargetMs)
            .InclusiveBetween(1, 60000)
            .WithMessage("Latency target must be between 1 and 60000 milliseconds");

        RuleFor(x => x.SamplingRate)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Sampling rate must be between 0.0 and 1.0");

        RuleFor(x => x.Examples)
            .NotNull()
            .WithMessage("Examples list cannot be null (use empty list if no examples)");
    }
}
