using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A concise statement of something the strategy must achieve, living in exactly one
/// perspective. Realized by linked Processes/Workflows and by initiatives (ActionItems
/// with SourceType = StrategicObjective).
/// </summary>
public class StrategicObjective : BaseEntity
{
    public Guid PerspectiveId { get; set; }
    public ScorecardPerspective Perspective { get; set; } = null!;

    /// <summary>Auto-generated short identifier (e.g., "OBJ-001").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Accountable owner.</summary>
    public Guid? OwnerOrgUnitId { get; set; }
    public OrgUnit? OwnerOrgUnit { get; set; }

    public StrategicObjectiveStatus Status { get; set; } = StrategicObjectiveStatus.Proposed;

    public DateTime? TargetDate { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ObjectiveMeasure> Measures { get; set; } = new List<ObjectiveMeasure>();

    public ICollection<ObjectiveProcessLink> ProcessLinks { get; set; } = new List<ObjectiveProcessLink>();
}
