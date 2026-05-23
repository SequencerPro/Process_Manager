namespace ProcessManager.Domain.Entities;

/// <summary>
/// Designates that an inventory location is intended to supply a particular Kind
/// (Phase 37 "designed flow" mode). Lets the factory model show planned material
/// routes independently of whatever stock happens to be on hand today.
/// </summary>
public class FloorPlanInventoryLocationKind : BaseEntity
{
    public Guid FloorPlanInventoryLocationId { get; set; }
    public Guid KindId { get; set; }

    // Navigation
    public FloorPlanInventoryLocation FloorPlanInventoryLocation { get; set; } = null!;
    public Kind Kind { get; set; } = null!;
}
