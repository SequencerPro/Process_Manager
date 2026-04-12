namespace ProcessManager.Domain.Entities;

public class FloorPlanWorkstation : BaseEntity
{
    public Guid FloorPlanId { get; set; }
    public string PlacementId { get; set; } = "";
    public Guid? EquipmentId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Guid? StorageLocationId { get; set; }

    // Navigation
    public FloorPlan FloorPlan { get; set; } = null!;
    public Equipment? Equipment { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public ICollection<FloorPlanWorkstationProcess> Processes { get; set; } = new List<FloorPlanWorkstationProcess>();
    public ICollection<FloorPlanWorkstationTool> Tools { get; set; } = new List<FloorPlanWorkstationTool>();
}
