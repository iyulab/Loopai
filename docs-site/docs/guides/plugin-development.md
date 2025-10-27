---
title: Plugin Development
sidebar_position: 5
---

# Loopai Plugin Development Guide

This guide explains how to create custom plugins for the Loopai framework.

---

## Overview

Loopai provides three types of plugins for extensibility:

1. **Validator Plugins** - Custom validation logic for execution results
2. **Sampler Plugins** - Custom sampling strategies for quality assurance
3. **Webhook Handler Plugins** - Custom webhook event processing

All plugins implement the `IPlugin` base interface and register with the `IPluginRegistry`.

---

## Plugin Types

### 1. Validator Plugin (`IValidatorPlugin`)

Validates execution results against expected outputs or custom business rules.

**Use Cases**:
- Custom schema validation
- Business rule verification
- Domain-specific output validation
- Multi-field constraint checking

**Interface**:
```csharp
public interface IValidatorPlugin : IPlugin
{
    Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
```

**Example Implementation**:
```csharp
using Loopai.Core.Plugins;
using Loopai.Core.Models;

public class CustomBusinessRuleValidator : IValidatorPlugin
{
    public string Name => "custom-business-rule-validator";
    public string Description => "Validates output against custom business rules";
    public string Version => "1.0.0";
    public string Author => "Your Name";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;

    public Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        // Your validation logic here
        var isValid = /* custom validation */;

        return Task.FromResult(new ValidationResult
        {
            IsValid = isValid,
            ValidatorType = Name,
            Message = isValid ? "Valid" : "Invalid business rule",
            ConfidenceScore = isValid ? 1.0 : 0.0
        });
    }
}
```

### 2. Sampler Plugin (`ISamplerPlugin`)

Determines which executions should be sampled for validation.

**Use Cases**:
- Time-based sampling
- Error-rate triggered sampling
- Load-based sampling
- Pattern-based sampling

**Interface**:
```csharp
public interface ISamplerPlugin : IPlugin
{
    Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default);
}
```

**Example Implementation**:
```csharp
using Loopai.Core.Plugins;
using Loopai.Core.Models;

public class ErrorRateSamplerPlugin : ISamplerPlugin
{
    private readonly double _errorThreshold;

    public ErrorRateSamplerPlugin(double errorThreshold = 0.05)
    {
        _errorThreshold = errorThreshold;
    }

    public string Name => "error-rate-sampler";
    public string Description => $"Samples when error rate exceeds {_errorThreshold * 100}%";
    public string Version => "1.0.0";
    public string Author => "Your Name";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 50;

    public Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default)
    {
        // Calculate error rate from context
        var errorRate = /* calculate from execution history */;
        var shouldSample = errorRate > _errorThreshold;

        return Task.FromResult(new SamplingDecision
        {
            ShouldSample = shouldSample,
            Reason = $"Error rate: {errorRate:P2}",
            SamplerType = Name,
            ConfidenceScore = shouldSample ? 1.0 : 0.0
        });
    }
}
```

### 3. Webhook Handler Plugin (`IWebhookHandlerPlugin`)

Processes webhook events for integrations and notifications.

**Use Cases**:
- Slack/Teams notifications
- External system integration
- Email alerts
- Custom analytics

**Interface**:
```csharp
public interface IWebhookHandlerPlugin : IPlugin
{
    IEnumerable<WebhookEventType> SupportedEvents { get; }

    Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken = default);

    bool ValidateConfiguration(IReadOnlyDictionary<string, object> configuration);
}
```

**Example Implementation**:
```csharp
using Loopai.Core.Plugins;

public class SlackWebhookHandlerPlugin : IWebhookHandlerPlugin
{
    public string Name => "slack-webhook-handler";
    public string Description => "Sends notifications to Slack";
    public string Version => "1.0.0";
    public string Author => "Your Name";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;

    public IEnumerable<WebhookEventType> SupportedEvents => new[]
    {
        WebhookEventType.ProgramExecutionFailed,
        WebhookEventType.AccuracyThresholdBreached,
        WebhookEventType.CanaryRollback
    };

    public async Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.IsDryRun)
            return;

        var slackWebhookUrl = context.Configuration["SlackWebhookUrl"] as string;
        var message = FormatSlackMessage(webhookEvent);

        await SendToSlackAsync(slackWebhookUrl, message, cancellationToken);
    }

    public bool ValidateConfiguration(IReadOnlyDictionary<string, object> configuration)
    {
        return configuration.ContainsKey("SlackWebhookUrl") &&
               !string.IsNullOrWhiteSpace(configuration["SlackWebhookUrl"] as string);
    }

    private string FormatSlackMessage(WebhookEvent webhookEvent)
    {
        // Format message for Slack
        return $"Event: {webhookEvent.EventType} at {webhookEvent.Timestamp}";
    }

    private async Task SendToSlackAsync(string webhookUrl, string message, CancellationToken ct)
    {
        // Send to Slack API
    }
}
```

---

## Plugin Registration

### 1. Manual Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Loopai.Core.Plugins;

// In Program.cs or Startup.cs
builder.Services.AddSingleton<IPluginRegistry, PluginRegistry>();

var registry = app.Services.GetRequiredService<IPluginRegistry>();

// Register validators
registry.Register<IValidatorPlugin>(new JsonSchemaValidatorPlugin());
registry.Register<IValidatorPlugin>(new CustomBusinessRuleValidator());

// Register samplers
registry.Register<ISamplerPlugin>(new PercentageSamplerPlugin(0.1));
registry.Register<ISamplerPlugin>(new TimeBasedSamplerPlugin(TimeSpan.FromMinutes(5)));
registry.Register<ISamplerPlugin>(new ErrorRateSamplerPlugin(0.05));

// Register webhook handlers
registry.Register<IWebhookHandlerPlugin>(new SlackWebhookHandlerPlugin());
registry.Register<IWebhookHandlerPlugin>(new ConsoleWebhookHandlerPlugin());
```

### 2. Configuration-Based Registration

**appsettings.json**:
```json
{
  "Loopai": {
    "Plugins": {
      "Validators": [
        {
          "Name": "json-schema-validator",
          "Type": "validator",
          "Enabled": true,
          "Priority": 100,
          "Configuration": {}
        },
        {
          "Name": "custom-business-rule-validator",
          "Type": "validator",
          "Enabled": true,
          "Priority": 50,
          "Configuration": {
            "StrictMode": true
          }
        }
      ],
      "Samplers": [
        {
          "Name": "percentage-sampler",
          "Type": "sampler",
          "Enabled": true,
          "Priority": 100,
          "Configuration": {
            "SamplingRate": 0.1
          }
        }
      ],
      "WebhookHandlers": [
        {
          "Name": "slack-webhook-handler",
          "Type": "webhook-handler",
          "Enabled": true,
          "Priority": 100,
          "Configuration": {
            "SlackWebhookUrl": "https://hooks.slack.com/services/..."
          }
        }
      ]
    }
  }
}
```

**Code**:
```csharp
builder.Services.Configure<PluginsConfiguration>(
    builder.Configuration.GetSection("Loopai:Plugins")
);
```

---

## Plugin Lifecycle

### 1. Registration
Plugins are registered with the `IPluginRegistry` at application startup.

### 2. Discovery
Services query the registry to discover available plugins by type.

### 3. Execution
Plugins are executed in priority order (higher priority = earlier execution).

### 4. Filtering
Only enabled plugins are executed (check `IsEnabled` property).

---

## Built-In Plugins

Loopai provides several built-in plugins in `Loopai.Core.Plugins.BuiltIn`:

### Validators
- **JsonSchemaValidatorPlugin** - Validates against JSON schema and expected output

### Samplers
- **PercentageSamplerPlugin** - Samples a fixed percentage of executions
- **TimeBasedSamplerPlugin** - Samples based on time intervals

### Webhook Handlers
- **ConsoleWebhookHandlerPlugin** - Logs events to console (development/testing)

---

## Best Practices

### 1. Plugin Naming
- Use descriptive, kebab-case names: `custom-validator`, `error-rate-sampler`
- Include organization prefix for clarity: `acme-business-validator`

### 2. Versioning
- Use semantic versioning: `1.0.0`, `1.2.3`
- Increment major version for breaking changes

### 3. Error Handling
- Always handle exceptions gracefully
- Return meaningful error messages
- Log errors with sufficient context

```csharp
public async Task<ValidationResult> ValidateAsync(...)
{
    try
    {
        // Validation logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Validation failed for {PluginName}", Name);
        return new ValidationResult
        {
            IsValid = false,
            ValidatorType = Name,
            Message = $"Validation error: {ex.Message}"
        };
    }
}
```

### 4. Configuration Validation
- Always validate configuration in `ValidateConfiguration()` for webhook handlers
- Throw clear exceptions for invalid configuration during plugin construction

### 5. Performance
- Keep validation logic fast (&lt;100ms recommended)
- Use async operations for I/O-bound work
- Cache expensive computations when possible

### 6. Testing
- Write unit tests for each plugin
- Test with various input scenarios
- Mock external dependencies

---

## Advanced Topics

### Multi-Plugin Coordination

Execute multiple plugins in sequence:

```csharp
var validators = registry.List<IValidatorPlugin>();
var results = new List<ValidationResult>();

foreach (var validator in validators)
{
    var result = await validator.ValidateAsync(execution, context, ct);
    results.Add(result);

    // Stop on first failure if needed
    if (!result.IsValid && stopOnFirstFailure)
        break;
}
```

### Plugin Prioritization

Plugins execute in priority order:

```csharp
// Higher priority executes first
var criticalValidator = new CriticalValidator { Priority = 1000 };
var standardValidator = new StandardValidator { Priority = 100 };

registry.Register<IValidatorPlugin>(criticalValidator);
registry.Register<IValidatorPlugin>(standardValidator);

// Executes: criticalValidator → standardValidator
```

### Conditional Execution

Enable/disable plugins at runtime:

```csharp
var plugin = registry.Resolve<IValidatorPlugin>("my-validator");
if (plugin != null)
{
    plugin.IsEnabled = shouldEnable;
}
```

---

## Packaging and Distribution

### Create NuGet Package

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>YourCompany.Loopai.Plugins</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Custom plugins for Loopai</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Loopai.Core" Version="0.1.0" />
  </ItemGroup>
</Project>
```

```bash
dotnet pack -c Release
```

### Install Plugin Package

```bash
dotnet add package YourCompany.Loopai.Plugins
```

---

## Example: Complete Custom Plugin

```csharp
using Loopai.Core.Plugins;
using Loopai.Core.Models;
using Microsoft.Extensions.Logging;

namespace MyCompany.Loopai.Plugins;

public class AnomalyDetectionSamplerPlugin : ISamplerPlugin
{
    private readonly ILogger<AnomalyDetectionSamplerPlugin> _logger;
    private readonly double _deviationThreshold;

    public AnomalyDetectionSamplerPlugin(
        ILogger<AnomalyDetectionSamplerPlugin> logger,
        double deviationThreshold = 2.0)
    {
        _logger = logger;
        _deviationThreshold = deviationThreshold;
    }

    public string Name => "anomaly-detection-sampler";
    public string Description => "Samples executions with anomalous latency patterns";
    public string Version => "1.0.0";
    public string Author => "MyCompany";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 75;

    public Task<SamplingDecision> ShouldSampleAsync(
        ExecutionRecord execution,
        SamplingContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Calculate mean and std deviation from context
            var meanLatency = /* calculate from historical data */;
            var stdDeviation = /* calculate from historical data */;

            // Check if current execution is anomalous
            var deviation = Math.Abs(execution.LatencyMs - meanLatency) / stdDeviation;
            var isAnomalous = deviation > _deviationThreshold;

            if (isAnomalous)
            {
                _logger.LogWarning(
                    "Anomalous latency detected: {Latency}ms (mean: {Mean}ms, deviation: {Deviation}σ)",
                    execution.LatencyMs,
                    meanLatency,
                    deviation
                );
            }

            return Task.FromResult(new SamplingDecision
            {
                ShouldSample = isAnomalous,
                Reason = isAnomalous
                    ? $"Anomalous latency: {deviation:F2}σ from mean"
                    : "Normal latency pattern",
                SamplerType = Name,
                ConfidenceScore = isAnomalous ? deviation / 10.0 : 0.0,
                Metadata = new Dictionary<string, object>
                {
                    ["mean_latency_ms"] = meanLatency,
                    ["std_deviation_ms"] = stdDeviation,
                    ["deviation_sigma"] = deviation,
                    ["threshold_sigma"] = _deviationThreshold
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in anomaly detection sampler");
            return Task.FromResult(new SamplingDecision
            {
                ShouldSample = false,
                Reason = $"Error: {ex.Message}",
                SamplerType = Name
            });
        }
    }
}
```

---

## Support

- **Documentation**: [Loopai Docs](https://github.com/iyulab/loopai)
- **Issues**: [GitHub Issues](https://github.com/iyulab/loopai/issues)
- **Examples**: See `src/Loopai.Core/Plugins/BuiltIn/` for reference implementations

---

## License

Plugin development follows the same MIT license as Loopai core.
