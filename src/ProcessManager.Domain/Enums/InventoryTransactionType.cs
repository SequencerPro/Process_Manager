namespace ProcessManager.Domain.Enums;

/// <summary>
/// Types of inventory movement transactions.
/// </summary>
public enum InventoryTransactionType
{
    Receipt,
    Issue,
    Transfer,
    Adjustment,
    PicklistConsumption
}
