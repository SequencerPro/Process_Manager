namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a Job.
/// </summary>
public enum JobStatus
{
    Created,
    InProgress,
    Completed,
    Cancelled,
    OnHold
}
