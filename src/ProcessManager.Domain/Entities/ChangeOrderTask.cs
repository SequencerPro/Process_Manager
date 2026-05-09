using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ChangeOrderTask : BaseEntity
{
    public Guid ChangeOrderId { get; set; }

    public ChangeOrder ChangeOrder { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AssigneeUserId { get; set; }

    public string? AssigneeDisplayName { get; set; }

    public DateTime? DueDate { get; set; }

    public ActionItemStatus Status { get; set; } = ActionItemStatus.Open;

    public DateTime? CompletedAt { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? Notes { get; set; }
}
