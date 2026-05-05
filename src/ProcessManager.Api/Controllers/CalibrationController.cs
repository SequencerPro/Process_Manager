using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/calibration")]
public class CalibrationController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public CalibrationController(ProcessManagerDbContext db) => _db = db;

    // ── Records: List ────────────────────────────────────────────────────────

    [HttpGet("records")]
    public async Task<ActionResult<PaginatedResponse<CalibrationRecordSummaryDto>>> GetRecords(
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] string? result = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.CalibrationRecords
            .Include(r => r.Equipment)
            .AsQueryable();

        if (equipmentId.HasValue)
            query = query.Where(r => r.EquipmentId == equipmentId.Value);

        if (!string.IsNullOrWhiteSpace(result) && Enum.TryParse<CalibrationResult>(result, true, out var res))
            query = query.Where(r => r.Result == res);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CalibrationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummaryDto).ToList();
        return new PaginatedResponse<CalibrationRecordSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Records: Get by ID ───────────────────────────────────────────────────

    [HttpGet("records/{id:guid}")]
    public async Task<ActionResult<CalibrationRecordResponseDto>> GetRecord(Guid id)
    {
        var record = await _db.CalibrationRecords
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record is null) return NotFound();
        return MapToDto(record);
    }

    // ── Records: Create ──────────────────────────────────────────────────────

    [HttpPost("records")]
    public async Task<ActionResult<CalibrationRecordResponseDto>> CreateRecord([FromBody] CreateCalibrationRecordDto dto)
    {
        if (!Enum.TryParse<CalibrationType>(dto.CalibrationType, true, out var calType))
            return BadRequest("Invalid CalibrationType.");

        if (!Enum.TryParse<CalibrationResult>(dto.Result, true, out var calResult))
            return BadRequest("Invalid Result.");

        var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == dto.EquipmentId);
        if (equipment is null) return BadRequest("Equipment not found.");

        var record = new CalibrationRecord
        {
            EquipmentId = dto.EquipmentId,
            CalibrationType = calType,
            CalibrationDate = dto.CalibrationDate,
            NextDueDate = dto.NextDueDate,
            CertificateNumber = dto.CertificateNumber,
            CertificateFileName = dto.CertificateFileName,
            Result = calResult,
            PerformedBy = dto.PerformedBy,
            StandardsUsed = dto.StandardsUsed,
            TemperatureHumidity = dto.TemperatureHumidity,
            AsFoundReading = dto.AsFoundReading,
            AsLeftReading = dto.AsLeftReading,
            Uncertainty = dto.Uncertainty,
            Notes = dto.Notes,
        };

        _db.CalibrationRecords.Add(record);

        // Update schedule consecutive pass count if a schedule exists
        var schedule = await _db.CalibrationSchedules.FirstOrDefaultAsync(s => s.EquipmentId == dto.EquipmentId && s.IsActive);
        if (schedule is not null)
        {
            if (calResult == CalibrationResult.Pass)
            {
                schedule.ConsecutivePassCount++;
                if (schedule.IntervalAdjustmentMethod == IntervalAdjustmentMethod.ReliabilityBased)
                {
                    var extended = (int)(schedule.IntervalDays * (1.0 + schedule.ExtensionPercent / 100.0));
                    schedule.IntervalDays = Math.Min(extended, schedule.MaxIntervalDays);
                }
            }
            else
            {
                schedule.ConsecutivePassCount = 0;
                if (schedule.IntervalAdjustmentMethod == IntervalAdjustmentMethod.ReliabilityBased)
                {
                    schedule.IntervalDays = schedule.MinIntervalDays;
                }
            }
        }

        await _db.SaveChangesAsync();

        record.Equipment = equipment;
        return CreatedAtAction(nameof(GetRecord), new { id = record.Id }, MapToDto(record));
    }

    // ── Records: Update ──────────────────────────────────────────────────────

    [HttpPut("records/{id:guid}")]
    public async Task<ActionResult<CalibrationRecordResponseDto>> UpdateRecord(Guid id, [FromBody] UpdateCalibrationRecordDto dto)
    {
        var record = await _db.CalibrationRecords
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record is null) return NotFound();

        if (!Enum.TryParse<CalibrationType>(dto.CalibrationType, true, out var calType))
            return BadRequest("Invalid CalibrationType.");

        if (!Enum.TryParse<CalibrationResult>(dto.Result, true, out var calResult))
            return BadRequest("Invalid Result.");

        record.CalibrationType = calType;
        record.CalibrationDate = dto.CalibrationDate;
        record.NextDueDate = dto.NextDueDate;
        record.CertificateNumber = dto.CertificateNumber;
        record.CertificateFileName = dto.CertificateFileName;
        record.Result = calResult;
        record.PerformedBy = dto.PerformedBy;
        record.StandardsUsed = dto.StandardsUsed;
        record.TemperatureHumidity = dto.TemperatureHumidity;
        record.AsFoundReading = dto.AsFoundReading;
        record.AsLeftReading = dto.AsLeftReading;
        record.Uncertainty = dto.Uncertainty;
        record.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        return MapToDto(record);
    }

    // ── Records: Delete ──────────────────────────────────────────────────────

    [HttpDelete("records/{id:guid}")]
    public async Task<IActionResult> DeleteRecord(Guid id)
    {
        var record = await _db.CalibrationRecords.FirstOrDefaultAsync(r => r.Id == id);
        if (record is null) return NotFound();

        _db.CalibrationRecords.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Records: History for Equipment ───────────────────────────────────────

    [HttpGet("equipment/{equipmentId:guid}/history")]
    public async Task<ActionResult<List<CalibrationRecordSummaryDto>>> GetEquipmentHistory(Guid equipmentId)
    {
        var records = await _db.CalibrationRecords
            .Include(r => r.Equipment)
            .Where(r => r.EquipmentId == equipmentId)
            .OrderByDescending(r => r.CalibrationDate)
            .ToListAsync();

        return records.Select(MapToSummaryDto).ToList();
    }

    // ── Schedules: List ──────────────────────────────────────────────────────

    [HttpGet("schedules")]
    public async Task<ActionResult<List<CalibrationScheduleResponseDto>>> GetSchedules(
        [FromQuery] bool? activeOnly = null)
    {
        var query = _db.CalibrationSchedules
            .Include(s => s.Equipment)
            .AsQueryable();

        if (activeOnly == true)
            query = query.Where(s => s.IsActive);

        var items = await query.OrderBy(s => s.Equipment.Code).ToListAsync();

        var result = new List<CalibrationScheduleResponseDto>();
        foreach (var s in items)
        {
            var lastRecord = await _db.CalibrationRecords
                .Where(r => r.EquipmentId == s.EquipmentId)
                .OrderByDescending(r => r.CalibrationDate)
                .FirstOrDefaultAsync();

            result.Add(MapToScheduleDto(s, lastRecord));
        }

        return result;
    }

    // ── Schedules: Get by ID ─────────────────────────────────────────────────

    [HttpGet("schedules/{id:guid}")]
    public async Task<ActionResult<CalibrationScheduleResponseDto>> GetSchedule(Guid id)
    {
        var schedule = await _db.CalibrationSchedules
            .Include(s => s.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        var lastRecord = await _db.CalibrationRecords
            .Where(r => r.EquipmentId == schedule.EquipmentId)
            .OrderByDescending(r => r.CalibrationDate)
            .FirstOrDefaultAsync();

        return MapToScheduleDto(schedule, lastRecord);
    }

    // ── Schedules: Create ────────────────────────────────────────────────────

    [HttpPost("schedules")]
    public async Task<ActionResult<CalibrationScheduleResponseDto>> CreateSchedule([FromBody] CreateCalibrationScheduleDto dto)
    {
        if (!Enum.TryParse<IntervalAdjustmentMethod>(dto.IntervalAdjustmentMethod, true, out var method))
            return BadRequest("Invalid IntervalAdjustmentMethod.");

        var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == dto.EquipmentId);
        if (equipment is null) return BadRequest("Equipment not found.");

        var existing = await _db.CalibrationSchedules.AnyAsync(s => s.EquipmentId == dto.EquipmentId);
        if (existing) return Conflict("A calibration schedule already exists for this equipment.");

        if (dto.MinIntervalDays > dto.MaxIntervalDays)
            return BadRequest("MinIntervalDays cannot exceed MaxIntervalDays.");

        var schedule = new CalibrationSchedule
        {
            EquipmentId = dto.EquipmentId,
            IntervalDays = dto.IntervalDays,
            IntervalAdjustmentMethod = method,
            MaxIntervalDays = dto.MaxIntervalDays,
            MinIntervalDays = dto.MinIntervalDays,
            ExtensionPercent = dto.ExtensionPercent,
        };

        _db.CalibrationSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        schedule.Equipment = equipment;
        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, MapToScheduleDto(schedule, null));
    }

    // ── Schedules: Update ────────────────────────────────────────────────────

    [HttpPut("schedules/{id:guid}")]
    public async Task<ActionResult<CalibrationScheduleResponseDto>> UpdateSchedule(Guid id, [FromBody] UpdateCalibrationScheduleDto dto)
    {
        var schedule = await _db.CalibrationSchedules
            .Include(s => s.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule is null) return NotFound();

        if (!Enum.TryParse<IntervalAdjustmentMethod>(dto.IntervalAdjustmentMethod, true, out var method))
            return BadRequest("Invalid IntervalAdjustmentMethod.");

        if (dto.MinIntervalDays > dto.MaxIntervalDays)
            return BadRequest("MinIntervalDays cannot exceed MaxIntervalDays.");

        schedule.IntervalDays = dto.IntervalDays;
        schedule.IntervalAdjustmentMethod = method;
        schedule.MaxIntervalDays = dto.MaxIntervalDays;
        schedule.MinIntervalDays = dto.MinIntervalDays;
        schedule.ExtensionPercent = dto.ExtensionPercent;
        schedule.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var lastRecord = await _db.CalibrationRecords
            .Where(r => r.EquipmentId == schedule.EquipmentId)
            .OrderByDescending(r => r.CalibrationDate)
            .FirstOrDefaultAsync();

        return MapToScheduleDto(schedule, lastRecord);
    }

    // ── Schedules: Delete ────────────────────────────────────────────────────

    [HttpDelete("schedules/{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await _db.CalibrationSchedules.FirstOrDefaultAsync(s => s.Id == id);
        if (schedule is null) return NotFound();

        _db.CalibrationSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<CalibrationDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysFromNow = now.AddDays(30);

        var schedules = await _db.CalibrationSchedules
            .Include(s => s.Equipment)
            .ToListAsync();

        var activeSchedules = schedules.Where(s => s.IsActive).ToList();

        var allRecords = await _db.CalibrationRecords.ToListAsync();

        var recalls = new List<CalibrationRecallDto>();
        foreach (var schedule in activeSchedules)
        {
            var lastRecord = allRecords
                .Where(r => r.EquipmentId == schedule.EquipmentId)
                .OrderByDescending(r => r.CalibrationDate)
                .FirstOrDefault();

            var nextDue = lastRecord?.NextDueDate ?? now;
            var daysUntil = (int)(nextDue - now).TotalDays;

            recalls.Add(new CalibrationRecallDto(
                schedule.EquipmentId,
                schedule.Equipment.Code,
                schedule.Equipment.Name,
                nextDue,
                daysUntil,
                lastRecord?.Result.ToString(),
                lastRecord?.CalibrationDate));
        }

        var dueRecalls = recalls.Where(r => r.DaysUntilDue >= 0 && r.DaysUntilDue <= 30)
            .OrderBy(r => r.DaysUntilDue).ToList();

        var overdueRecalls = recalls.Where(r => r.DaysUntilDue < 0)
            .OrderBy(r => r.DaysUntilDue).ToList();

        return new CalibrationDashboardDto(
            TotalSchedules: schedules.Count,
            ActiveSchedules: activeSchedules.Count,
            DueCount: dueRecalls.Count,
            OverdueCount: overdueRecalls.Count,
            TotalRecords: allRecords.Count,
            PassCount: allRecords.Count(r => r.Result == CalibrationResult.Pass),
            FailCount: allRecords.Count(r => r.Result == CalibrationResult.Fail),
            LimitedCount: allRecords.Count(r => r.Result == CalibrationResult.Limited),
            DueRecalls: dueRecalls,
            OverdueRecalls: overdueRecalls);
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static CalibrationRecordResponseDto MapToDto(CalibrationRecord r) => new(
        r.Id,
        r.EquipmentId,
        r.Equipment.Code,
        r.Equipment.Name,
        r.CalibrationType.ToString(),
        r.CalibrationDate,
        r.NextDueDate,
        r.CertificateNumber,
        r.CertificateFileName,
        r.Result.ToString(),
        r.PerformedBy,
        r.StandardsUsed,
        r.TemperatureHumidity,
        r.AsFoundReading,
        r.AsLeftReading,
        r.Uncertainty,
        r.Notes,
        r.CreatedAt,
        r.UpdatedAt);

    private static CalibrationRecordSummaryDto MapToSummaryDto(CalibrationRecord r) => new(
        r.Id,
        r.EquipmentId,
        r.Equipment.Code,
        r.Equipment.Name,
        r.CalibrationType.ToString(),
        r.CalibrationDate,
        r.NextDueDate,
        r.Result.ToString(),
        r.CertificateNumber,
        r.PerformedBy);

    private static CalibrationScheduleResponseDto MapToScheduleDto(CalibrationSchedule s, CalibrationRecord? lastRecord) => new(
        s.Id,
        s.EquipmentId,
        s.Equipment.Code,
        s.Equipment.Name,
        s.IntervalDays,
        s.IntervalAdjustmentMethod.ToString(),
        s.ConsecutivePassCount,
        s.MaxIntervalDays,
        s.MinIntervalDays,
        s.ExtensionPercent,
        s.IsActive,
        lastRecord?.CalibrationDate,
        lastRecord?.NextDueDate,
        lastRecord?.Result.ToString(),
        s.CreatedAt,
        s.UpdatedAt);
}
