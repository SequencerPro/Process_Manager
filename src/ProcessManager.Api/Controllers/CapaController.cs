using System.Security.Claims;
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
[Route("api/capas")]
public class CapaController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public CapaController(ProcessManagerDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CapaRecordSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.CapaRecords
            .Include(c => c.Steps)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CapaStatus>(status, true, out var st))
            query = query.Where(c => c.Status == st);

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<CapaType>(type, true, out var tp))
            query = query.Where(c => c.Type == tp);

        if (!string.IsNullOrWhiteSpace(sourceType) && Enum.TryParse<CapaSourceType>(sourceType, true, out var src))
            query = query.Where(c => c.SourceType == src);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(s)
                                  || c.ProblemStatement.ToLower().Contains(s)
                                  || c.OwnerDisplayName.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var capaIds = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync();

        var items = await query
            .Where(c => capaIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var actionItemCounts = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.Capa && capaIds.Contains(a.SourceEntityId!.Value))
            .GroupBy(a => a.SourceEntityId!.Value)
            .Select(g => new { CapaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CapaId, g => g.Count);

        var dtos = items.Select(c =>
        {
            actionItemCounts.TryGetValue(c.Id, out var aiCount);
            return MapToSummaryDto(c, aiCount);
        }).ToList();

        return new PaginatedResponse<CapaRecordSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CapaRecordResponseDto>> GetById(Guid id)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<CapaRecordResponseDto>> Create(CreateCapaRecordDto dto)
    {
        if (!Enum.TryParse<CapaType>(dto.Type, true, out var capaType))
            return BadRequest($"Invalid type '{dto.Type}'. Valid values: {string.Join(", ", Enum.GetNames<CapaType>())}");

        if (!Enum.TryParse<CapaSourceType>(dto.SourceType, true, out var sourceType))
            return BadRequest($"Invalid source type '{dto.SourceType}'. Valid values: {string.Join(", ", Enum.GetNames<CapaSourceType>())}");

        var year = DateTime.UtcNow.Year;
        var existingCount = await _db.CapaRecords
            .CountAsync(c => c.Code.StartsWith($"CAPA-{year}-"));
        var code = $"CAPA-{year}-{(existingCount + 1):D3}";

        var capa = new CapaRecord
        {
            Code = code,
            Type = capaType,
            SourceType = sourceType,
            SourceEntityId = dto.SourceEntityId,
            ProblemStatement = dto.ProblemStatement.Trim(),
            ContainmentAction = dto.ContainmentAction?.Trim(),
            OwnerUserId = dto.OwnerUserId.Trim(),
            OwnerDisplayName = dto.OwnerDisplayName.Trim(),
            TeamMemberIds = dto.TeamMemberIds?.Trim(),
            Status = CapaStatus.Open
        };

        _db.CapaRecords.Add(capa);

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = "Opened",
            CompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = $"CAPA {code} opened."
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = capa.Id }, MapToDto(capa, 0));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CapaRecordResponseDto>> Update(Guid id, UpdateCapaRecordDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status == CapaStatus.Closed)
            return BadRequest("Cannot update a closed CAPA.");

        if (dto.ProblemStatement != null) capa.ProblemStatement = dto.ProblemStatement.Trim();
        if (dto.ContainmentAction != null) capa.ContainmentAction = dto.ContainmentAction.Trim();
        if (dto.PermanentCorrectiveAction != null) capa.PermanentCorrectiveAction = dto.PermanentCorrectiveAction.Trim();
        if (dto.PreventiveAction != null) capa.PreventiveAction = dto.PreventiveAction.Trim();
        if (dto.VerificationMethod != null) capa.VerificationMethod = dto.VerificationMethod.Trim();
        if (dto.VerificationDueDate.HasValue) capa.VerificationDueDate = dto.VerificationDueDate.Value.ToUniversalTime();
        if (dto.EffectivenessReviewDate.HasValue) capa.EffectivenessReviewDate = dto.EffectivenessReviewDate.Value.ToUniversalTime();
        if (dto.TeamMemberIds != null) capa.TeamMemberIds = dto.TeamMemberIds.Trim();

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status != CapaStatus.Open)
            return BadRequest("Only CAPA records in 'Open' status can be deleted.");

        _db.CapaSteps.RemoveRange(capa.Steps);
        _db.CapaRecords.Remove(capa);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Lifecycle transitions ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/transition")]
    public async Task<ActionResult<CapaRecordResponseDto>> Transition(Guid id, TransitionCapaDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        var nextStatus = GetNextStatus(capa.Status);
        if (nextStatus is null)
            return BadRequest($"Cannot transition from '{capa.Status}'. CAPA is already closed.");

        if (nextStatus == CapaStatus.RootCauseAnalysis && capa.ContainmentAction == null)
            return BadRequest("Containment action must be documented before moving to Root Cause Analysis.");

        if (nextStatus == CapaStatus.Implementation && capa.RootCauseAnalysisId == null)
            return BadRequest("Root cause analysis must be linked before moving to Implementation.");

        if (nextStatus == CapaStatus.Verification && capa.PermanentCorrectiveAction == null)
            return BadRequest("Permanent corrective action must be documented before moving to Verification.");

        capa.Status = nextStatus.Value;

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = nextStatus.Value.ToString(),
            CompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<CapaRecordResponseDto>> Close(Guid id, TransitionCapaDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status != CapaStatus.EffectivenessReview)
            return BadRequest("CAPA can only be closed from the Effectiveness Review stage.");

        if (!capa.EffectivenessVerifiedAt.HasValue)
            return BadRequest("Effectiveness must be verified before closing.");

        capa.Status = CapaStatus.Closed;
        capa.ClosedAt = DateTime.UtcNow;

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = "Closed",
            CompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Link RCA ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/link-rca")]
    public async Task<ActionResult<CapaRecordResponseDto>> LinkRca(Guid id, LinkRcaDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status == CapaStatus.Closed)
            return BadRequest("Cannot link RCA to a closed CAPA.");

        var rcaType = dto.RootCauseAnalysisType.Trim().ToLower();
        if (rcaType == "ishikawa")
        {
            if (!await _db.IshikawaDiagrams.AnyAsync(i => i.Id == dto.RootCauseAnalysisId))
                return NotFound("Ishikawa diagram not found.");
        }
        else if (rcaType == "fivewhys")
        {
            if (!await _db.FiveWhysAnalyses.AnyAsync(f => f.Id == dto.RootCauseAnalysisId))
                return NotFound("Five Whys analysis not found.");
        }
        else
        {
            return BadRequest($"Invalid RCA type '{dto.RootCauseAnalysisType}'. Valid values: Ishikawa, FiveWhys");
        }

        capa.RootCauseAnalysisId = dto.RootCauseAnalysisId;
        capa.RootCauseAnalysisType = dto.RootCauseAnalysisType.Trim();

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = "RcaLinked",
            CompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = $"Linked {dto.RootCauseAnalysisType} analysis {dto.RootCauseAnalysisId}"
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Verify ────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<CapaRecordResponseDto>> Verify(Guid id, VerifyCapaDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status != CapaStatus.Verification)
            return BadRequest("CAPA must be in Verification stage to verify.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == capa.OwnerUserId)
            return BadRequest("CAPA cannot be verified by the owner (anti-self-certification).");

        capa.VerifiedByUserId = userId;
        capa.VerifiedAt = DateTime.UtcNow;

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = "Verified",
            CompletedByUserId = userId,
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Verify Effectiveness ──────────────────────────────────────────────────

    [HttpPost("{id:guid}/verify-effectiveness")]
    public async Task<ActionResult<CapaRecordResponseDto>> VerifyEffectiveness(Guid id, VerifyCapaDto dto)
    {
        var capa = await _db.CapaRecords
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (capa is null) return NotFound();

        if (capa.Status != CapaStatus.EffectivenessReview)
            return BadRequest("CAPA must be in Effectiveness Review stage.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        capa.EffectivenessVerifiedByUserId = userId;
        capa.EffectivenessVerifiedAt = DateTime.UtcNow;

        var step = new CapaStep
        {
            CapaRecordId = capa.Id,
            StepType = "EffectivenessVerified",
            CompletedByUserId = userId,
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };
        _db.CapaSteps.Add(step);

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id);

        return MapToDto(capa, aiCount);
    }

    // ── Steps CRUD ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/steps")]
    public async Task<ActionResult<List<CapaStepResponseDto>>> GetSteps(Guid id)
    {
        if (!await _db.CapaRecords.AnyAsync(c => c.Id == id))
            return NotFound();

        var steps = await _db.CapaSteps
            .Where(s => s.CapaRecordId == id)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return steps.Select(MapStepToDto).ToList();
    }

    [HttpPost("{id:guid}/steps")]
    public async Task<ActionResult<CapaStepResponseDto>> AddStep(Guid id, CreateCapaStepDto dto)
    {
        var capa = await _db.CapaRecords.FindAsync(id);
        if (capa is null) return NotFound();

        if (capa.Status == CapaStatus.Closed)
            return BadRequest("Cannot add steps to a closed CAPA.");

        var step = new CapaStep
        {
            CapaRecordId = id,
            StepType = dto.StepType.Trim(),
            CompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CompletedByDisplayName = User.FindFirstValue("display_name"),
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes?.Trim(),
            AttachmentFileName = dto.AttachmentFileName?.Trim()
        };

        _db.CapaSteps.Add(step);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSteps), new { id }, MapStepToDto(step));
    }

    // ── Action Items ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/action-items")]
    public async Task<ActionResult<List<ActionItemDto>>> GetActionItems(Guid id)
    {
        if (!await _db.CapaRecords.AnyAsync(c => c.Id == id))
            return NotFound();

        var items = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.Capa && a.SourceEntityId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(MapActionItemToDto).ToList();
    }

    [HttpPost("{id:guid}/action-items")]
    public async Task<ActionResult<ActionItemDto>> CreateActionItem(Guid id, CreateActionItemDto dto)
    {
        var capa = await _db.CapaRecords.FindAsync(id);
        if (capa is null) return NotFound();

        if (!Enum.TryParse<ActionItemPriority>(dto.Priority, true, out var priority))
            priority = ActionItemPriority.High;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var displayName = User.FindFirstValue("display_name") ?? "";

        var actionItem = new ActionItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            AssignedToUserId = dto.AssignedToUserId.Trim(),
            AssignedToDisplayName = dto.AssignedToDisplayName.Trim(),
            AssignedByUserId = userId,
            AssignedByDisplayName = displayName,
            DueDate = dto.DueDate.ToUniversalTime(),
            Priority = priority,
            SourceType = ActionItemSourceType.Capa,
            SourceEntityId = id
        };

        _db.ActionItems.Add(actionItem);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActionItems), new { id }, MapActionItemToDto(actionItem));
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<CapaDashboardDto>> GetDashboard()
    {
        var all = await _db.CapaRecords.ToListAsync();

        var open = all.Where(c => c.Status != CapaStatus.Closed).ToList();
        var closed = all.Where(c => c.Status == CapaStatus.Closed).ToList();

        var overdue = open.Where(c => c.VerificationDueDate.HasValue && c.VerificationDueDate.Value < DateTime.UtcNow).ToList();

        var avgDaysToClose = closed.Any()
            ? closed.Average(c => (c.ClosedAt!.Value - c.CreatedAt).TotalDays)
            : 0;

        var byStatus = all
            .GroupBy(c => c.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var bySource = all
            .GroupBy(c => c.SourceType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var effectivenessVerified = closed.Count(c => c.EffectivenessVerifiedAt.HasValue);
        var effectivenessRate = closed.Any() ? (double)effectivenessVerified / closed.Count * 100 : 0;

        var overdueCapaIds = overdue.Select(c => c.Id).ToList();
        var overdueActionItemCounts = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.Capa && overdueCapaIds.Contains(a.SourceEntityId!.Value))
            .GroupBy(a => a.SourceEntityId!.Value)
            .Select(g => new { CapaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CapaId, g => g.Count);

        var overdueDtos = overdue
            .OrderBy(c => c.VerificationDueDate)
            .Take(10)
            .Select(c =>
            {
                overdueActionItemCounts.TryGetValue(c.Id, out var aiCount);
                return MapToSummaryDto(c, aiCount);
            })
            .ToList();

        return new CapaDashboardDto(
            open.Count,
            overdue.Count,
            closed.Count,
            Math.Round(avgDaysToClose, 1),
            byStatus,
            bySource,
            Math.Round(effectivenessRate, 1),
            overdueDtos);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static CapaStatus? GetNextStatus(CapaStatus current) => current switch
    {
        CapaStatus.Open => CapaStatus.Containment,
        CapaStatus.Containment => CapaStatus.RootCauseAnalysis,
        CapaStatus.RootCauseAnalysis => CapaStatus.Implementation,
        CapaStatus.Implementation => CapaStatus.Verification,
        CapaStatus.Verification => CapaStatus.EffectivenessReview,
        _ => null
    };

    private static CapaRecordResponseDto MapToDto(CapaRecord c, int actionItemCount)
    {
        return new CapaRecordResponseDto(
            c.Id, c.Code, c.Type.ToString(), c.SourceType.ToString(),
            c.SourceEntityId, c.ProblemStatement,
            c.ContainmentAction,
            c.RootCauseAnalysisId, c.RootCauseAnalysisType,
            c.PermanentCorrectiveAction, c.PreventiveAction,
            c.VerificationMethod, c.VerificationDueDate,
            c.VerifiedByUserId, c.VerifiedAt,
            c.EffectivenessReviewDate,
            c.EffectivenessVerifiedByUserId, c.EffectivenessVerifiedAt,
            c.Status.ToString(),
            c.OwnerUserId, c.OwnerDisplayName,
            c.TeamMemberIds, c.ClosedAt,
            c.Steps.Count, actionItemCount,
            c.CreatedAt, c.UpdatedAt);
    }

    private static CapaRecordSummaryDto MapToSummaryDto(CapaRecord c, int actionItemCount)
    {
        return new CapaRecordSummaryDto(
            c.Id, c.Code, c.Type.ToString(), c.SourceType.ToString(),
            c.Status.ToString(), c.ProblemStatement,
            c.OwnerDisplayName, c.VerificationDueDate,
            c.ClosedAt, c.Steps.Count, actionItemCount,
            c.CreatedAt);
    }

    private static ActionItemDto MapActionItemToDto(ActionItem a)
    {
        var isOverdue = a.Status != ActionItemStatus.Complete
                     && a.Status != ActionItemStatus.Verified
                     && a.Status != ActionItemStatus.Cancelled
                     && a.DueDate < DateTime.UtcNow;
        return new ActionItemDto(
            a.Id, a.Title, a.Description,
            a.AssignedToUserId, a.AssignedToDisplayName,
            a.AssignedByUserId, a.AssignedByDisplayName,
            a.DueDate, a.Priority.ToString(), a.Status.ToString(),
            a.SourceType.ToString(), a.SourceEntityId,
            a.CompletedBy, a.CompletedAt, a.CompletionNotes,
            a.VerifiedBy, a.VerifiedAt,
            a.CreatedAt, a.CreatedBy, a.UpdatedAt, isOverdue);
    }

    private static CapaStepResponseDto MapStepToDto(CapaStep s)
    {
        return new CapaStepResponseDto(
            s.Id, s.CapaRecordId, s.StepType,
            s.CompletedByUserId, s.CompletedByDisplayName,
            s.CompletedAt, s.Notes, s.AttachmentFileName,
            s.CreatedAt);
    }
}
