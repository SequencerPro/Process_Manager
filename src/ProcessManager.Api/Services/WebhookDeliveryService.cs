using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Services;

/// <summary>
/// Background service that consumes webhook events from the in-memory channel,
/// matches them to active subscriptions, and delivers HTTP POST requests.
/// Retries up to 3 times with exponential backoff on failure.
/// </summary>
public class WebhookDeliveryService : BackgroundService
{
    private readonly WebhookEventQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public WebhookDeliveryService(
        WebhookEventQueue queue,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookDeliveryService started.");

        await foreach (var evt in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessEventAsync(evt, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook event {EventType}.", evt.EventType);
            }
        }
    }

    private async Task ProcessEventAsync(WebhookEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();

        // Find matching active subscriptions
        var subscriptions = await db.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var matchingSubscriptions = subscriptions
            .Where(s => MatchesEventType(s.EventTypes, evt.EventType))
            .ToList();

        if (!matchingSubscriptions.Any())
            return;

        var payloadJson = JsonSerializer.Serialize(new
        {
            eventType = evt.EventType,
            publishedAt = evt.PublishedAt,
            data = evt.Data,
        }, _json);

        foreach (var sub in matchingSubscriptions)
        {
            await DeliverWithRetriesAsync(db, sub, evt.EventType, payloadJson, ct);
        }
    }

    private async Task DeliverWithRetriesAsync(
        ProcessManagerDbContext db,
        WebhookSubscription sub,
        string eventType,
        string payloadJson,
        CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var delivery = new WebhookDelivery
            {
                WebhookSubscriptionId = sub.Id,
                EventType = eventType,
                Payload = payloadJson,
                AttemptNumber = attempt,
            };

            try
            {
                var client = _httpClientFactory.CreateClient("Webhooks");
                client.Timeout = TimeSpan.FromSeconds(10);

                var request = new HttpRequestMessage(HttpMethod.Post, sub.Url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json"),
                };

                // Sign the payload with HMAC-SHA256 if a secret is configured
                if (!string.IsNullOrEmpty(sub.Secret))
                {
                    var signature = ComputeHmacSignature(payloadJson, sub.Secret);
                    request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                }

                request.Headers.Add("X-Webhook-Event", eventType);

                var response = await client.SendAsync(request, ct);
                delivery.StatusCode = (int)response.StatusCode;
                delivery.ResponseBody = Truncate(await response.Content.ReadAsStringAsync(ct), 2000);
                delivery.IsSuccess = response.IsSuccessStatusCode;

                if (delivery.IsSuccess)
                    delivery.DeliveredAt = DateTime.UtcNow;

                db.WebhookDeliveries.Add(delivery);
                await db.SaveChangesAsync(ct);

                if (delivery.IsSuccess)
                {
                    _logger.LogDebug("Webhook delivered to {Url} for {Event} (attempt {Attempt}).",
                        sub.Url, eventType, attempt);
                    return; // Success — stop retrying
                }
            }
            catch (Exception ex)
            {
                delivery.ErrorMessage = Truncate(ex.Message, 500);
                delivery.IsSuccess = false;

                db.WebhookDeliveries.Add(delivery);
                await db.SaveChangesAsync(ct);

                _logger.LogWarning(ex, "Webhook delivery failed to {Url} for {Event} (attempt {Attempt}).",
                    sub.Url, eventType, attempt);
            }

            // Exponential backoff before retry (1s, 4s)
            if (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>Checks if a comma-separated subscription event type list matches an event.</summary>
    private static bool MatchesEventType(string subscriptionEventTypes, string eventType)
    {
        if (string.IsNullOrWhiteSpace(subscriptionEventTypes))
            return false;

        var types = subscriptionEventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return types.Any(t =>
            t == "*" ||
            string.Equals(t, eventType, StringComparison.OrdinalIgnoreCase) ||
            (t.EndsWith(".*") && eventType.StartsWith(t[..^2], StringComparison.OrdinalIgnoreCase)));
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
