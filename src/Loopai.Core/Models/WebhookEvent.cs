using System.Text.Json;

namespace Loopai.Core.Models;

/// <summary>
/// Types of events that can trigger webhook notifications.
/// </summary>
public enum WebhookEventType
{
    ProgramExecutionFailed,
    ValidationFailed,
    ProgramImproved,
    CanaryRollback,
    CanaryCompleted,
    ABTestCompleted
}

/// <summary>
/// Webhook event payload sent to subscribers.
/// </summary>
public record WebhookEvent
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required WebhookEventType EventType { get; init; }
    public required Guid TaskId { get; init; }
    public Guid? ProgramId { get; init; }
    public required string Message { get; init; }
    public JsonDocument? Data { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? Severity { get; init; } // "info", "warning", "error", "critical"
}

/// <summary>
/// Webhook subscription configuration.
/// </summary>
public record WebhookSubscription
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Url { get; init; }
    public required List<WebhookEventType> EventTypes { get; init; }
    public string? Secret { get; init; } // For HMAC signature verification
    public bool IsActive { get; init; } = true;
    public int MaxRetries { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 30;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string>? Headers { get; init; } // Custom headers
}

/// <summary>
/// Webhook delivery attempt record.
/// </summary>
public record WebhookDelivery
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid SubscriptionId { get; init; }
    public required Guid EventId { get; init; }
    public required string Url { get; init; }
    public required bool Success { get; init; }
    public int? StatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public int AttemptNumber { get; init; }
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;
    public long DurationMs { get; init; }
}
