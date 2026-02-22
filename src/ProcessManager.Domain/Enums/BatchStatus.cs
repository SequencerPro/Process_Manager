namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a Batch.
/// </summary>
public enum BatchStatus
{
    Open,
    Closed,
    InProcess,
    Completed
}
