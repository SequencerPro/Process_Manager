using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ChangeOrderApprover : BaseEntity
{
    public Guid ChangeOrderId { get; set; }

    public ChangeOrder ChangeOrder { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Role { get; set; }

    public ApproverDecision Decision { get; set; } = ApproverDecision.Pending;

    public DateTime? DecidedAt { get; set; }

    public string? Comments { get; set; }
}
