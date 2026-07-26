namespace ProcessManager.Domain.Entities;

/// <summary>
/// A directed cause-and-effect edge between two objectives of the same scorecard —
/// the strategy-map arrows (e.g., a Learning &amp; Growth objective drives an Internal
/// Process objective).
/// </summary>
public class ObjectiveCauseLink : BaseEntity
{
    public Guid ScorecardId { get; set; }
    public Scorecard Scorecard { get; set; } = null!;

    public Guid SourceObjectiveId { get; set; }
    public StrategicObjective SourceObjective { get; set; } = null!;

    public Guid TargetObjectiveId { get; set; }
    public StrategicObjective TargetObjective { get; set; } = null!;

    /// <summary>The causal hypothesis ("better training reduces rework").</summary>
    public string? Description { get; set; }
}
