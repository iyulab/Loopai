using Loopai.Core.Models;

namespace Loopai.Core.Interfaces;

/// <summary>
/// Service for managing webhook subscriptions and event delivery.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Subscribe to webhook events.
    /// </summary>
    Task<WebhookSubscription> SubscribeAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from webhook events.
    /// </summary>
    Task<bool> UnsubscribeAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active subscriptions.
    /// </summary>
    Task<IEnumerable<WebhookSubscription>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish an event to all matching subscribers.
    /// </summary>
    Task PublishEventAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get delivery history for a subscription.
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(Guid subscriptionId, int limit = 100, CancellationToken cancellationToken = default);
}
