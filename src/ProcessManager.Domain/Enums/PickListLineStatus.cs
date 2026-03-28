namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a single PickList line.
/// </summary>
public enum PickListLineStatus
{
    Pending,
    Picked,
    ShortShipped,
    Consumed
}
