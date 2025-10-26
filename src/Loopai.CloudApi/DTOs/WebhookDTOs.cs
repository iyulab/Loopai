using Loopai.Core.Models;

namespace Loopai.CloudApi.DTOs;

/// <summary>
/// Request to create a webhook subscription.
/// </summary>
public record CreateWebhookSubscriptionRequest
{
    public required string Url { get; init; }
    public required List<WebhookEventType> EventTypes { get; init; }
    public string? Secret { get; init; }
    public int? MaxRetries { get; init; }
    public int? TimeoutSeconds { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}
