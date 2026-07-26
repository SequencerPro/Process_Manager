using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class QualityCost : BaseEntity
{
    public QualityCostSourceType SourceType { get; set; } = QualityCostSourceType.Manual;

    public Guid? SourceEntityId { get; set; }

    public string? SourceEntityCode { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public QualityCostCategory CostCategory { get; set; } = QualityCostCategory.InternalFailure;

    public Guid? KindId { get; set; }

    public string? KindName { get; set; }

    public Guid? JobId { get; set; }

    public string? Description { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public string RecordedByDisplayName { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
}
