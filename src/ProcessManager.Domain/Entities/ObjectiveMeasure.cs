using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A quantified indicator on a StrategicObjective with a baseline, target, and RAG
/// thresholds. Manual measures read their latest MeasureSnapshot; auto-sourced measures
/// are resolved live from operational data by MeasureValueResolver.
/// </summary>
public class ObjectiveMeasure : BaseEntity
{
    public Guid ObjectiveId { get; set; }
    public StrategicObjective Objective { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Unit of measure label (e.g., "%", "$", "days").</summary>
    public string? Units { get; set; }

    public MeasureDirection Direction { get; set; } = MeasureDirection.HigherIsBetter;

    /// <summary>Starting point when the measure was adopted.</summary>
    public decimal? BaselineValue { get; set; }

    public decimal TargetValue { get; set; }

    /// <summary>At/beyond this (respecting Direction) the measure reads Green. For TargetIsBest this is an absolute deviation from target.</summary>
    public decimal? GreenThreshold { get; set; }

    /// <summary>At/beyond this (respecting Direction) the measure reads Red. For TargetIsBest this is an absolute deviation from target.</summary>
    public decimal? RedThreshold { get; set; }

    public MeasureSourceType SourceType { get; set; } = MeasureSourceType.Manual;

    /// <summary>
    /// The entity the source resolves against — a Process for ProcessYield/ProcessMaturity,
    /// a Workflow for WorkflowThroughput, etc. Null for Manual and org-wide sources.
    /// </summary>
    public Guid? SourceEntityId { get; set; }

    /// <summary>
    /// Source-specific configuration. For ProcessYield this may hold the "good" Grade id;
    /// when empty, any non-scrapped item counts as good.
    /// </summary>
    public string? SourceParameter { get; set; }

    public ICollection<MeasureSnapshot> Snapshots { get; set; } = new List<MeasureSnapshot>();
}
