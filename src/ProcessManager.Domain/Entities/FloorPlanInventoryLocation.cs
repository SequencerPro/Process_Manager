namespace ProcessManager.Domain.Entities;

public class FloorPlanInventoryLocation : BaseEntity
{
    public Guid FloorPlanId { get; set; }
    public string PlacementId { get; set; } = "";
    public Guid StorageLocationId { get; set; }

    // Navigation
    public FloorPlan FloorPlan { get; set; } = null!;
    public StorageLocation StorageLocation { get; set; } = null!;
}
