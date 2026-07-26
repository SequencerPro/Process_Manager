namespace ProcessManager.Domain.Entities;

public class PlanChangeLog : BaseEntity
{
    public SubscriptionPlan FromPlan { get; set; }
    public SubscriptionPlan ToPlan { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
}
