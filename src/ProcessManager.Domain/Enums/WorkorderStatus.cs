namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a Workorder.
/// </summary>
public enum WorkorderStatus
{
    Created,
    InProgress,
    Completed,
    Cancelled
}
