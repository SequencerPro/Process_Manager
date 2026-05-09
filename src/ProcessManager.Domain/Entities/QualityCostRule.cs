using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class QualityCostRule : BaseEntity
{
    public QualityCostTriggerEvent TriggerEvent { get; set; }

    public QualityCostCategory DefaultCategory { get; set; }

    public QualityCostSourceType DefaultSourceType { get; set; }

    public decimal DefaultAmount { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
