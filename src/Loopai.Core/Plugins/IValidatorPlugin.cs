using Loopai.Core.Models;

namespace Loopai.Core.Plugins;

/// <summary>
/// Interface for validator plugins that validate execution results.
/// </summary>
public interface IValidatorPlugin : IPlugin
{
    /// <summary>
    /// Validates an execution result against expected output or schema.
    /// </summary>
    /// <param name="execution">Execution record to validate.</param>
    /// <param name="context">Validation context with schema and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or failure.</returns>
    Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
