using System.Text.Json;

namespace Loopai.Core.Plugins.BuiltIn;

/// <summary>
/// Built-in webhook handler that logs events to console (for development/testing).
/// </summary>
public class ConsoleWebhookHandlerPlugin : IWebhookHandlerPlugin
{
    private readonly WebhookEventType[] _supportedEvents;

    /// <summary>
    /// Creates a new console webhook handler.
    /// </summary>
    /// <param name="supportedEvents">Event types to handle (null = all events).</param>
    public ConsoleWebhookHandlerPlugin(params WebhookEventType[]? supportedEvents)
    {
        _supportedEvents = supportedEvents ?? Enum.GetValues<WebhookEventType>();
    }

    /// <inheritdoc/>
    public string Name => "console-webhook-handler";

    /// <inheritdoc/>
    public string Description => "Logs webhook events to console for development and testing";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Loopai";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public int Priority { get; set; } = 0;

    /// <inheritdoc/>
    public IEnumerable<WebhookEventType> SupportedEvents => _supportedEvents;

    /// <inheritdoc/>
    public Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.IsDryRun)
        {
            Console.WriteLine($"[DRY-RUN] Would handle webhook event: {webhookEvent.EventType}");
            return Task.CompletedTask;
        }

        // Format event for console output
        var severityColor = webhookEvent.Severity switch
        {
            EventSeverity.Info => ConsoleColor.Cyan,
            EventSeverity.Warning => ConsoleColor.Yellow,
            EventSeverity.Error => ConsoleColor.Red,
            EventSeverity.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };

        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = severityColor;
            Console.WriteLine();
            Console.WriteLine($"╔══════════════════════════════════════════════════════════════════");
            Console.WriteLine($"║ Webhook Event: {webhookEvent.EventType}");
            Console.WriteLine($"║ Severity: {webhookEvent.Severity}");
            Console.WriteLine($"║ Event ID: {webhookEvent.EventId}");
            Console.WriteLine($"║ Timestamp: {webhookEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");

            if (webhookEvent.TaskId.HasValue)
                Console.WriteLine($"║ Task ID: {webhookEvent.TaskId.Value}");

            if (webhookEvent.ExecutionId.HasValue)
                Console.WriteLine($"║ Execution ID: {webhookEvent.ExecutionId.Value}");

            if (webhookEvent.ProgramId.HasValue)
                Console.WriteLine($"║ Program ID: {webhookEvent.ProgramId.Value}");

            Console.WriteLine($"╠══════════════════════════════════════════════════════════════════");
            Console.WriteLine($"║ Payload:");

            // Pretty-print JSON payload
            var options = new JsonSerializerOptions { WriteIndented = true };
            var payloadJson = JsonSerializer.Serialize(webhookEvent.Payload, options);
            foreach (var line in payloadJson.Split('\n'))
            {
                Console.WriteLine($"║   {line}");
            }

            if (context.RetryAttempt > 0)
            {
                Console.WriteLine($"╠══════════════════════════════════════════════════════════════════");
                Console.WriteLine($"║ Retry Attempt: {context.RetryAttempt} / {context.MaxRetries}");
            }

            Console.WriteLine($"╚══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public bool ValidateConfiguration(IReadOnlyDictionary<string, object> configuration)
    {
        // Console handler has no required configuration
        return true;
    }
}
