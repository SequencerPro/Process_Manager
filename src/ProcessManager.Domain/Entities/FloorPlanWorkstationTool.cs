namespace ProcessManager.Domain.Entities;

public class FloorPlanWorkstationTool : BaseEntity
{
    public Guid FloorPlanWorkstationId { get; set; }
    public Guid KindId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }

    // Navigation
    public FloorPlanWorkstation FloorPlanWorkstation { get; set; } = null!;
    public Kind Kind { get; set; } = null!;
}
