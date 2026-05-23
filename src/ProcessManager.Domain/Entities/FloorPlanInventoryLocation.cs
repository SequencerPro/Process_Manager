namespace ProcessManager.Domain.Entities;

public class FloorPlanInventoryLocation : BaseEntity
{
    public Guid FloorPlanId { get; set; }
    public string PlacementId { get; set; } = "";
    public Guid StorageLocationId { get; set; }

    // Navigation
    public FloorPlan FloorPlan { get; set; } = null!;
    public StorageLocation StorageLocation { get; set; } = null!;

    /// <summary>
    /// Kinds this location is explicitly designated to supply (Phase 37 "designed
    /// flow" mode). Independent of live on-hand stock.
    /// </summary>
    public ICollection<FloorPlanInventoryLocationKind> DesignatedKinds { get; set; }
        = new List<FloorPlanInventoryLocationKind>();
}
