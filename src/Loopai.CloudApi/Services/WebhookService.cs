using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;

namespace Loopai.CloudApi.Services;

/// <summary>
/// Service for managing webhook subscriptions and event delivery with retry logic.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly ConcurrentDictionary<Guid, WebhookSubscription> _subscriptions = new();
    private readonly ConcurrentBag<WebhookDelivery> _deliveryHistory = new();

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<WebhookSubscription> SubscribeAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(subscription.Url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid webhook URL", nameof(subscription));
        }

        if (subscription.EventTypes == null || subscription.EventTypes.Count == 0)
        {
            throw new ArgumentException("At least one event type must be specified", nameof(subscription));
        }

        _subscriptions[subscription.Id] = subscription;
        _logger.LogInformation("Webhook subscription created: {SubscriptionId} for {EventCount} event types",
            subscription.Id, subscription.EventTypes.Count);

        return Task.FromResult(subscription);
    }

    public Task<bool> UnsubscribeAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var removed = _subscriptions.TryRemove(subscriptionId, out _);
        if (removed)
        {
            _logger.LogInformation("Webhook subscription removed: {SubscriptionId}", subscriptionId);
        }
        return Task.FromResult(removed);
    }

    public Task<IEnumerable<WebhookSubscription>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<WebhookSubscription>>(_subscriptions.Values.Where(s => s.IsActive).ToList());
    }

    public async Task PublishEventAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var matchingSubscriptions = _subscriptions.Values
            .Where(s => s.IsActive && s.EventTypes.Contains(webhookEvent.EventType))
            .ToList();

        if (matchingSubscriptions.Count == 0)
        {
            _logger.LogDebug("No active subscriptions for event type {EventType}", webhookEvent.EventType);
            return;
        }

        _logger.LogInformation("Publishing event {EventId} ({EventType}) to {SubscriptionCount} subscribers",
            webhookEvent.Id, webhookEvent.EventType, matchingSubscriptions.Count);

        // Deliver to all matching subscriptions in parallel
        var deliveryTasks = matchingSubscriptions.Select(subscription =>
            DeliverEventAsync(subscription, webhookEvent, cancellationToken));

        await Task.WhenAll(deliveryTasks);
    }

    public Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(Guid subscriptionId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var history = _deliveryHistory
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.AttemptedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IEnumerable<WebhookDelivery>>(history);
    }

    private async Task DeliverEventAsync(WebhookSubscription subscription, WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var maxRetries = subscription.MaxRetries;
        var attemptNumber = 1;

        while (attemptNumber <= maxRetries)
        {
            try
            {
                var delivery = await AttemptDeliveryAsync(subscription, webhookEvent, attemptNumber, cancellationToken);
                _deliveryHistory.Add(delivery);

                if (delivery.Success)
                {
                    _logger.LogInformation("Webhook delivered successfully to {Url} (attempt {Attempt})",
                        subscription.Url, attemptNumber);
                    return;
                }

                if (attemptNumber < maxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delayMs = (int)Math.Pow(2, attemptNumber - 1) * 1000;
                    await Task.Delay(delayMs, cancellationToken);
                }

                attemptNumber++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering webhook to {Url} (attempt {Attempt})",
                    subscription.Url, attemptNumber);

                _deliveryHistory.Add(new WebhookDelivery
                {
                    SubscriptionId = subscription.Id,
                    EventId = webhookEvent.Id,
                    Url = subscription.Url,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AttemptNumber = attemptNumber,
                    DurationMs = 0
                });

                if (attemptNumber >= maxRetries)
                {
                    break;
                }

                attemptNumber++;
            }
        }

        _logger.LogWarning("Failed to deliver webhook to {Url} after {Attempts} attempts",
            subscription.Url, maxRetries);
    }

    private async Task<WebhookDelivery> AttemptDeliveryAsync(
        WebhookSubscription subscription,
        WebhookEvent webhookEvent,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(subscription.TimeoutSeconds);

        try
        {
            // Serialize event payload
            var payload = JsonSerializer.Serialize(webhookEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            // Add custom headers
            if (subscription.Headers != null)
            {
                foreach (var (key, value) in subscription.Headers)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            // Add HMAC signature if secret is configured
            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var signature = ComputeHmacSignature(payload, subscription.Secret);
                request.Headers.Add("X-Loopai-Signature", signature);
            }

            // Add event metadata headers
            request.Headers.Add("X-Loopai-Event-Id", webhookEvent.Id.ToString());
            request.Headers.Add("X-Loopai-Event-Type", webhookEvent.EventType.ToString());
            request.Headers.Add("X-Loopai-Delivery-Attempt", attemptNumber.ToString());

            // Send request
            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var success = response.IsSuccessStatusCode;

            return new WebhookDelivery
            {
                SubscriptionId = subscription.Id,
                EventId = webhookEvent.Id,
                Url = subscription.Url,
                Success = success,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody.Length > 1000 ? responseBody[..1000] : responseBody,
                ErrorMessage = success ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                AttemptNumber = attemptNumber,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new WebhookDelivery
            {
                SubscriptionId = subscription.Id,
                EventId = webhookEvent.Id,
                Url = subscription.Url,
                Success = false,
                ErrorMessage = ex.Message,
                AttemptNumber = attemptNumber,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
