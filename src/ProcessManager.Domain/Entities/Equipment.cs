namespace ProcessManager.Domain.Entities;

/// <summary>
/// A specific machine, workstation, tool, or facility resource.
/// </summary>
public class Equipment : BaseEntity
{
    /// <summary>Short identifier (e.g. "CNC-01", "CMM-3").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string? Location { get; set; }

    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public string? SerialNumber { get; set; }

    /// <summary>Used to drive age-based PM triggers.</summary>
    public DateTime? InstallDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public EquipmentCategory Category { get; set; } = null!;
    public ICollection<DowntimeRecord> DowntimeRecords { get; set; } = new List<DowntimeRecord>();
    public ICollection<MaintenanceTrigger> MaintenanceTriggers { get; set; } = new List<MaintenanceTrigger>();
    public ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();
}
