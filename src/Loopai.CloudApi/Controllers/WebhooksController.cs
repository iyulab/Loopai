using Loopai.CloudApi.DTOs;
using Loopai.Core.Interfaces;
using Loopai.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Loopai.CloudApi.Controllers;

/// <summary>
/// API endpoints for webhook subscription management.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to webhook events.
    /// </summary>
    /// <param name="request">Webhook subscription configuration</param>
    /// <response code="201">Subscription created successfully</response>
    /// <response code="400">Invalid subscription configuration</response>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] CreateWebhookSubscriptionRequest request)
    {
        try
        {
            var subscription = new WebhookSubscription
            {
                Url = request.Url,
                EventTypes = request.EventTypes,
                Secret = request.Secret,
                MaxRetries = request.MaxRetries ?? 3,
                TimeoutSeconds = request.TimeoutSeconds ?? 30,
                Headers = request.Headers
            };

            var created = await _webhookService.SubscribeAsync(subscription);

            return CreatedAtAction(nameof(GetSubscriptions), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_SUBSCRIPTION",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Unsubscribe from webhook events.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <response code="204">Subscription removed successfully</response>
    /// <response code="404">Subscription not found</response>
    [HttpDelete("{subscriptionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe(Guid subscriptionId)
    {
        var removed = await _webhookService.UnsubscribeAsync(subscriptionId);

        if (!removed)
        {
            return NotFound(new ErrorResponse
            {
                Code = "SUBSCRIPTION_NOT_FOUND",
                Message = $"Subscription {subscriptionId} not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all active webhook subscriptions.
    /// </summary>
    /// <response code="200">List of active subscriptions</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WebhookSubscription>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subscriptions = await _webhookService.GetSubscriptionsAsync();
        return Ok(subscriptions);
    }

    /// <summary>
    /// Get webhook delivery history for a subscription.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <response code="200">Delivery history retrieved successfully</response>
    [HttpGet("{subscriptionId}/history")]
    [ProducesResponseType(typeof(IEnumerable<WebhookDelivery>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryHistory(Guid subscriptionId, [FromQuery] int limit = 100)
    {
        var history = await _webhookService.GetDeliveryHistoryAsync(subscriptionId, limit);
        return Ok(history);
    }
}
