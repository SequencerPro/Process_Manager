using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A scheduled or ad-hoc maintenance task for a piece of equipment.
/// </summary>
public class MaintenanceTask : BaseEntity
{
    public Guid EquipmentId { get; set; }

    /// <summary>Null for ad-hoc tasks.</summary>
    public Guid? TriggerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public MaintenanceTaskType Type { get; set; }

    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Upcoming;

    public DateTime DueDate { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CompletedBy { get; set; }

    /// <summary>Completion notes, findings, parts used.</summary>
    public string? Notes { get; set; }

    /// <summary>Downtime record created or resolved by this task.</summary>
    public Guid? LinkedDowntimeRecordId { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
    public MaintenanceTrigger? Trigger { get; set; }
}
