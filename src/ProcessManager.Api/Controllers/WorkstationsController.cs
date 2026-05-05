using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/workstations")]
public class WorkstationsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public WorkstationsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WorkstationSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Workstations
            .Include(w => w.FixedLocation)
            .Include(w => w.ApiKeys)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(w => w.Code.ToLower().Contains(s) || w.Name.ToLower().Contains(s));
        }

        if (active.HasValue)
            query = query.Where(w => w.IsActive == active.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(w => w.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<WorkstationSummaryDto>(
            items.Select(w => new WorkstationSummaryDto(
                w.Id, w.Code, w.Name, w.FixedLocation.Code, w.IsActive, w.ApiKeys.Count)).ToList(),
            totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkstationResponseDto>> GetById(Guid id)
    {
        var ws = await _db.Workstations
            .Include(w => w.FixedLocation)
            .Include(w => w.ApiKeys)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ws is null) return NotFound();

        var lastScan = await _db.ScanEvents
            .Where(s => s.WorkstationId == id)
            .OrderByDescending(s => s.ScannedAt)
            .Select(s => (DateTime?)s.ScannedAt)
            .FirstOrDefaultAsync();

        return MapToDto(ws, lastScan);
    }

    [HttpPost]
    public async Task<ActionResult<WorkstationResponseDto>> Create(CreateWorkstationDto dto)
    {
        if (await _db.Workstations.AnyAsync(w => w.Code == dto.Code.Trim()))
            return Conflict($"A workstation with code '{dto.Code}' already exists.");

        var location = await _db.StorageLocations.FindAsync(dto.FixedLocationId);
        if (location is null || !location.IsActive)
            return BadRequest("Fixed location not found or inactive.");

        var ws = new Workstation
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            FixedLocationId = dto.FixedLocationId
        };

        _db.Workstations.Add(ws);
        await _db.SaveChangesAsync();

        ws.FixedLocation = location;
        return CreatedAtAction(nameof(GetById), new { id = ws.Id }, MapToDto(ws, null));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkstationResponseDto>> Update(Guid id, UpdateWorkstationDto dto)
    {
        var ws = await _db.Workstations
            .Include(w => w.FixedLocation)
            .Include(w => w.ApiKeys)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ws is null) return NotFound();

        var location = await _db.StorageLocations.FindAsync(dto.FixedLocationId);
        if (location is null || !location.IsActive)
            return BadRequest("Fixed location not found or inactive.");

        ws.Name = dto.Name.Trim();
        ws.Description = dto.Description?.Trim();
        ws.FixedLocationId = dto.FixedLocationId;
        ws.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        ws.FixedLocation = location;
        return MapToDto(ws, null);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ws = await _db.Workstations
            .Include(w => w.ApiKeys)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ws is null) return NotFound();

        if (ws.ApiKeys.Any(k => k.IsActive))
            return Conflict("Cannot delete a workstation with active API keys. Deactivate or remove them first.");

        ws.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WorkstationResponseDto MapToDto(Workstation ws, DateTime? lastScan) => new(
        ws.Id, ws.Code, ws.Name, ws.Description,
        ws.FixedLocationId, ws.FixedLocation.Code, ws.FixedLocation.Name,
        ws.IsActive, ws.ApiKeys?.Count ?? 0, lastScan,
        ws.CreatedAt, ws.UpdatedAt);
}
