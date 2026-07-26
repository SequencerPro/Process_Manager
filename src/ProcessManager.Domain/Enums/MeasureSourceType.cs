namespace ProcessManager.Domain.Enums;

/// <summary>
/// Where an ObjectiveMeasure's current value comes from. Manual measures read their
/// latest MeasureSnapshot; all other sources are resolved live from operational data
/// by MeasureValueResolver.
/// </summary>
public enum MeasureSourceType
{
    Manual,

    /// <summary>Good items ÷ total items across jobs of the linked Process (percent).</summary>
    ProcessYield,

    /// <summary>Mean step-template maturity score (0–100) across the linked Process.</summary>
    ProcessMaturity,

    /// <summary>Completed workorders of the linked Workflow in the rolling window.</summary>
    WorkflowThroughput,

    /// <summary>Latest Cpk of the linked SpcChart.</summary>
    SpcCapability,

    /// <summary>30-day ActionItem close rate (percent).</summary>
    ActionCloseRate,

    /// <summary>Count of non-conformances awaiting disposition.</summary>
    OpenNonConformances,

    /// <summary>Percent of required competencies that are current.</summary>
    TrainingCompliance,

    /// <summary>OEE percent for linked Equipment or plant-wide.</summary>
    Oee,

    /// <summary>Total cost of quality in the rolling window.</summary>
    QualityCost,

    /// <summary>Aggregated numeric prompt value via the analytics path.</summary>
    PromptMetric
}
