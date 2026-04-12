namespace ProcessManager.Domain.Entities;

public class FloorPlan : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public FloorPlanStatus Status { get; set; } = FloorPlanStatus.Draft;
    public string LayoutJson { get; set; } = """{"canvasWidth":50000,"canvasHeight":30000,"gridSize":500,"backgroundColor":"#f5f5f5","elements":[]}""";
    public string? ThumbnailBase64 { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<FloorPlanWorkstation> Workstations { get; set; } = new List<FloorPlanWorkstation>();
    public ICollection<FloorPlanInventoryLocation> InventoryLocations { get; set; } = new List<FloorPlanInventoryLocation>();
}

public enum FloorPlanStatus { Draft, Published, Archived }
