namespace ProcessManager.Domain.Enums;

/// <summary>How an ObjectiveMeasure's value is interpreted against its target and thresholds.</summary>
public enum MeasureDirection
{
    HigherIsBetter,
    LowerIsBetter,

    /// <summary>Closest to target wins; Green/Red thresholds are absolute deviations from target.</summary>
    TargetIsBest
}
