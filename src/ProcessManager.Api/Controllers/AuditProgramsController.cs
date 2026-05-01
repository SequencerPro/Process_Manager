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
[Route("api/audit-programs")]
public class AuditProgramsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public AuditProgramsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditProgramSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.AuditPrograms
            .Include(p => p.Audits)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AuditProgramStatus>(status, true, out var s))
            query = query.Where(p => p.Status == s);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<AuditProgramSummaryDto>(
            items.Select(MapToSummary).ToList(),
            totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditProgramDto>> GetById(Guid id)
    {
        var program = await _db.AuditPrograms
            .Include(p => p.Audits)
                .ThenInclude(a => a.Findings)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program is null) return NotFound();
        return MapToDto(program);
    }

    [HttpPost]
    public async Task<ActionResult<AuditProgramDto>> Create([FromBody] CreateAuditProgramDto dto)
    {
        if (!Enum.TryParse<ConformanceStandard>(dto.Standard, true, out var std))
            return BadRequest($"Invalid standard: {dto.Standard}");

        var program = new AuditProgram
        {
            Name = dto.Name,
            Standard = std,
            Year = dto.Year,
            LeadAuditor = dto.LeadAuditor,
            Status = AuditProgramStatus.Planning
        };

        _db.AuditPrograms.Add(program);
        await _db.SaveChangesAsync();

        return Created($"api/audit-programs/{program.Id}", MapToDto(program));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AuditProgramDto>> Update(Guid id, [FromBody] UpdateAuditProgramDto dto)
    {
        var program = await _db.AuditPrograms
            .Include(p => p.Audits)
                .ThenInclude(a => a.Findings)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program is null) return NotFound();

        if (!Enum.TryParse<ConformanceStandard>(dto.Standard, true, out var std))
            return BadRequest($"Invalid standard: {dto.Standard}");

        program.Name = dto.Name;
        program.Standard = std;
        program.Year = dto.Year;
        program.LeadAuditor = dto.LeadAuditor;

        await _db.SaveChangesAsync();
        return MapToDto(program);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<AuditProgramDto>> Activate(Guid id)
    {
        var program = await _db.AuditPrograms
            .Include(p => p.Audits).ThenInclude(a => a.Findings)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (program is null) return NotFound();
        if (program.Status != AuditProgramStatus.Planning)
            return BadRequest("Only Planning programmes can be activated.");

        program.Status = AuditProgramStatus.Active;
        await _db.SaveChangesAsync();
        return MapToDto(program);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<AuditProgramDto>> Close(Guid id)
    {
        var program = await _db.AuditPrograms
            .Include(p => p.Audits).ThenInclude(a => a.Findings)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (program is null) return NotFound();
        if (program.Status != AuditProgramStatus.Active)
            return BadRequest("Only Active programmes can be closed.");

        program.Status = AuditProgramStatus.Closed;
        await _db.SaveChangesAsync();
        return MapToDto(program);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var program = await _db.AuditPrograms
            .Include(p => p.Audits)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (program is null) return NotFound();
        if (program.Audits.Any())
            return BadRequest("Cannot delete a programme that has audits. Delete the audits first.");

        _db.AuditPrograms.Remove(program);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AuditProgramSummaryDto MapToSummary(AuditProgram p) =>
        new(p.Id, p.Name, p.Standard.ToString(), p.Year, p.LeadAuditor,
            p.Status.ToString(), p.Audits.Count);

    private static AuditProgramDto MapToDto(AuditProgram p)
    {
        var openFindings = p.Audits
            .SelectMany(a => a.Findings)
            .Count(f => f.Status != FindingStatus.Closed);

        return new AuditProgramDto(
            p.Id, p.Name, p.Standard.ToString(), p.Year, p.LeadAuditor,
            p.Status.ToString(), p.Audits.Count, openFindings, p.CreatedAt);
    }
}
