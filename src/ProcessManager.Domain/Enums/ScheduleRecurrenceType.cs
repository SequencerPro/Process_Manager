namespace ProcessManager.Domain.Enums;

/// <summary>
/// Defines how frequently a WorkflowSchedule recurs.
/// Stored as string in the database.
/// </summary>
public enum ScheduleRecurrenceType
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annually
}
