using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Defines a recurring schedule that automatically creates Workorders for a Workflow.
/// </summary>
public class WorkflowSchedule : BaseEntity
{
    /// <summary>The Workflow to create workorders for.</summary>
    public Guid WorkflowId { get; set; }

    /// <summary>Human-readable name for this schedule.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>How often this schedule recurs.</summary>
    public ScheduleRecurrenceType RecurrenceType { get; set; }

    /// <summary>How many units of RecurrenceType between firings (e.g., every 2 days). Default 1.</summary>
    public int RecurrenceInterval { get; set; } = 1;

    /// <summary>
    /// For Weekly schedules: day of week (0=Sunday, 1=Monday, … 6=Saturday).
    /// Null for all other recurrence types.
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// For Monthly/Quarterly/Annually schedules: day of the month (1–31, clamped to last day of month).
    /// Null for Hourly, Daily, and Weekly.
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>When the schedule becomes active and the first workorder may be created.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Optional date after which no more workorders are created.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Template for generated workorder names. Supports {Month}, {Year}, {Date} tokens.</summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>Whether the scheduler will fire this schedule.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>The next UTC time at which this schedule will fire. Null if schedule has expired.</summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>The last UTC time this schedule fired and created a workorder.</summary>
    public DateTime? LastRunAt { get; set; }

    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public ICollection<Workorder> Workorders { get; set; } = new List<Workorder>();
}
