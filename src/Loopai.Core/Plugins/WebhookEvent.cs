using System.Text.Json;

namespace Loopai.Core.Plugins;

/// <summary>
/// Webhook event data passed to webhook handler plugins.
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Type of webhook event.
    /// </summary>
    public required WebhookEventType EventType { get; init; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Task identifier related to this event.
    /// </summary>
    public Guid? TaskId { get; init; }

    /// <summary>
    /// Execution identifier related to this event.
    /// </summary>
    public Guid? ExecutionId { get; init; }

    /// <summary>
    /// Program identifier related to this event.
    /// </summary>
    public Guid? ProgramId { get; init; }

    /// <summary>
    /// Event payload (structured data specific to event type).
    /// </summary>
    public required JsonDocument Payload { get; init; }

    /// <summary>
    /// Event severity level.
    /// </summary>
    public EventSeverity Severity { get; init; } = EventSeverity.Info;

    /// <summary>
    /// Additional metadata for the event.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Severity level for webhook events.
/// </summary>
public enum EventSeverity
{
    /// <summary>
    /// Informational event.
    /// </summary>
    Info,

    /// <summary>
    /// Warning event requiring attention.
    /// </summary>
    Warning,

    /// <summary>
    /// Error event indicating failure.
    /// </summary>
    Error,

    /// <summary>
    /// Critical event requiring immediate action.
    /// </summary>
    Critical
}
