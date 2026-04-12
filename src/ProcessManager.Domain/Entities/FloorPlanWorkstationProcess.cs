namespace ProcessManager.Domain.Entities;

public class FloorPlanWorkstationProcess : BaseEntity
{
    public Guid FloorPlanWorkstationId { get; set; }
    public Guid ProcessId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public FloorPlanWorkstation FloorPlanWorkstation { get; set; } = null!;
    public Process Process { get; set; } = null!;
}
