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
[Route("api/non-conformances")]
public class NonConformancesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public NonConformancesController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NonConformanceResponseDto>>> GetAll(
        [FromQuery] Guid? jobId = null,
        [FromQuery] Guid? stepExecutionId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.NonConformances
            .Include(nc => nc.StepExecution).ThenInclude(se => se.Job)
            .Include(nc => nc.StepExecution).ThenInclude(se => se.ProcessStep)
            .Include(nc => nc.ContentBlock)
            .AsQueryable();

        if (jobId.HasValue)
            query = query.Where(nc => nc.StepExecution.JobId == jobId.Value);

        if (stepExecutionId.HasValue)
            query = query.Where(nc => nc.StepExecutionId == stepExecutionId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DispositionStatus>(status, true, out var ds))
            query = query.Where(nc => nc.DispositionStatus == ds);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(nc => nc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<NonConformanceResponseDto>(
            items.Select(MapToDto).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NonConformanceResponseDto>> GetById(Guid id)
    {
        var nc = await LoadNonConformance(id);
        if (nc is null) return NotFound();
        return MapToDto(nc);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<NonConformanceResponseDto>> Create(CreateNonConformanceDto dto)
    {
        var stepExecution = await _db.StepExecutions
            .Include(se => se.Job)
            .Include(se => se.ProcessStep)
            .FirstOrDefaultAsync(se => se.Id == dto.StepExecutionId);

        if (stepExecution is null) return BadRequest("StepExecution not found.");

        var contentBlock = await _db.ProcessStepContents.FindAsync(dto.ContentBlockId);
        if (contentBlock is null) return BadRequest("ContentBlock not found.");

        if (!Enum.TryParse<LimitType>(dto.LimitType, true, out var limitType))
            return BadRequest($"Invalid LimitType '{dto.LimitType}'. Expected: LSL, USL, FailResult.");

        var nc = new NonConformance
        {
            StepExecutionId = dto.StepExecutionId,
            ContentBlockId = dto.ContentBlockId,
            ActualValue = dto.ActualValue,
            LimitType = limitType,
            DispositionStatus = DispositionStatus.Pending
        };

        _db.NonConformances.Add(nc);
        await _db.SaveChangesAsync();

        var result = await LoadNonConformance(nc.Id);
        return CreatedAtAction(nameof(GetById), new { id = nc.Id }, MapToDto(result!));
    }

    // ───── Dispose ─────

    [HttpPost("{id:guid}/dispose")]
    public async Task<ActionResult<NonConformanceResponseDto>> Dispose(Guid id, DispositionNonConformanceDto dto)
    {
        if (!Enum.TryParse<DispositionStatus>(dto.DispositionStatus, true, out var status))
            return BadRequest($"Invalid DispositionStatus '{dto.DispositionStatus}'.");

        if (status == DispositionStatus.Pending)
            return BadRequest("Cannot set disposition to Pending.");

        if (status == DispositionStatus.UseAsIs && string.IsNullOrWhiteSpace(dto.JustificationText))
            return BadRequest("JustificationText is required for UseAsIs disposition.");

        var nc = await LoadNonConformance(id);
        if (nc is null) return NotFound();

        if (nc.DispositionStatus != DispositionStatus.Pending)
            return Conflict($"Non-conformance already disposed with status '{nc.DispositionStatus}'.");

        nc.DispositionStatus = status;
        nc.DisposedBy = dto.DisposedBy;
        nc.DisposedAt = DateTime.UtcNow;
        nc.JustificationText = dto.JustificationText;

        // Quarantine automatically requires MRB review
        if (status == DispositionStatus.Quarantine)
            nc.MrbRequired = true;

        await _db.SaveChangesAsync();
        return MapToDto(nc);
    }

    // ───── Helpers ─────

    private async Task<NonConformance?> LoadNonConformance(Guid id)
    {
        return await _db.NonConformances
            .Include(nc => nc.StepExecution).ThenInclude(se => se.Job)
            .Include(nc => nc.StepExecution).ThenInclude(se => se.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .Include(nc => nc.ContentBlock)
            .FirstOrDefaultAsync(nc => nc.Id == id);
    }

    private static NonConformanceResponseDto MapToDto(NonConformance nc) => new(
        nc.Id,
        nc.StepExecutionId,
        nc.ContentBlockId,
        nc.ContentBlock?.Label,
        nc.StepExecution?.ProcessStep?.NameOverride ?? nc.StepExecution?.ProcessStep?.StepTemplate?.Name,
        nc.StepExecution?.Job?.Code,
        nc.ActualValue,
        nc.LimitType.ToString(),
        nc.DispositionStatus.ToString(),
        nc.DisposedBy,
        nc.DisposedAt,
        nc.JustificationText,
        nc.MrbRequired,
        nc.MrbReviewId,
        nc.CreatedAt,
        nc.UpdatedAt
    );
}
