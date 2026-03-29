using System.Threading.Channels;

namespace ProcessManager.Api.Services;

/// <summary>
/// Webhook event that has been published and is waiting for delivery.
/// </summary>
public record WebhookEvent(string EventType, object Data, DateTime PublishedAt);

/// <summary>
/// Channel-backed in-memory queue for webhook events.
/// Implements IWebhookEventPublisher for producers and exposes
/// the ChannelReader for the background delivery service consumer.
/// </summary>
public class WebhookEventQueue : IWebhookEventPublisher
{
    private readonly Channel<WebhookEvent> _channel =
        Channel.CreateBounded<WebhookEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

    /// <summary>Reader used by the background delivery service.</summary>
    public ChannelReader<WebhookEvent> Reader => _channel.Reader;

    /// <inheritdoc />
    public void Publish(string eventType, object data)
    {
        _channel.Writer.TryWrite(new WebhookEvent(eventType, data, DateTime.UtcNow));
    }
}
