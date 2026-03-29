namespace ProcessManager.Api.Services;

/// <summary>
/// Publishes webhook events to an in-memory channel for asynchronous delivery.
/// </summary>
public interface IWebhookEventPublisher
{
    /// <summary>
    /// Enqueues a webhook event for delivery to all matching subscribers.
    /// This method is non-blocking and fire-and-forget.
    /// </summary>
    /// <param name="eventType">Dot-separated event type, e.g. "job.created", "nonconformance.created".</param>
    /// <param name="data">The event payload object (will be JSON-serialized).</param>
    void Publish(string eventType, object data);
}
