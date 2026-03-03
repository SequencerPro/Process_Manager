namespace ProcessManager.Domain.Enums;

/// <summary>
/// Which specification boundary was breached (or which fail trigger caused an NC).
/// </summary>
public enum LimitType
{
    /// <summary>Numeric value was below the lower specification limit.</summary>
    LSL,

    /// <summary>Numeric value was above the upper specification limit.</summary>
    USL,

    /// <summary>A PassFail prompt was answered Fail.</summary>
    FailResult
}
