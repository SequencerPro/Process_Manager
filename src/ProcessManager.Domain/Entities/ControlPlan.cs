namespace ProcessManager.Domain.Entities;

public class ControlPlan : BaseEntity
{
    public Guid ProcessId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int ProcessVersion { get; set; } = 1;
    public bool IsStale { get; set; } = false;
    public string? StalenessClearedBy { get; set; }
    public DateTime? StalenessClearedAt { get; set; }
    public string? StalenessClearanceNotes { get; set; }

    // Navigation
    public Process Process { get; set; } = null!;
    public ICollection<ControlPlanEntry> Entries { get; set; } = new List<ControlPlanEntry>();
}
