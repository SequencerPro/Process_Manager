namespace ProcessManager.Domain.Entities;

public class TenantSubscription : BaseEntity
{
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionPlan PlanCode { get; set; } = SubscriptionPlan.Trial;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? GraceEndsAt { get; set; }
    public int FailedPaymentCount { get; set; }
    public string? LastStripeEventId { get; set; }
    public string? CouponCode { get; set; }
}

public enum SubscriptionPlan
{
    Trial,
    Starter,
    Professional,
    Enterprise
}

public enum SubscriptionStatus
{
    Trial,
    Active,
    PastDue,
    Suspended,
    Cancelled
}
