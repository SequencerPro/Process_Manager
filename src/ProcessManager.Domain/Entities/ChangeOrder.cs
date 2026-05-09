using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ChangeOrder : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public ChangeOrderType Type { get; set; } = ChangeOrderType.ProcessChange;

    public ChangeOrderPriority Priority { get; set; } = ChangeOrderPriority.Routine;

    public ChangeOrderStatus Status { get; set; } = ChangeOrderStatus.Draft;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Justification { get; set; }

    public string RequestedByUserId { get; set; } = string.Empty;

    public string RequestedByDisplayName { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public DateTime? TargetImplementationDate { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<ChangeOrderImpact> Impacts { get; set; } = new List<ChangeOrderImpact>();

    public ICollection<ChangeOrderApprover> Approvers { get; set; } = new List<ChangeOrderApprover>();

    public ICollection<ChangeOrderTask> Tasks { get; set; } = new List<ChangeOrderTask>();
}
