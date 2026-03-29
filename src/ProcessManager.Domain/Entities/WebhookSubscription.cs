namespace ProcessManager.Domain.Entities;

/// <summary>
/// A registered webhook endpoint that receives event notifications.
/// Extends BaseEntity for standard audit fields.
/// </summary>
public class WebhookSubscription : BaseEntity
{
    /// <summary>Target URL to POST events to.</summary>
    public string Url { get; set; } = "";

    /// <summary>Optional HMAC-SHA256 secret for signing payloads (X-Webhook-Signature header).</summary>
    public string? Secret { get; set; }

    /// <summary>Comma-separated list of event types this subscription listens to, e.g. "job.created,job.completed".</summary>
    public string EventTypes { get; set; } = "";

    /// <summary>Human-readable description, e.g. "Slack quality alerts".</summary>
    public string? Description { get; set; }

    /// <summary>Whether this subscription is active and should receive events.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
