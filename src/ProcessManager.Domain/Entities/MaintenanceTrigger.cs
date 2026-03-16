using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A time-based or usage-based maintenance rule that auto-generates MaintenanceTasks.
/// </summary>
public class MaintenanceTrigger : BaseEntity
{
    public Guid EquipmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public MaintenanceTriggerType TriggerType { get; set; }

    /// <summary>Days between tasks (TimeBased triggers).</summary>
    public int? IntervalDays { get; set; }

    /// <summary>Step executions on this equipment between tasks (UsageBased triggers).</summary>
    public int? IntervalUsageCycles { get; set; }

    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>When the next task should be generated.</summary>
    public DateTime? NextDueAt { get; set; }

    /// <summary>How many days before NextDueAt to surface the upcoming task.</summary>
    public int AdvanceNoticeDays { get; set; } = 7;

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
