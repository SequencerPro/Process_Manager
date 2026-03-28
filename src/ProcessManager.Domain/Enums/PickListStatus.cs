namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for a PickList.
/// </summary>
public enum PickListStatus
{
    Open,
    PartiallyPicked,
    Picked,
    Consumed
}
