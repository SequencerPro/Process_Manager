using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/workflowschedules")]
public class WorkflowSchedulesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public WorkflowSchedulesController(ProcessManagerDbContext db) => _db = db;

    // ───── CRUD ─────

    [HttpGet]
    public async Task<ActionResult<List<WorkflowScheduleResponseDto>>> GetAll(
        [FromQuery] Guid? workflowId = null)
    {
        var query = _db.WorkflowSchedules
            .Include(s => s.Workflow)
            .AsQueryable();

        if (workflowId.HasValue)
            query = query.Where(s => s.WorkflowId == workflowId.Value);

        var schedules = await query.OrderBy(s => s.Name).ToListAsync();

        var ids = schedules.Select(s => s.Id).ToList();
        var counts = await _db.Workorders
            .Where(w => w.ScheduleId != null && ids.Contains(w.ScheduleId!.Value))
            .GroupBy(w => w.ScheduleId)
            .Select(g => new { ScheduleId = g.Key!.Value, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

        return schedules.Select(s => MapToDto(s, counts.GetValueOrDefault(s.Id, 0))).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowScheduleResponseDto>> GetById(Guid id)
    {
        var schedule = await _db.WorkflowSchedules
            .Include(s => s.Workflow)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        var count = await _db.Workorders.CountAsync(w => w.ScheduleId == id);
        return MapToDto(schedule, count);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowScheduleResponseDto>> Create(CreateWorkflowScheduleDto dto)
    {
        var workflow = await _db.Workflows.FirstOrDefaultAsync(wf => wf.Id == dto.WorkflowId);
        if (workflow is null)
            return BadRequest($"Workflow '{dto.WorkflowId}' not found.");

        if (!ValidateInterval(dto.RecurrenceType, dto.RecurrenceInterval, out var intervalError))
            return BadRequest(intervalError);

        var schedule = new WorkflowSchedule
        {
            WorkflowId = dto.WorkflowId,
            Name = dto.Name,
            RecurrenceType = dto.RecurrenceType,
            RecurrenceInterval = dto.RecurrenceInterval,
            DayOfWeek = ShouldStoreDayOfWeek(dto.RecurrenceType) ? dto.DayOfWeek : null,
            DayOfMonth = ShouldStoreDayOfMonth(dto.RecurrenceType) ? dto.DayOfMonth : null,
            StartDate = dto.StartDate == default ? DateTime.UtcNow : dto.StartDate,
            EndDate = dto.EndDate,
            SubjectTemplate = dto.SubjectTemplate ?? string.Empty,
            IsActive = dto.IsActive
        };

        schedule.NextRunAt = ComputeInitialNextRunAt(schedule);

        _db.WorkflowSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        // Reload with nav props
        await _db.Entry(schedule).Reference(s => s.Workflow).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, MapToDto(schedule, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowScheduleResponseDto>> Update(Guid id, UpdateWorkflowScheduleDto dto)
    {
        var schedule = await _db.WorkflowSchedules
            .Include(s => s.Workflow)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        if (!ValidateInterval(dto.RecurrenceType, dto.RecurrenceInterval, out var intervalError))
            return BadRequest(intervalError);

        schedule.Name = dto.Name;
        schedule.RecurrenceType = dto.RecurrenceType;
        schedule.RecurrenceInterval = dto.RecurrenceInterval;
        schedule.DayOfWeek = ShouldStoreDayOfWeek(dto.RecurrenceType) ? dto.DayOfWeek : null;
        schedule.DayOfMonth = ShouldStoreDayOfMonth(dto.RecurrenceType) ? dto.DayOfMonth : null;
        schedule.StartDate = dto.StartDate == default ? schedule.StartDate : dto.StartDate;
        schedule.EndDate = dto.EndDate;
        schedule.SubjectTemplate = dto.SubjectTemplate ?? string.Empty;
        schedule.IsActive = dto.IsActive;
        schedule.NextRunAt = ComputeInitialNextRunAt(schedule);

        await _db.SaveChangesAsync();

        var count = await _db.Workorders.CountAsync(w => w.ScheduleId == id);
        return MapToDto(schedule, count);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var schedule = await _db.WorkflowSchedules.FirstOrDefaultAsync(s => s.Id == id);
        if (schedule is null) return NotFound();

        var workorderCount = await _db.Workorders.CountAsync(w => w.ScheduleId == id);
        if (workorderCount > 0)
            return BadRequest($"Cannot delete a schedule that has {workorderCount} workorder(s). Deactivate it instead.");

        _db.WorkflowSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<WorkflowScheduleResponseDto>> Activate(Guid id)
    {
        var schedule = await _db.WorkflowSchedules
            .Include(s => s.Workflow)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        schedule.IsActive = true;
        if (schedule.NextRunAt is null)
            schedule.NextRunAt = ComputeInitialNextRunAt(schedule);

        await _db.SaveChangesAsync();

        var count = await _db.Workorders.CountAsync(w => w.ScheduleId == id);
        return MapToDto(schedule, count);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<WorkflowScheduleResponseDto>> Deactivate(Guid id)
    {
        var schedule = await _db.WorkflowSchedules
            .Include(s => s.Workflow)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        schedule.IsActive = false;

        await _db.SaveChangesAsync();

        var count = await _db.Workorders.CountAsync(w => w.ScheduleId == id);
        return MapToDto(schedule, count);
    }

    // ───── NextRunAt computation ─────

    /// <summary>
    /// Computes the initial NextRunAt when a schedule is created or fully updated.
    /// Uses StartDate as the base time.
    /// Returns null if EndDate has already passed (schedule fully expired).
    /// </summary>
    internal static DateTime? ComputeInitialNextRunAt(WorkflowSchedule schedule)
    {
        // If EndDate is already past, the schedule has fully expired
        if (schedule.EndDate.HasValue && schedule.EndDate.Value < DateTime.UtcNow)
            return null;

        var from = schedule.StartDate;

        return schedule.RecurrenceType switch
        {
            // Hourly: first fire = StartDate + interval hours
            ScheduleRecurrenceType.Hourly => ComputeNextRunAt(schedule, from),
            // Daily/Weekly/Monthly: first fire = StartDate itself (or snapped occurrence)
            ScheduleRecurrenceType.Daily => ReturnIfBeforeEndDate(from, schedule),
            ScheduleRecurrenceType.Weekly => SnapToNextDayOfWeek(schedule, from),
            ScheduleRecurrenceType.Monthly => SnapToDayOfMonth(schedule, from),
            ScheduleRecurrenceType.Quarterly => SnapToDayOfMonth(schedule, from),
            ScheduleRecurrenceType.Annually => SnapToDayOfMonth(schedule, from),
            _ => null
        };
    }

    private static DateTime? ReturnIfBeforeEndDate(DateTime candidate, WorkflowSchedule schedule)
    {
        if (schedule.EndDate.HasValue && candidate > schedule.EndDate.Value)
            return null;
        return candidate;
    }

    /// <summary>
    /// Computes the NEXT run time after a schedule fires, using <paramref name="from"/> as the base.
    /// Never backtracks: always returns a future time or null if EndDate has passed.
    /// </summary>
    internal static DateTime? ComputeNextRunAt(WorkflowSchedule schedule, DateTime from)
    {
        DateTime next;

        switch (schedule.RecurrenceType)
        {
            case ScheduleRecurrenceType.Hourly:
                next = from.AddHours(schedule.RecurrenceInterval);
                break;

            case ScheduleRecurrenceType.Daily:
                // Preserve the time-of-day from StartDate
                var startTimeOfDay = schedule.StartDate.TimeOfDay;
                next = from.Date.AddDays(schedule.RecurrenceInterval).Add(startTimeOfDay);
                break;

            case ScheduleRecurrenceType.Weekly:
            {
                var dayOfWeek = schedule.DayOfWeek ?? 0;
                var candidate = from.AddDays(1);
                var daysToAdd = ((dayOfWeek - (int)candidate.DayOfWeek + 7) % 7);
                // If daysToAdd == 0 we're already on the right day, advance by interval weeks
                if (daysToAdd == 0)
                    candidate = candidate.AddDays(7 * (schedule.RecurrenceInterval - 1));
                else
                    candidate = candidate.AddDays(daysToAdd + 7 * (schedule.RecurrenceInterval - 1));
                next = candidate.Date.Add(schedule.StartDate.TimeOfDay);
                break;
            }

            case ScheduleRecurrenceType.Monthly:
            {
                var targetDay = schedule.DayOfMonth ?? 1;
                var candidate = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(schedule.RecurrenceInterval);
                var daysInMonth = DateTime.DaysInMonth(candidate.Year, candidate.Month);
                next = new DateTime(candidate.Year, candidate.Month, Math.Min(targetDay, daysInMonth),
                    0, 0, 0, DateTimeKind.Utc);
                break;
            }

            case ScheduleRecurrenceType.Quarterly:
            {
                var targetDay = schedule.DayOfMonth ?? 1;
                var candidate = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(3 * schedule.RecurrenceInterval);
                var daysInMonth = DateTime.DaysInMonth(candidate.Year, candidate.Month);
                next = new DateTime(candidate.Year, candidate.Month, Math.Min(targetDay, daysInMonth),
                    0, 0, 0, DateTimeKind.Utc);
                break;
            }

            case ScheduleRecurrenceType.Annually:
            {
                var targetDay = schedule.DayOfMonth ?? 1;
                var candidate = new DateTime(from.Year + schedule.RecurrenceInterval, from.Month, 1,
                    0, 0, 0, DateTimeKind.Utc);
                var daysInMonth = DateTime.DaysInMonth(candidate.Year, candidate.Month);
                next = new DateTime(candidate.Year, candidate.Month, Math.Min(targetDay, daysInMonth),
                    0, 0, 0, DateTimeKind.Utc);
                break;
            }

            default:
                return null;
        }

        if (schedule.EndDate.HasValue && next > schedule.EndDate.Value)
            return null;

        return next;
    }

    // ───── Private helpers ─────

    private static DateTime? SnapToNextDayOfWeek(WorkflowSchedule schedule, DateTime from)
    {
        var dayOfWeek = schedule.DayOfWeek ?? 0;
        var daysToAdd = ((dayOfWeek - (int)from.DayOfWeek + 7) % 7);
        var candidate = from.Date.AddDays(daysToAdd == 0 ? 0 : daysToAdd).Add(from.TimeOfDay);

        // Snap to start time-of-day
        candidate = candidate.Date.Add(schedule.StartDate.TimeOfDay);

        if (schedule.EndDate.HasValue && candidate > schedule.EndDate.Value)
            return null;

        return candidate;
    }

    private static DateTime? SnapToDayOfMonth(WorkflowSchedule schedule, DateTime from)
    {
        var targetDay = schedule.DayOfMonth ?? 1;
        var daysInMonth = DateTime.DaysInMonth(from.Year, from.Month);
        var candidate = new DateTime(from.Year, from.Month, Math.Min(targetDay, daysInMonth),
            0, 0, 0, DateTimeKind.Utc);

        // If candidate is before StartDate, stay at candidate (initial)
        if (schedule.EndDate.HasValue && candidate > schedule.EndDate.Value)
            return null;

        return candidate;
    }

    private static bool ShouldStoreDayOfWeek(ScheduleRecurrenceType type)
        => type == ScheduleRecurrenceType.Weekly;

    private static bool ShouldStoreDayOfMonth(ScheduleRecurrenceType type)
        => type == ScheduleRecurrenceType.Monthly
        || type == ScheduleRecurrenceType.Quarterly
        || type == ScheduleRecurrenceType.Annually;

    private static bool ValidateInterval(ScheduleRecurrenceType type, int interval, out string? error)
    {
        var (min, max) = type switch
        {
            ScheduleRecurrenceType.Hourly => (1, 168),
            ScheduleRecurrenceType.Daily => (1, 365),
            ScheduleRecurrenceType.Weekly => (1, 52),
            ScheduleRecurrenceType.Monthly => (1, 24),
            ScheduleRecurrenceType.Quarterly => (1, 8),
            ScheduleRecurrenceType.Annually => (1, 10),
            _ => (1, int.MaxValue)
        };

        if (interval < min || interval > max)
        {
            error = $"RecurrenceInterval for {type} must be between {min} and {max}.";
            return false;
        }

        error = null;
        return true;
    }

    // ───── Mapper ─────

    private static WorkflowScheduleResponseDto MapToDto(WorkflowSchedule s, int workorderCount)
    {
        return new WorkflowScheduleResponseDto(
            s.Id,
            s.WorkflowId,
            s.Workflow?.Name ?? string.Empty,
            s.Name,
            s.RecurrenceType.ToString(),
            s.RecurrenceInterval,
            s.DayOfWeek,
            s.DayOfMonth,
            s.StartDate,
            s.EndDate,
            s.SubjectTemplate,
            s.IsActive,
            s.NextRunAt,
            s.LastRunAt,
            workorderCount,
            s.CreatedAt,
            s.UpdatedAt);
    }
}
