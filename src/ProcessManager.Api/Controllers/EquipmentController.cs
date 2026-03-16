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
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public EquipmentController(ProcessManagerDbContext db) => _db = db;

    // ───── Equipment Categories ─────

    [HttpGet("categories")]
    public async Task<ActionResult<List<EquipmentCategoryResponseDto>>> GetCategories()
    {
        var cats = await _db.EquipmentCategories
            .OrderBy(c => c.Name)
            .Select(c => new EquipmentCategoryResponseDto(
                c.Id, c.Code, c.Name,
                c.Equipment.Count,
                c.CreatedAt, c.UpdatedAt))
            .ToListAsync();

        return cats;
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<EquipmentCategoryResponseDto>> CreateCategory(EquipmentCategoryCreateDto dto)
    {
        if (await _db.EquipmentCategories.AnyAsync(c => c.Code == dto.Code))
            return Conflict($"Category code '{dto.Code}' already exists.");

        var cat = new EquipmentCategory { Code = dto.Code, Name = dto.Name };
        _db.EquipmentCategories.Add(cat);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { },
            new EquipmentCategoryResponseDto(cat.Id, cat.Code, cat.Name, 0, cat.CreatedAt, cat.UpdatedAt));
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<EquipmentCategoryResponseDto>> UpdateCategory(Guid id, EquipmentCategoryUpdateDto dto)
    {
        var cat = await _db.EquipmentCategories.Include(c => c.Equipment).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();

        cat.Name = dto.Name;
        await _db.SaveChangesAsync();

        return new EquipmentCategoryResponseDto(cat.Id, cat.Code, cat.Name, cat.Equipment.Count, cat.CreatedAt, cat.UpdatedAt);
    }

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var cat = await _db.EquipmentCategories.Include(c => c.Equipment).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();

        if (cat.Equipment.Any())
            return Conflict("Cannot delete a category that has equipment assigned to it.");

        _db.EquipmentCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Equipment CRUD ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EquipmentSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Equipment.Include(e => e.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Code.Contains(search) || e.Name.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);
        if (activeOnly.HasValue)
            query = query.Where(e => e.IsActive == activeOnly.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(e => e.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var now = DateTime.UtcNow;
        var ids = items.Select(e => e.Id).ToList();

        var currentDowntime = await _db.DowntimeRecords
            .Where(d => ids.Contains(d.EquipmentId) && d.EndedAt == null)
            .ToListAsync();

        var nextMaint = await _db.MaintenanceTasks
            .Where(t => ids.Contains(t.EquipmentId)
                && (t.Status == MaintenanceTaskStatus.Upcoming || t.Status == MaintenanceTaskStatus.Due || t.Status == MaintenanceTaskStatus.Overdue))
            .GroupBy(t => t.EquipmentId)
            .Select(g => new { EquipmentId = g.Key, NextDue = g.Min(t => t.DueDate) })
            .ToListAsync();

        var result = items.Select(e =>
        {
            var down = currentDowntime.FirstOrDefault(d => d.EquipmentId == e.Id);
            var maint = nextMaint.FirstOrDefault(m => m.EquipmentId == e.Id);
            return new EquipmentSummaryDto(
                e.Id, e.Code, e.Name, e.CategoryId, e.Category.Name,
                e.Location, e.IsActive,
                down is not null,
                down?.Type.ToString(),
                maint?.NextDue,
                e.CreatedAt, e.UpdatedAt);
        }).ToList();

        return new PaginatedResponse<EquipmentSummaryDto>(result, total, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentResponseDto>> GetById(Guid id)
    {
        var eq = await _db.Equipment
            .Include(e => e.Category)
            .Include(e => e.DowntimeRecords.OrderByDescending(d => d.StartedAt).Take(20))
            .Include(e => e.MaintenanceTriggers)
            .Include(e => e.MaintenanceTasks.OrderByDescending(t => t.DueDate).Take(30))
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eq is null) return NotFound();
        return MapToDto(eq, includeDetails: true);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<EquipmentResponseDto>> Create(EquipmentCreateDto dto)
    {
        if (await _db.Equipment.AnyAsync(e => e.Code == dto.Code))
            return Conflict($"Equipment code '{dto.Code}' already exists.");

        if (!await _db.EquipmentCategories.AnyAsync(c => c.Id == dto.CategoryId))
            return BadRequest($"Category '{dto.CategoryId}' not found.");

        var eq = new Equipment
        {
            Code = dto.Code, Name = dto.Name, CategoryId = dto.CategoryId,
            Location = dto.Location, Manufacturer = dto.Manufacturer,
            Model = dto.Model, SerialNumber = dto.SerialNumber,
            InstallDate = dto.InstallDate
        };
        _db.Equipment.Add(eq);
        await _db.SaveChangesAsync();

        var result = await _db.Equipment.Include(e => e.Category).FirstAsync(e => e.Id == eq.Id);
        return CreatedAtAction(nameof(GetById), new { id = eq.Id }, MapToDto(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<EquipmentResponseDto>> Update(Guid id, EquipmentUpdateDto dto)
    {
        var eq = await _db.Equipment.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);
        if (eq is null) return NotFound();

        if (!await _db.EquipmentCategories.AnyAsync(c => c.Id == dto.CategoryId))
            return BadRequest($"Category '{dto.CategoryId}' not found.");

        eq.Name = dto.Name; eq.CategoryId = dto.CategoryId;
        eq.Location = dto.Location; eq.Manufacturer = dto.Manufacturer;
        eq.Model = dto.Model; eq.SerialNumber = dto.SerialNumber;
        eq.InstallDate = dto.InstallDate; eq.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        var result = await _db.Equipment.Include(e => e.Category).FirstAsync(e => e.Id == id);
        return MapToDto(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var eq = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (eq is null) return NotFound();

        if (await _db.DowntimeRecords.AnyAsync(d => d.EquipmentId == id))
            return Conflict("Cannot delete equipment with downtime history. Mark as inactive instead.");

        _db.Equipment.Remove(eq);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Downtime Records ─────

    [HttpGet("{equipmentId:guid}/downtime")]
    public async Task<ActionResult<List<DowntimeRecordResponseDto>>> GetDowntime(Guid equipmentId)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == equipmentId)) return NotFound();

        var records = await _db.DowntimeRecords
            .Include(d => d.Equipment)
            .Where(d => d.EquipmentId == equipmentId)
            .OrderByDescending(d => d.StartedAt)
            .ToListAsync();

        return records.Select(MapDowntimeToDto).ToList();
    }

    [HttpPost("{equipmentId:guid}/downtime")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<DowntimeRecordResponseDto>> StartDowntime(Guid equipmentId, CreateDowntimeRecordDto dto)
    {
        var eq = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (eq is null) return NotFound();

        var record = new DowntimeRecord
        {
            EquipmentId = equipmentId,
            Type = dto.Type,
            StartedAt = dto.StartedAt,
            Reason = dto.Reason
        };
        _db.DowntimeRecords.Add(record);
        await _db.SaveChangesAsync();

        var result = await _db.DowntimeRecords.Include(d => d.Equipment).FirstAsync(d => d.Id == record.Id);
        return CreatedAtAction(nameof(GetDowntime), new { equipmentId }, MapDowntimeToDto(result));
    }

    [HttpPost("{equipmentId:guid}/downtime/{recordId:guid}/close")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<DowntimeRecordResponseDto>> CloseDowntime(Guid equipmentId, Guid recordId, CloseDowntimeRecordDto dto)
    {
        var record = await _db.DowntimeRecords
            .Include(d => d.Equipment)
            .FirstOrDefaultAsync(d => d.Id == recordId && d.EquipmentId == equipmentId);
        if (record is null) return NotFound();

        if (record.EndedAt is not null)
            return BadRequest("This downtime record is already closed.");

        record.EndedAt = dto.EndedAt;
        record.ResolvedBy = dto.ResolvedBy;
        await _db.SaveChangesAsync();
        return MapDowntimeToDto(record);
    }

    // ───── Maintenance Triggers ─────

    [HttpGet("{equipmentId:guid}/triggers")]
    public async Task<ActionResult<List<MaintenanceTriggerResponseDto>>> GetTriggers(Guid equipmentId)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == equipmentId)) return NotFound();

        var triggers = await _db.MaintenanceTriggers
            .Where(t => t.EquipmentId == equipmentId)
            .OrderBy(t => t.Title)
            .ToListAsync();

        return triggers.Select(MapTriggerToDto).ToList();
    }

    [HttpPost("{equipmentId:guid}/triggers")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTriggerResponseDto>> CreateTrigger(Guid equipmentId, CreateMaintenanceTriggerDto dto)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == equipmentId)) return NotFound();

        var trigger = new MaintenanceTrigger
        {
            EquipmentId = equipmentId, Title = dto.Title,
            TriggerType = dto.TriggerType, IntervalDays = dto.IntervalDays,
            IntervalUsageCycles = dto.IntervalUsageCycles,
            AdvanceNoticeDays = dto.AdvanceNoticeDays
        };

        // Set initial NextDueAt for time-based triggers
        if (dto.TriggerType == MaintenanceTriggerType.TimeBased && dto.IntervalDays.HasValue)
            trigger.NextDueAt = DateTime.UtcNow.AddDays(dto.IntervalDays.Value);

        _db.MaintenanceTriggers.Add(trigger);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTriggers), new { equipmentId }, MapTriggerToDto(trigger));
    }

    [HttpDelete("{equipmentId:guid}/triggers/{triggerId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTrigger(Guid equipmentId, Guid triggerId)
    {
        var trigger = await _db.MaintenanceTriggers.FirstOrDefaultAsync(t => t.Id == triggerId && t.EquipmentId == equipmentId);
        if (trigger is null) return NotFound();
        _db.MaintenanceTriggers.Remove(trigger);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Maintenance Tasks ─────

    [HttpGet("{equipmentId:guid}/tasks")]
    public async Task<ActionResult<List<MaintenanceTaskResponseDto>>> GetTasks(Guid equipmentId,
        [FromQuery] string? status = null)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == equipmentId)) return NotFound();

        var query = _db.MaintenanceTasks.Include(t => t.Equipment).Where(t => t.EquipmentId == equipmentId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MaintenanceTaskStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);

        var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();
        return tasks.Select(MapTaskToDto).ToList();
    }

    [HttpPost("tasks")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTaskResponseDto>> CreateTask(CreateMaintenanceTaskDto dto)
    {
        if (!await _db.Equipment.AnyAsync(e => e.Id == dto.EquipmentId))
            return BadRequest($"Equipment '{dto.EquipmentId}' not found.");

        var now = DateTime.UtcNow;
        var status = dto.DueDate.Date <= now.Date
            ? MaintenanceTaskStatus.Due
            : MaintenanceTaskStatus.Upcoming;

        var task = new MaintenanceTask
        {
            EquipmentId = dto.EquipmentId, TriggerId = dto.TriggerId,
            Title = dto.Title, Type = dto.Type,
            DueDate = dto.DueDate, AssignedTo = dto.AssignedTo,
            Status = status
        };
        _db.MaintenanceTasks.Add(task);
        await _db.SaveChangesAsync();

        var result = await _db.MaintenanceTasks.Include(t => t.Equipment).FirstAsync(t => t.Id == task.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.EquipmentId }, MapTaskToDto(result));
    }

    [HttpPut("tasks/{taskId:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTaskResponseDto>> UpdateTask(Guid taskId, UpdateMaintenanceTaskDto dto)
    {
        var task = await _db.MaintenanceTasks.Include(t => t.Equipment).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null) return NotFound();

        if (task.Status == MaintenanceTaskStatus.Completed || task.Status == MaintenanceTaskStatus.Cancelled)
            return BadRequest($"Cannot update a {task.Status} task.");

        task.Title = dto.Title; task.Type = dto.Type;
        task.DueDate = dto.DueDate; task.AssignedTo = dto.AssignedTo;

        // Recalculate status
        var now = DateTime.UtcNow;
        if (task.DueDate.Date < now.Date) task.Status = MaintenanceTaskStatus.Overdue;
        else if (task.DueDate.Date == now.Date) task.Status = MaintenanceTaskStatus.Due;

        await _db.SaveChangesAsync();
        return MapTaskToDto(task);
    }

    [HttpPost("tasks/{taskId:guid}/start")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTaskResponseDto>> StartTask(Guid taskId)
    {
        var task = await _db.MaintenanceTasks.Include(t => t.Equipment).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null) return NotFound();

        if (task.Status == MaintenanceTaskStatus.Completed || task.Status == MaintenanceTaskStatus.Cancelled)
            return BadRequest($"Task is already {task.Status}.");

        task.Status = MaintenanceTaskStatus.InProgress;
        await _db.SaveChangesAsync();
        return MapTaskToDto(task);
    }

    [HttpPost("tasks/{taskId:guid}/complete")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTaskResponseDto>> CompleteTask(Guid taskId, CompleteMaintenanceTaskDto dto)
    {
        var task = await _db.MaintenanceTasks
            .Include(t => t.Equipment)
            .Include(t => t.Trigger)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null) return NotFound();

        if (task.Status == MaintenanceTaskStatus.Completed)
            return BadRequest("Task is already completed.");

        task.Status = MaintenanceTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedBy = dto.CompletedBy;
        task.Notes = dto.Notes;
        task.LinkedDowntimeRecordId = dto.LinkedDowntimeRecordId;

        // Advance TimeBased trigger NextDueAt
        if (task.Trigger is { TriggerType: MaintenanceTriggerType.TimeBased, IntervalDays: not null })
        {
            task.Trigger.LastTriggeredAt = DateTime.UtcNow;
            task.Trigger.NextDueAt = DateTime.UtcNow.AddDays(task.Trigger.IntervalDays.Value);
        }

        await _db.SaveChangesAsync();
        return MapTaskToDto(task);
    }

    [HttpPost("tasks/{taskId:guid}/cancel")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<MaintenanceTaskResponseDto>> CancelTask(Guid taskId)
    {
        var task = await _db.MaintenanceTasks.Include(t => t.Equipment).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null) return NotFound();

        if (task.Status == MaintenanceTaskStatus.Completed || task.Status == MaintenanceTaskStatus.Cancelled)
            return BadRequest($"Task is already {task.Status}.");

        task.Status = MaintenanceTaskStatus.Cancelled;
        await _db.SaveChangesAsync();
        return MapTaskToDto(task);
    }

    // All tasks across all equipment (for the maintenance list view)
    [HttpGet("tasks")]
    public async Task<ActionResult<PaginatedResponse<MaintenanceTaskResponseDto>>> GetAllTasks(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.MaintenanceTasks.Include(t => t.Equipment).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MaintenanceTaskStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<MaintenanceTaskType>(type, true, out var tp))
            query = query.Where(t => t.Type == tp);
        if (equipmentId.HasValue)
            query = query.Where(t => t.EquipmentId == equipmentId.Value);

        var total = await query.CountAsync();
        var tasks = await query.OrderBy(t => t.DueDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedResponse<MaintenanceTaskResponseDto>(tasks.Select(MapTaskToDto).ToList(), total, page, pageSize);
    }

    // ───── Mappers ─────

    private static EquipmentResponseDto MapToDto(Equipment eq, bool includeDetails = false)
    {
        var currentDown = eq.DowntimeRecords.FirstOrDefault(d => d.EndedAt == null);
        var nextMaint = eq.MaintenanceTasks
            .Where(t => t.Status is MaintenanceTaskStatus.Upcoming or MaintenanceTaskStatus.Due or MaintenanceTaskStatus.Overdue)
            .MinBy(t => t.DueDate);

        return new EquipmentResponseDto(
            eq.Id, eq.Code, eq.Name, eq.CategoryId, eq.Category?.Name ?? "",
            eq.Location, eq.Manufacturer, eq.Model, eq.SerialNumber,
            eq.InstallDate, eq.IsActive,
            currentDown is not null, currentDown?.Type.ToString(),
            nextMaint?.DueDate,
            eq.CreatedAt, eq.UpdatedAt,
            includeDetails ? eq.DowntimeRecords.Select(MapDowntimeToDto).ToList() : null,
            includeDetails ? eq.MaintenanceTriggers.Select(MapTriggerToDto).ToList() : null,
            includeDetails ? eq.MaintenanceTasks.Select(MapTaskToDto).ToList() : null);
    }

    internal static DowntimeRecordResponseDto MapDowntimeToDto(DowntimeRecord d)
    {
        double? duration = d.EndedAt.HasValue
            ? (d.EndedAt.Value - d.StartedAt).TotalMinutes
            : null;
        return new DowntimeRecordResponseDto(
            d.Id, d.EquipmentId, d.Equipment?.Code ?? "", d.Equipment?.Name ?? "",
            d.Type.ToString(), d.StartedAt, d.EndedAt, duration,
            d.Reason, d.ResolvedBy, d.LinkedMaintenanceTaskId,
            d.CreatedAt, d.UpdatedAt);
    }

    internal static MaintenanceTriggerResponseDto MapTriggerToDto(MaintenanceTrigger t) =>
        new(t.Id, t.EquipmentId, t.Title, t.TriggerType.ToString(),
            t.IntervalDays, t.IntervalUsageCycles,
            t.LastTriggeredAt, t.NextDueAt, t.AdvanceNoticeDays,
            t.CreatedAt, t.UpdatedAt);

    internal static MaintenanceTaskResponseDto MapTaskToDto(MaintenanceTask t) =>
        new(t.Id, t.EquipmentId, t.Equipment?.Code ?? "", t.Equipment?.Name ?? "",
            t.TriggerId, t.Title, t.Type.ToString(), t.Status.ToString(),
            t.DueDate, t.AssignedTo, t.CompletedAt, t.CompletedBy, t.Notes,
            t.LinkedDowntimeRecordId, t.CreatedAt, t.UpdatedAt);
}
