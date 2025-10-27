namespace Loopai.Core.Plugins;

/// <summary>
/// Interface for webhook handler plugins that process webhook events.
/// </summary>
public interface IWebhookHandlerPlugin : IPlugin
{
    /// <summary>
    /// Event types that this handler supports.
    /// </summary>
    IEnumerable<WebhookEventType> SupportedEvents { get; }

    /// <summary>
    /// Handles a webhook event.
    /// </summary>
    /// <param name="webhookEvent">Webhook event to handle.</param>
    /// <param name="context">Handler context with configuration and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task HandleAsync(
        WebhookEvent webhookEvent,
        WebhookHandlerContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates handler configuration.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    bool ValidateConfiguration(IReadOnlyDictionary<string, object> configuration);
}
