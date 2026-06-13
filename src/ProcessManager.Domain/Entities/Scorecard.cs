using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Phase 50: a Balanced Scorecard (Kaplan/Norton) — the top-level strategy container.
/// Holds mission/vision plus a set of perspectives, each with strategic objectives.
/// </summary>
public class Scorecard : BaseEntity
{
    /// <summary>Auto-generated short identifier (e.g., "BSC-001").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? MissionStatement { get; set; }

    public string? VisionStatement { get; set; }

    public string? StrategyNotes { get; set; }

    public ScorecardStatus Status { get; set; } = ScorecardStatus.Draft;

    /// <summary>Owning department/role.</summary>
    public Guid? OwnerOrgUnitId { get; set; }
    public OrgUnit? OwnerOrgUnit { get; set; }

    public ICollection<ScorecardPerspective> Perspectives { get; set; } = new List<ScorecardPerspective>();

    public ICollection<ObjectiveCauseLink> CauseLinks { get; set; } = new List<ObjectiveCauseLink>();
}
