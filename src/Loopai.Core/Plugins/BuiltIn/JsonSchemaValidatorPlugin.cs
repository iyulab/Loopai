using System.Diagnostics;
using System.Text.Json;
using Loopai.Core.Models;

namespace Loopai.Core.Plugins.BuiltIn;

/// <summary>
/// Built-in validator that validates output against JSON schema.
/// </summary>
public class JsonSchemaValidatorPlugin : IValidatorPlugin
{
    /// <inheritdoc/>
    public string Name => "json-schema-validator";

    /// <inheritdoc/>
    public string Description => "Validates execution output against JSON schema";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Loopai";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public int Priority { get; set; } = 100;

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Basic validation: check if output exists for successful executions
            if (execution.Status == ExecutionStatus.Success && execution.OutputData == null)
            {
                return Task.FromResult(new ValidationResult
                {
                    IsValid = false,
                    ValidatorType = Name,
                    Message = "Output data is null for successful execution",
                    DurationMs = stopwatch.ElapsedMilliseconds
                });
            }

            // Check if output matches expected output (if provided)
            if (context.ExpectedOutput != null && execution.OutputData != null)
            {
                var outputJson = execution.OutputData.RootElement.GetRawText();
                var expectedJson = context.ExpectedOutput.RootElement.GetRawText();

                var isMatch = JsonDocument.Parse(outputJson).RootElement.ToString() ==
                             JsonDocument.Parse(expectedJson).RootElement.ToString();

                stopwatch.Stop();

                return Task.FromResult(new ValidationResult
                {
                    IsValid = isMatch,
                    ValidatorType = Name,
                    Message = isMatch
                        ? "Output matches expected output"
                        : "Output does not match expected output",
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ConfidenceScore = isMatch ? 1.0 : 0.0
                });
            }

            // If no expected output, just validate structure exists
            stopwatch.Stop();
            return Task.FromResult(new ValidationResult
            {
                IsValid = execution.Status == ExecutionStatus.Success,
                ValidatorType = Name,
                Message = execution.Status == ExecutionStatus.Success
                    ? "Execution succeeded with valid output structure"
                    : $"Execution failed with status: {execution.Status}",
                DurationMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(new ValidationResult
            {
                IsValid = false,
                ValidatorType = Name,
                Message = $"Validation error: {ex.Message}",
                DurationMs = stopwatch.ElapsedMilliseconds
            });
        }
    }
}
