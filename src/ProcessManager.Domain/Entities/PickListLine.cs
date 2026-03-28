using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single line on a pick list — one Kind to be picked from a suggested location.
/// The specific Item is assigned at pick time (late binding).
/// </summary>
public class PickListLine : BaseEntity
{
    /// <summary>Parent pick list.</summary>
    public Guid PickListId { get; set; }

    /// <summary>The Kind of item required (blueprint reference).</summary>
    public Guid KindId { get; set; }

    /// <summary>Specific item assigned when operator confirms pick (null until picked).</summary>
    public Guid? ItemId { get; set; }

    /// <summary>Suggested location with available stock (null if no stock found).</summary>
    public Guid? SourceLocationId { get; set; }

    /// <summary>Quantity required from the process input port definition.</summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>Quantity the operator confirmed picking.</summary>
    public decimal PickedQuantity { get; set; }

    /// <summary>Quantity confirmed consumed during execution.</summary>
    public decimal ConsumedQuantity { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public PickListLineStatus Status { get; set; } = PickListLineStatus.Pending;

    /// <summary>Free-form notes.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public PickList PickList { get; set; } = null!;
    public Kind Kind { get; set; } = null!;
    public Item? Item { get; set; }
    public StorageLocation? SourceLocation { get; set; }
}
