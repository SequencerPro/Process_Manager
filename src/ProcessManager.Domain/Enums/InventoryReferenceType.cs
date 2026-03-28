namespace ProcessManager.Domain.Enums;

/// <summary>
/// Types of entities that can be referenced by an inventory transaction.
/// </summary>
public enum InventoryReferenceType
{
    Job,
    PickList,
    ManualAdjustment
}
