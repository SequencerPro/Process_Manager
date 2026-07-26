using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/oee")]
public class OeeController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IOeeCalculationService _oeeService;

    public OeeController(ProcessManagerDbContext db, IOeeCalculationService oeeService)
    {
        _db = db;
        _oeeService = oeeService;
    }

    // ── Shifts: List ────────────────────────────────────────────────────────

    [HttpGet("shifts")]
    public async Task<ActionResult<List<ShiftDefinitionResponseDto>>> GetShifts([FromQuery] bool? activeOnly = null)
    {
        var query = _db.Set<ShiftDefinition>().AsQueryable();
        if (activeOnly == true)
            query = query.Where(s => s.IsActive);

        var shifts = await query.OrderBy(s => s.StartTime).ToListAsync();
        return shifts.Select(MapToDto).ToList();
    }

    // ── Shifts: Get by ID ──────────────────��────────────────────────────────

    [HttpGet("shifts/{id:guid}")]
    public async Task<ActionResult<ShiftDefinitionResponseDto>> GetShift(Guid id)
    {
        var shift = await _db.Set<ShiftDefinition>().FirstOrDefaultAsync(s => s.Id == id);
        if (shift is null) return NotFound();
        return MapToDto(shift);
    }

    // ── Shifts: Create ────────────────────────────────────────────────��─────

    [HttpPost("shifts")]
    public async Task<ActionResult<ShiftDefinitionResponseDto>> CreateShift([FromBody] CreateShiftDefinitionDto dto)
    {
        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
            return BadRequest("Invalid StartTime format. Use HH:mm.");

        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
            return BadRequest("Invalid EndTime format. Use HH:mm.");

        var exists = await _db.Set<ShiftDefinition>().AnyAsync(s => s.Code == dto.Code);
        if (exists) return Conflict($"Shift with code '{dto.Code}' already exists.");

        var shift = new ShiftDefinition
        {
            Code = dto.Code,
            Name = dto.Name,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = true
        };

        _db.Set<ShiftDefinition>().Add(shift);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, MapToDto(shift));
    }

    // ── Shifts: Update ────────────────────────────────────────���─────────────

    [HttpPut("shifts/{id:guid}")]
    public async Task<ActionResult<ShiftDefinitionResponseDto>> UpdateShift(Guid id, [FromBody] UpdateShiftDefinitionDto dto)
    {
        var shift = await _db.Set<ShiftDefinition>().FirstOrDefaultAsync(s => s.Id == id);
        if (shift is null) return NotFound();

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
            return BadRequest("Invalid StartTime format. Use HH:mm.");

        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
            return BadRequest("Invalid EndTime format. Use HH:mm.");

        shift.Name = dto.Name;
        shift.StartTime = startTime;
        shift.EndTime = endTime;
        shift.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return MapToDto(shift);
    }

    // ── Shifts: Delete ───────────────��──────────────────────────────────────

    [HttpDelete("shifts/{id:guid}")]
    public async Task<IActionResult> DeleteShift(Guid id)
    {
        var shift = await _db.Set<ShiftDefinition>().FirstOrDefaultAsync(s => s.Id == id);
        if (shift is null) return NotFound();

        _db.Set<ShiftDefinition>().Remove(shift);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── OEE Dashboard ────────────────��────────────────────────��─────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<OeeDashboardDto>> GetDashboard(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] decimal targetOee = 85m)
    {
        var dashboard = await _oeeService.GetDashboardAsync(fromDate, toDate, equipmentId, targetOee);
        return dashboard;
    }

    // ── OEE Trend ───────────────────���───────────────────────────────────────

    [HttpGet("trend/{equipmentId:guid}")]
    public async Task<ActionResult<OeeTrendDto>> GetTrend(
        Guid equipmentId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-7);
        var to = toDate ?? DateTime.UtcNow.Date;

        var trend = await _oeeService.GetTrendAsync(equipmentId, from, to);
        if (trend is null) return NotFound();
        return trend;
    }

    // ── OEE Losses ──────────��───────────────────────────────────────────────

    [HttpGet("losses")]
    public async Task<ActionResult<List<OeeLossCategoryDto>>> GetLosses(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? equipmentId = null)
    {
        var losses = await _oeeService.GetLossesAsync(fromDate, toDate, equipmentId);
        return losses;
    }

    // ─��� OEE for specific equipment + date + shift ───────────────────────────

    [HttpGet("calculate")]
    public async Task<ActionResult<OeeSnapshotDto>> Calculate(
        [FromQuery] Guid equipmentId,
        [FromQuery] DateTime shiftDate,
        [FromQuery] Guid shiftId)
    {
        var shift = await _db.Set<ShiftDefinition>().FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null) return BadRequest("Shift not found.");

        var snapshot = await _oeeService.CalculateForEquipmentAsync(equipmentId, shiftDate, shift);
        if (snapshot is null) return NotFound("Equipment not found.");
        return snapshot;
    }

    // ── Mapping ───────────���───────────────────────��─────────────────────────

    private static ShiftDefinitionResponseDto MapToDto(ShiftDefinition s) => new(
        s.Id,
        s.Code,
        s.Name,
        s.StartTime.ToString("HH:mm"),
        s.EndTime.ToString("HH:mm"),
        s.IsActive,
        s.CreatedAt,
        s.UpdatedAt);
}
