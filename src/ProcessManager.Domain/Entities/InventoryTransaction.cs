using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An immutable record of an item moving to/from a storage location.
/// On-hand quantities are computed by aggregating transactions.
/// </summary>
public class InventoryTransaction : BaseEntity
{
    /// <summary>Type of movement.</summary>
    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>The physical item being moved.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Source location (null for receipts).</summary>
    public Guid? FromLocationId { get; set; }

    /// <summary>Destination location (null for issues/consumption).</summary>
    public Guid? ToLocationId { get; set; }

    /// <summary>Quantity moved (positive value; direction determined by TransactionType).</summary>
    public decimal Quantity { get; set; }

    /// <summary>What entity triggered this transaction.</summary>
    public InventoryReferenceType? ReferenceType { get; set; }

    /// <summary>FK to the triggering entity (JobId, PickListId, etc.).</summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>Free-form notes (e.g. adjustment reason).</summary>
    public string? Notes { get; set; }

    /// <summary>When the transaction occurred (server UTC).</summary>
    public DateTime TransactedAt { get; set; }

    /// <summary>Who performed the transaction.</summary>
    public string TransactedByUserId { get; set; } = string.Empty;

    // Navigation properties
    public Item Item { get; set; } = null!;
    public StorageLocation? FromLocation { get; set; }
    public StorageLocation? ToLocation { get; set; }
}
