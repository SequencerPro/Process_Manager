namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a Step Execution.
/// </summary>
public enum StepExecutionStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped,
    Failed
}
