using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ChangeOrderImpact : BaseEntity
{
    public Guid ChangeOrderId { get; set; }

    public ChangeOrder ChangeOrder { get; set; } = null!;

    public ChangeOrderImpactEntityType AffectedEntityType { get; set; }

    public Guid AffectedEntityId { get; set; }

    public string? AffectedEntityName { get; set; }

    public string? ImpactDescription { get; set; }

    public string? MitigationPlan { get; set; }
}
