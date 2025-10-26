using System.Text.Json;
using Json.Schema;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;

namespace Loopai.CloudApi.Services;

/// <summary>
/// JSON Schema-based output validator.
/// </summary>
public class SchemaOutputValidator : IOutputValidator
{
    private readonly ITaskRepository _taskRepository;
    private readonly IExecutionRecordRepository _executionRepository;
    private readonly ILogger<SchemaOutputValidator> _logger;

    public SchemaOutputValidator(
        ITaskRepository taskRepository,
        IExecutionRecordRepository executionRepository,
        ILogger<SchemaOutputValidator> logger)
    {
        _taskRepository = taskRepository;
        _executionRepository = executionRepository;
        _logger = logger;
    }

    public async Task<OutputValidation> ValidateOutputAsync(
        Guid taskId,
        JsonDocument output,
        JsonDocument? expectedOutput = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get task specification
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("Task not found: {TaskId}", taskId);
                return new OutputValidation
                {
                    IsValid = false,
                    Score = 0.0,
                    Errors = new[]
                    {
                        new ValidationError
                        {
                            Type = "task_not_found",
                            Message = $"Task {taskId} not found"
                        }
                    },
                    Method = "schema"
                };
            }

            // Parse output schema
            var schemaText = task.OutputSchema.RootElement.GetRawText();
            var schema = JsonSchema.FromText(schemaText);

            // Validate against schema
            var validationResults = schema.Evaluate(output.RootElement, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            var errors = new List<ValidationError>();

            if (!validationResults.IsValid)
            {
                // Collect schema validation errors
                foreach (var detail in validationResults.Details)
                {
                    if (!detail.IsValid && detail.Errors != null)
                    {
                        foreach (var error in detail.Errors)
                        {
                            errors.Add(new ValidationError
                            {
                                Type = "schema",
                                Path = detail.InstanceLocation?.ToString(),
                                Message = error.Value,
                                Expected = detail.SchemaLocation?.ToString()
                            });
                        }
                    }
                }

                _logger.LogDebug("Output validation failed for task {TaskId}: {ErrorCount} errors",
                    taskId, errors.Count);
            }

            // If expected output provided, do exact comparison
            if (expectedOutput != null && validationResults.IsValid)
            {
                var comparisonErrors = CompareOutputs(output, expectedOutput);
                errors.AddRange(comparisonErrors);
            }

            var isValid = errors.Count == 0;
            var score = CalculateValidationScore(validationResults, errors);

            return new OutputValidation
            {
                IsValid = isValid,
                Score = score,
                Errors = errors,
                Method = expectedOutput != null ? "schema+comparison" : "schema"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating output for task {TaskId}", taskId);
            return new OutputValidation
            {
                IsValid = false,
                Score = 0.0,
                Errors = new[]
                {
                    new ValidationError
                    {
                        Type = "validation_error",
                        Message = $"Validation error: {ex.Message}"
                    }
                },
                Method = "schema"
            };
        }
    }

    public async Task<OutputValidation> ValidateExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        // Get execution record
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        if (execution == null)
        {
            _logger.LogWarning("Execution not found: {ExecutionId}", executionId);
            return new OutputValidation
            {
                IsValid = false,
                Score = 0.0,
                Errors = new[]
                {
                    new ValidationError
                    {
                        Type = "execution_not_found",
                        Message = $"Execution {executionId} not found"
                    }
                },
                Method = "schema"
            };
        }

        // Check if execution was successful
        if (execution.Status != ExecutionStatus.Success || execution.OutputData == null)
        {
            return new OutputValidation
            {
                IsValid = false,
                Score = 0.0,
                Errors = new[]
                {
                    new ValidationError
                    {
                        Type = "execution_failed",
                        Message = $"Execution status: {execution.Status}",
                        Actual = execution.Status.ToString()
                    }
                },
                Method = "schema"
            };
        }

        // Validate the output
        return await ValidateOutputAsync(
            execution.TaskId,
            execution.OutputData,
            expectedOutput: null,
            cancellationToken);
    }

    private static List<ValidationError> CompareOutputs(JsonDocument actual, JsonDocument expected)
    {
        var errors = new List<ValidationError>();

        if (!JsonElementEquals(actual.RootElement, expected.RootElement, "$", errors))
        {
            // Errors already added during comparison
        }

        return errors;
    }

    private static bool JsonElementEquals(
        JsonElement actual,
        JsonElement expected,
        string path,
        List<ValidationError> errors)
    {
        if (actual.ValueKind != expected.ValueKind)
        {
            errors.Add(new ValidationError
            {
                Type = "type_mismatch",
                Path = path,
                Message = $"Type mismatch at {path}",
                Expected = expected.ValueKind.ToString(),
                Actual = actual.ValueKind.ToString()
            });
            return false;
        }

        switch (actual.ValueKind)
        {
            case JsonValueKind.Object:
                return CompareObjects(actual, expected, path, errors);

            case JsonValueKind.Array:
                return CompareArrays(actual, expected, path, errors);

            case JsonValueKind.String:
                if (actual.GetString() != expected.GetString())
                {
                    errors.Add(new ValidationError
                    {
                        Type = "value_mismatch",
                        Path = path,
                        Message = $"String value mismatch at {path}",
                        Expected = expected.GetString(),
                        Actual = actual.GetString()
                    });
                    return false;
                }
                break;

            case JsonValueKind.Number:
                if (actual.GetRawText() != expected.GetRawText())
                {
                    errors.Add(new ValidationError
                    {
                        Type = "value_mismatch",
                        Path = path,
                        Message = $"Number value mismatch at {path}",
                        Expected = expected.GetRawText(),
                        Actual = actual.GetRawText()
                    });
                    return false;
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                if (actual.GetBoolean() != expected.GetBoolean())
                {
                    errors.Add(new ValidationError
                    {
                        Type = "value_mismatch",
                        Path = path,
                        Message = $"Boolean value mismatch at {path}",
                        Expected = expected.GetBoolean().ToString(),
                        Actual = actual.GetBoolean().ToString()
                    });
                    return false;
                }
                break;

            case JsonValueKind.Null:
                // Both are null, which is equal
                break;
        }

        return true;
    }

    private static bool CompareObjects(
        JsonElement actual,
        JsonElement expected,
        string path,
        List<ValidationError> errors)
    {
        var allMatch = true;
        var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Check for missing properties
        foreach (var expectedProp in expectedProps.Keys)
        {
            if (!actualProps.ContainsKey(expectedProp))
            {
                errors.Add(new ValidationError
                {
                    Type = "missing_property",
                    Path = $"{path}.{expectedProp}",
                    Message = $"Missing property: {expectedProp}",
                    Expected = expectedProp
                });
                allMatch = false;
            }
        }

        // Check for extra properties (optional - could be configurable)
        foreach (var actualProp in actualProps.Keys)
        {
            if (!expectedProps.ContainsKey(actualProp))
            {
                // Note: Extra properties might be acceptable in some cases
                // For strict validation, uncomment:
                // errors.Add(new ValidationError { ... });
                // allMatch = false;
            }
        }

        // Compare matching properties
        foreach (var prop in expectedProps.Keys)
        {
            if (actualProps.TryGetValue(prop, out var actualValue))
            {
                if (!JsonElementEquals(actualValue, expectedProps[prop], $"{path}.{prop}", errors))
                {
                    allMatch = false;
                }
            }
        }

        return allMatch;
    }

    private static bool CompareArrays(
        JsonElement actual,
        JsonElement expected,
        string path,
        List<ValidationError> errors)
    {
        var actualArray = actual.EnumerateArray().ToList();
        var expectedArray = expected.EnumerateArray().ToList();

        if (actualArray.Count != expectedArray.Count)
        {
            errors.Add(new ValidationError
            {
                Type = "array_length_mismatch",
                Path = path,
                Message = $"Array length mismatch at {path}",
                Expected = expectedArray.Count.ToString(),
                Actual = actualArray.Count.ToString()
            });
            return false;
        }

        var allMatch = true;
        for (int i = 0; i < actualArray.Count; i++)
        {
            if (!JsonElementEquals(actualArray[i], expectedArray[i], $"{path}[{i}]", errors))
            {
                allMatch = false;
            }
        }

        return allMatch;
    }

    private static double CalculateValidationScore(EvaluationResults schemaResults, List<ValidationError> errors)
    {
        if (schemaResults.IsValid && errors.Count == 0)
        {
            return 1.0;
        }

        if (!schemaResults.IsValid)
        {
            // Schema validation failed - low score
            return 0.0;
        }

        // Schema valid but comparison errors
        // Reduce score based on number of errors
        var errorPenalty = Math.Min(errors.Count * 0.1, 0.9);
        return Math.Max(1.0 - errorPenalty, 0.1);
    }
}
