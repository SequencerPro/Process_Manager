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
public class ControlPlansController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public ControlPlansController(ProcessManagerDbContext db) => _db = db;

    // ─── ControlPlan CRUD ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ControlPlanSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? processId = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? stale = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.ControlPlans.Include(cp => cp.Process).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(cp => cp.Code.Contains(search) || cp.Name.Contains(search));
        if (processId.HasValue)
            query = query.Where(cp => cp.ProcessId == processId.Value);
        if (active.HasValue)
            query = query.Where(cp => cp.IsActive == active.Value);
        if (stale.HasValue)
            query = query.Where(cp => cp.IsStale == stale.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(cp => cp.Process.Name).ThenBy(cp => cp.Version)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(cp => new ControlPlanSummaryDto(
                cp.Id, cp.Code, cp.Name, cp.Description, cp.Version, cp.IsActive,
                cp.ProcessId, cp.Process.Name, cp.Process.Code,
                cp.Entries.Count,
                cp.IsStale,
                cp.CreatedAt, cp.UpdatedAt))
            .ToListAsync();

        return new PaginatedResponse<ControlPlanSummaryDto>(items, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ControlPlanResponseDto>> GetById(Guid id)
    {
        var cp = await LoadControlPlan(id);
        if (cp is null) return NotFound();
        return MapToDto(cp);
    }

    [HttpPost]
    public async Task<ActionResult<ControlPlanResponseDto>> Create(ControlPlanCreateDto dto)
    {
        if (await _db.ControlPlans.AnyAsync(cp => cp.Code == dto.Code))
            return Conflict($"A Control Plan with code '{dto.Code}' already exists.");

        var process = await _db.Processes
            .Include(p => p.ProcessSteps).ThenInclude(s => s.StepTemplate)
            .FirstOrDefaultAsync(p => p.Id == dto.ProcessId);
        if (process is null) return BadRequest("Process not found.");

        var cp = new ControlPlan
        {
            ProcessId   = dto.ProcessId,
            Code        = dto.Code,
            Name        = dto.Name,
            Description = dto.Description,
            ProcessVersion = process.Version,
        };
        _db.ControlPlans.Add(cp);
        await _db.SaveChangesAsync();

        // Auto-populate one entry row per ProcessStep
        var steps = process.ProcessSteps.OrderBy(s => s.Sequence).ToList();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            _db.ControlPlanEntries.Add(new ControlPlanEntry
            {
                ControlPlanId       = cp.Id,
                ProcessStepId       = step.Id,
                CharacteristicName  = step.NameOverride ?? step.StepTemplate.Name,
                CharacteristicType  = CharacteristicType.Product,
                SortOrder           = (i + 1) * 10,
            });
        }
        await _db.SaveChangesAsync();

        var result = await LoadControlPlan(cp.Id);
        return CreatedAtAction(nameof(GetById), new { id = cp.Id }, MapToDto(result!));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ControlPlanResponseDto>> Update(Guid id, ControlPlanUpdateDto dto)
    {
        var cp = await LoadControlPlan(id);
        if (cp is null) return NotFound();

        cp.Name        = dto.Name;
        cp.Description = dto.Description;
        if (dto.IsActive.HasValue) cp.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync();
        return MapToDto(cp);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cp = await _db.ControlPlans.FindAsync(id);
        if (cp is null) return NotFound();
        _db.ControlPlans.Remove(cp);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/clear-staleness")]
    public async Task<ActionResult<ControlPlanResponseDto>> ClearStaleness(Guid id, ClearControlPlanStalenessDto dto)
    {
        var cp = await LoadControlPlan(id);
        if (cp is null) return NotFound();
        if (!cp.IsStale) return BadRequest("This Control Plan is not currently stale.");

        cp.IsStale                = false;
        cp.StalenessClearedBy     = dto.ClearedBy;
        cp.StalenessClearedAt     = DateTime.UtcNow;
        cp.StalenessClearanceNotes = dto.ClearanceNotes;
        await _db.SaveChangesAsync();
        return MapToDto(cp);
    }

    // ─── Entries ──────────────────────────────────────────────────────────

    [HttpPost("{controlPlanId:guid}/entries")]
    public async Task<ActionResult<ControlPlanResponseDto>> AddEntry(Guid controlPlanId, ControlPlanEntryCreateDto dto)
    {
        var cp = await _db.ControlPlans.FindAsync(controlPlanId);
        if (cp is null) return NotFound("Control Plan not found.");

        var stepExists = await _db.ProcessSteps
            .AnyAsync(s => s.Id == dto.ProcessStepId && s.ProcessId == cp.ProcessId);
        if (!stepExists) return BadRequest("ProcessStep does not belong to this Control Plan's process.");

        _db.ControlPlanEntries.Add(new ControlPlanEntry
        {
            ControlPlanId              = controlPlanId,
            ProcessStepId              = dto.ProcessStepId,
            CharacteristicName         = dto.CharacteristicName,
            CharacteristicType         = dto.CharacteristicType,
            SpecificationOrTolerance   = dto.SpecificationOrTolerance,
            MeasurementTechnique       = dto.MeasurementTechnique,
            SampleSize                 = dto.SampleSize,
            SampleFrequency            = dto.SampleFrequency,
            ControlMethod              = dto.ControlMethod,
            ReactionPlan               = dto.ReactionPlan,
            LinkedPfmeaFailureModeId   = dto.LinkedPfmeaFailureModeId,
            LinkedPortId               = dto.LinkedPortId,
            SortOrder                  = dto.SortOrder,
        });
        await _db.SaveChangesAsync();

        var result = await LoadControlPlan(controlPlanId);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{controlPlanId:guid}/entries/{entryId:guid}")]
    public async Task<ActionResult<ControlPlanResponseDto>> UpdateEntry(
        Guid controlPlanId, Guid entryId, ControlPlanEntryUpdateDto dto)
    {
        var entry = await _db.ControlPlanEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.ControlPlanId == controlPlanId);
        if (entry is null) return NotFound();

        entry.CharacteristicName         = dto.CharacteristicName;
        entry.CharacteristicType         = dto.CharacteristicType;
        entry.SpecificationOrTolerance   = dto.SpecificationOrTolerance;
        entry.MeasurementTechnique       = dto.MeasurementTechnique;
        entry.SampleSize                 = dto.SampleSize;
        entry.SampleFrequency            = dto.SampleFrequency;
        entry.ControlMethod              = dto.ControlMethod;
        entry.ReactionPlan               = dto.ReactionPlan;
        entry.LinkedPfmeaFailureModeId   = dto.LinkedPfmeaFailureModeId;
        entry.LinkedPortId               = dto.LinkedPortId;
        entry.SortOrder                  = dto.SortOrder;
        await _db.SaveChangesAsync();

        var result = await LoadControlPlan(controlPlanId);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{controlPlanId:guid}/entries/{entryId:guid}")]
    public async Task<ActionResult<ControlPlanResponseDto>> DeleteEntry(Guid controlPlanId, Guid entryId)
    {
        var entry = await _db.ControlPlanEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.ControlPlanId == controlPlanId);
        if (entry is null) return NotFound();
        _db.ControlPlanEntries.Remove(entry);
        await _db.SaveChangesAsync();

        var result = await LoadControlPlan(controlPlanId);
        return Ok(MapToDto(result!));
    }

    // ─── CSV Export ───────────────────────────────────────────────────────

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        var cp = await LoadControlPlan(id);
        if (cp is null) return NotFound();

        var csv = BuildCsv(cp);
        return File(System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"control-plan-{cp.Name.Replace(" ", "_")}.csv");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private async Task<ControlPlan?> LoadControlPlan(Guid id) =>
        await _db.ControlPlans
            .Include(cp => cp.Process)
            .Include(cp => cp.Entries.OrderBy(e => e.SortOrder).ThenBy(e => e.CreatedAt))
                .ThenInclude(e => e.ProcessStep).ThenInclude(s => s.StepTemplate)
            .Include(cp => cp.Entries)
                .ThenInclude(e => e.LinkedPfmeaFailureMode)
            .Include(cp => cp.Entries)
                .ThenInclude(e => e.LinkedPort)
            .FirstOrDefaultAsync(cp => cp.Id == id);

    private static ControlPlanResponseDto MapToDto(ControlPlan cp) => new(
        cp.Id, cp.Code, cp.Name, cp.Description, cp.Version, cp.IsActive,
        cp.ProcessId, cp.Process.Name, cp.Process.Code,
        cp.ProcessVersion, cp.IsStale,
        cp.StalenessClearedBy, cp.StalenessClearedAt, cp.StalenessClearanceNotes,
        cp.CreatedAt, cp.UpdatedAt,
        cp.Entries
            .OrderBy(e => e.ProcessStep?.Sequence ?? 0)
            .ThenBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new ControlPlanEntryResponseDto(
                e.Id, e.ControlPlanId, e.ProcessStepId,
                e.ProcessStep?.NameOverride ?? e.ProcessStep?.StepTemplate?.Name ?? "Unknown",
                e.ProcessStep?.Sequence ?? 0,
                e.CharacteristicName,
                e.CharacteristicType.ToString(),
                e.SpecificationOrTolerance,
                e.MeasurementTechnique,
                e.SampleSize,
                e.SampleFrequency,
                e.ControlMethod,
                e.ReactionPlan,
                e.LinkedPfmeaFailureModeId,
                e.LinkedPfmeaFailureMode?.FailureMode,
                e.LinkedPortId,
                e.LinkedPort?.Name,
                e.SortOrder,
                e.CreatedAt, e.UpdatedAt
            )).ToList()
    );

    private static string BuildCsv(ControlPlan cp)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Step,Sequence,Characteristic,Type,Specification / Tolerance,Measurement Technique,Sample Size,Sample Frequency,Control Method,Reaction Plan,PFMEA Failure Mode,Port");

        foreach (var e in cp.Entries.OrderBy(e => e.ProcessStep?.Sequence ?? 0).ThenBy(e => e.SortOrder))
        {
            var stepName = e.ProcessStep?.NameOverride ?? e.ProcessStep?.StepTemplate?.Name ?? "";
            var seq      = e.ProcessStep?.Sequence.ToString() ?? "";
            var pfmea    = e.LinkedPfmeaFailureMode?.FailureMode ?? "";
            var port     = e.LinkedPort?.Name ?? "";

            sb.AppendLine(string.Join(",",
                CsvCell(stepName), CsvCell(seq),
                CsvCell(e.CharacteristicName), CsvCell(e.CharacteristicType.ToString()),
                CsvCell(e.SpecificationOrTolerance), CsvCell(e.MeasurementTechnique),
                CsvCell(e.SampleSize), CsvCell(e.SampleFrequency),
                CsvCell(e.ControlMethod), CsvCell(e.ReactionPlan),
                CsvCell(pfmea), CsvCell(port)));
        }

        return sb.ToString();
    }

    private static string CsvCell(string? v) =>
        v is null ? "" : $"\"{v.Replace("\"", "\"\"")}\"";
}
