namespace ProcessManager.Domain.Entities;

/// <summary>
/// Append-only record of a webhook delivery attempt.
/// Does NOT extend BaseEntity — manages its own Id and timestamps.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to the subscription that triggered this delivery.</summary>
    public Guid WebhookSubscriptionId { get; set; }

    /// <summary>Event type, e.g. "job.completed".</summary>
    public string EventType { get; set; } = "";

    /// <summary>Full JSON payload sent to the webhook endpoint.</summary>
    public string Payload { get; set; } = "";

    /// <summary>HTTP status code returned by the endpoint, or null if the request failed before a response.</summary>
    public int? StatusCode { get; set; }

    /// <summary>Response body from the endpoint (truncated to 2000 chars).</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Error message for network/timeout failures.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Whether this attempt was successful (2xx status code).</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Attempt number (1 = first try, 2+ = retries).</summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>When the delivery attempt was created/queued (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the delivery was successfully completed (UTC). Null if failed.</summary>
    public DateTime? DeliveredAt { get; set; }

    // Navigation
    public WebhookSubscription Subscription { get; set; } = null!;
}
