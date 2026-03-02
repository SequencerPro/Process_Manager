namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle states for an Item.
/// </summary>
public enum ItemStatus
{
    Available,
    InProcess,
    Consumed,
    Completed,
    Scrapped
}
