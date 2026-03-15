using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PfmeasController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public PfmeasController(ProcessManagerDbContext db) => _db = db;

    // ─── PFMEA CRUD ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PfmeaSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? processId = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Pfmeas.Include(p => p.Process).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || p.Name.Contains(search));

        if (processId.HasValue)
            query = query.Where(p => p.ProcessId == processId.Value);

        if (active.HasValue)
            query = query.Where(p => p.IsActive == active.Value);

        var totalCount = await query.CountAsync();

        var pfmeas = await query
            .OrderBy(p => p.Process.Name).ThenBy(p => p.Version)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PfmeaSummaryDto(
                p.Id, p.Code, p.Name, p.Description, p.Version, p.IsActive,
                p.ProcessId, p.Process.Name, p.Process.Code,
                p.FailureModes.Count,
                p.FailureModes.SelectMany(f => f.Actions).Count(a =>
                    a.Status == Domain.Enums.PfmeaActionStatus.Open ||
                    a.Status == Domain.Enums.PfmeaActionStatus.InProgress),
                p.FailureModes.Any()
                    ? p.FailureModes.Max(f => f.Severity * f.Occurrence * f.Detection)
                    : 0,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

        return new PaginatedResponse<PfmeaSummaryDto>(pfmeas, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> GetById(Guid id)
    {
        var pfmea = await LoadPfmea(id);
        if (pfmea is null) return NotFound();
        return MapToDto(pfmea);
    }

    [HttpPost]
    public async Task<ActionResult<PfmeaResponseDto>> Create(PfmeaCreateDto dto)
    {
        if (await _db.Pfmeas.AnyAsync(p => p.Code == dto.Code))
            return Conflict($"A PFMEA with code '{dto.Code}' already exists.");

        var process = await _db.Processes
            .Include(p => p.ProcessSteps).ThenInclude(s => s.StepTemplate)
            .FirstOrDefaultAsync(p => p.Id == dto.ProcessId);

        if (process is null) return BadRequest("Process not found.");

        var pfmea = new Pfmea
        {
            ProcessId   = dto.ProcessId,
            Code        = dto.Code,
            Name        = dto.Name,
            Description = dto.Description,
        };

        _db.Pfmeas.Add(pfmea);
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmea.Id);
        return CreatedAtAction(nameof(GetById), new { id = pfmea.Id }, MapToDto(result!));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> Update(Guid id, PfmeaUpdateDto dto)
    {
        var pfmea = await LoadPfmea(id);
        if (pfmea is null) return NotFound();

        pfmea.Name        = dto.Name;
        pfmea.Description = dto.Description;
        if (dto.IsActive.HasValue) pfmea.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return MapToDto(pfmea);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var pfmea = await _db.Pfmeas.FindAsync(id);
        if (pfmea is null) return NotFound();
        _db.Pfmeas.Remove(pfmea);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Clear the staleness flag — reviewer has confirmed no PFMEA updates are required after the process revision.
    /// </summary>
    [HttpPost("{id:guid}/clear-staleness")]
    public async Task<ActionResult<PfmeaResponseDto>> ClearStaleness(Guid id, ClearPfmeaStalenessDto dto)
    {
        var pfmea = await LoadPfmea(id);
        if (pfmea is null) return NotFound();
        if (!pfmea.IsStale)
            return BadRequest("This PFMEA is not currently stale.");

        pfmea.IsStale = false;
        pfmea.StalenessClearedBy = dto.ClearedBy;
        pfmea.StalenessClearedAt = DateTime.UtcNow;
        pfmea.StalenessClearanceNotes = dto.ClearanceNotes;

        await _db.SaveChangesAsync();
        return MapToDto(pfmea);
    }

    /// <summary>
    /// Branch: creates a new PFMEA version from an existing one, copying all failure modes and actions.
    /// The original is left active; the caller may deactivate it.
    /// </summary>
    [HttpPost("{id:guid}/branch")]
    public async Task<ActionResult<PfmeaResponseDto>> Branch(Guid id)
    {
        var source = await LoadPfmea(id);
        if (source is null) return NotFound();

        // Generate a unique code for the new version
        var newVersion = source.Version + 1;
        var newCode    = $"{source.Code.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').TrimEnd('-', '_', 'V', 'v')}V{newVersion}";
        if (await _db.Pfmeas.AnyAsync(p => p.Code == newCode))
            newCode = $"{source.Code}-v{newVersion}-{Guid.NewGuid().ToString("N")[..6]}";

        var branched = new Pfmea
        {
            ProcessId   = source.ProcessId,
            Code        = newCode,
            Name        = source.Name,
            Description = source.Description,
            Version     = newVersion,
            IsActive    = true,
        };
        _db.Pfmeas.Add(branched);
        await _db.SaveChangesAsync();

        // Deep-copy failure modes and actions
        foreach (var fm in source.FailureModes)
        {
            var newFm = new PfmeaFailureMode
            {
                PfmeaId            = branched.Id,
                ProcessStepId      = fm.ProcessStepId,
                StepFunction       = fm.StepFunction,
                FailureMode        = fm.FailureMode,
                FailureEffect      = fm.FailureEffect,
                FailureCause       = fm.FailureCause,
                PreventionControls = fm.PreventionControls,
                DetectionControls  = fm.DetectionControls,
                Severity           = fm.Severity,
                Occurrence         = fm.Occurrence,
                Detection          = fm.Detection,
            };
            _db.PfmeaFailureModes.Add(newFm);
            foreach (var a in fm.Actions)
            {
                _db.PfmeaActions.Add(new PfmeaAction
                {
                    FailureModeId     = newFm.Id,
                    Description       = a.Description,
                    ResponsiblePerson = a.ResponsiblePerson,
                    TargetDate        = a.TargetDate,
                    Status            = a.Status,
                    CompletedDate     = a.CompletedDate,
                    CompletionNotes   = a.CompletionNotes,
                    RevisedOccurrence = a.RevisedOccurrence,
                    RevisedDetection  = a.RevisedDetection,
                });
            }
        }
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(branched.Id);
        return CreatedAtAction(nameof(GetById), new { id = branched.Id }, MapToDto(result!));
    }

    // ─── Failure Modes ────────────────────────────────────────────────────

    [HttpPost("{pfmeaId:guid}/failure-modes")]
    public async Task<ActionResult<PfmeaResponseDto>> AddFailureMode(
        Guid pfmeaId, PfmeaFailureModeCreateDto dto)
    {
        var pfmea = await _db.Pfmeas.FindAsync(pfmeaId);
        if (pfmea is null) return NotFound("PFMEA not found.");

        var stepExists = await _db.ProcessSteps
            .AnyAsync(s => s.Id == dto.ProcessStepId && s.ProcessId == pfmea.ProcessId);
        if (!stepExists) return BadRequest("ProcessStep does not belong to this PFMEA's process.");

        var fm = new PfmeaFailureMode
        {
            PfmeaId            = pfmeaId,
            ProcessStepId      = dto.ProcessStepId,
            StepFunction       = dto.StepFunction,
            FailureMode        = dto.FailureMode,
            FailureEffect      = dto.FailureEffect,
            FailureCause       = dto.FailureCause,
            PreventionControls = dto.PreventionControls,
            DetectionControls  = dto.DetectionControls,
            Severity           = dto.Severity,
            Occurrence         = dto.Occurrence,
            Detection          = dto.Detection,
        };
        _db.PfmeaFailureModes.Add(fm);
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{pfmeaId:guid}/failure-modes/{fmId:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> UpdateFailureMode(
        Guid pfmeaId, Guid fmId, PfmeaFailureModeUpdateDto dto)
    {
        var fm = await _db.PfmeaFailureModes
            .FirstOrDefaultAsync(f => f.Id == fmId && f.PfmeaId == pfmeaId);
        if (fm is null) return NotFound();

        fm.StepFunction       = dto.StepFunction;
        fm.FailureMode        = dto.FailureMode;
        fm.FailureEffect      = dto.FailureEffect;
        fm.FailureCause       = dto.FailureCause;
        fm.PreventionControls = dto.PreventionControls;
        fm.DetectionControls  = dto.DetectionControls;
        fm.Severity           = dto.Severity;
        fm.Occurrence         = dto.Occurrence;
        fm.Detection          = dto.Detection;

        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{pfmeaId:guid}/failure-modes/{fmId:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> DeleteFailureMode(Guid pfmeaId, Guid fmId)
    {
        var fm = await _db.PfmeaFailureModes
            .FirstOrDefaultAsync(f => f.Id == fmId && f.PfmeaId == pfmeaId);
        if (fm is null) return NotFound();
        _db.PfmeaFailureModes.Remove(fm);
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    // ─── Actions ──────────────────────────────────────────────────────────

    [HttpPost("{pfmeaId:guid}/failure-modes/{fmId:guid}/actions")]
    public async Task<ActionResult<PfmeaResponseDto>> AddAction(
        Guid pfmeaId, Guid fmId, PfmeaActionCreateDto dto)
    {
        var fm = await _db.PfmeaFailureModes
            .FirstOrDefaultAsync(f => f.Id == fmId && f.PfmeaId == pfmeaId);
        if (fm is null) return NotFound("Failure mode not found.");

        var action = new PfmeaAction
        {
            FailureModeId     = fmId,
            Description       = dto.Description,
            ResponsiblePerson = dto.ResponsiblePerson,
            TargetDate        = dto.TargetDate,
        };
        _db.PfmeaActions.Add(action);
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{pfmeaId:guid}/failure-modes/{fmId:guid}/actions/{actionId:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> UpdateAction(
        Guid pfmeaId, Guid fmId, Guid actionId, PfmeaActionUpdateDto dto)
    {
        var action = await _db.PfmeaActions
            .Include(a => a.FailureMode)
            .FirstOrDefaultAsync(a => a.Id == actionId && a.FailureModeId == fmId);
        if (action is null || action.FailureMode.PfmeaId != pfmeaId) return NotFound();

        action.Description       = dto.Description;
        action.ResponsiblePerson = dto.ResponsiblePerson;
        action.TargetDate        = dto.TargetDate;
        action.Status            = dto.Status;
        action.CompletedDate     = dto.CompletedDate;
        action.CompletionNotes   = dto.CompletionNotes;
        action.RevisedOccurrence = dto.RevisedOccurrence;
        action.RevisedDetection  = dto.RevisedDetection;

        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{pfmeaId:guid}/failure-modes/{fmId:guid}/actions/{actionId:guid}")]
    public async Task<ActionResult<PfmeaResponseDto>> DeleteAction(
        Guid pfmeaId, Guid fmId, Guid actionId)
    {
        var action = await _db.PfmeaActions
            .Include(a => a.FailureMode)
            .FirstOrDefaultAsync(a => a.Id == actionId && a.FailureModeId == fmId);
        if (action is null || action.FailureMode.PfmeaId != pfmeaId) return NotFound();
        _db.PfmeaActions.Remove(action);
        await _db.SaveChangesAsync();

        var result = await LoadPfmea(pfmeaId);
        return Ok(MapToDto(result!));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private async Task<Pfmea?> LoadPfmea(Guid id) =>
        await _db.Pfmeas
            .Include(p => p.Process)
            .Include(p => p.FailureModes.OrderBy(f => f.CreatedAt))
                .ThenInclude(f => f.ProcessStep).ThenInclude(s => s.StepTemplate)
            .Include(p => p.FailureModes)
                .ThenInclude(f => f.Actions.OrderBy(a => a.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

    private static PfmeaResponseDto MapToDto(Pfmea p) => new(
        p.Id, p.Code, p.Name, p.Description, p.Version, p.IsActive,
        p.ProcessId, p.Process.Name, p.Process.Code,
        p.ProcessVersion, p.IsStale, p.StalenessClearedBy, p.StalenessClearedAt, p.StalenessClearanceNotes,
        p.CreatedAt, p.UpdatedAt,
        p.FailureModes.OrderBy(f => f.ProcessStep.Sequence).ThenBy(f => f.CreatedAt)
            .Select(f => new PfmeaFailureModeResponseDto(
                f.Id, f.PfmeaId, f.ProcessStepId,
                f.ProcessStep?.NameOverride ?? f.ProcessStep?.StepTemplate?.Name ?? "Unknown",
                f.ProcessStep?.Sequence ?? 0,
                f.StepFunction, f.FailureMode, f.FailureEffect,
                f.FailureCause, f.PreventionControls, f.DetectionControls,
                f.Severity, f.Occurrence, f.Detection, f.Rpn,
                f.CreatedAt, f.UpdatedAt,
                f.Actions.OrderBy(a => a.CreatedAt).Select(a => new PfmeaActionResponseDto(
                    a.Id, a.FailureModeId, a.Description, a.ResponsiblePerson,
                    a.TargetDate, a.Status.ToString(), a.CompletedDate, a.CompletionNotes,
                    a.RevisedOccurrence, a.RevisedDetection, a.RevisedRpn,
                    a.CreatedAt, a.UpdatedAt
                )).ToList()
            )).ToList()
    );
}
