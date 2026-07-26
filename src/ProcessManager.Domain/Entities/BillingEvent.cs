namespace ProcessManager.Domain.Entities;

public class BillingEvent : BaseEntity
{
    public string StripeEventId { get; set; } = string.Empty;
    public BillingEventType EventType { get; set; }
    public string? Description { get; set; }
    public string? RawPayload { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public enum BillingEventType
{
    TrialStarted,
    SubscriptionCreated,
    PaymentSucceeded,
    PaymentFailed,
    SubscriptionUpdated,
    SubscriptionCancelled,
    TrialExpired,
    GracePeriodStarted,
    TenantSuspended,
    TenantReactivated
}
