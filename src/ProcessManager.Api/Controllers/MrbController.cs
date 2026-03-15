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
[Route("api/mrb")]
public class MrbController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public MrbController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MrbReviewSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? nonConformanceId = null,
        [FromQuery] bool? scarRequired = null,
        [FromQuery] bool? supplierCaused = null,
        [FromQuery] string? dispositionDecision = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.MrbReviews
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc.StepExecution)
                    .ThenInclude(se => se.Job)
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc.StepExecution)
                    .ThenInclude(se => se.ProcessStep)
            .Include(m => m.Participants)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MrbStatus>(status, true, out var s))
            query = query.Where(m => m.Status == s);

        if (nonConformanceId.HasValue)
            query = query.Where(m => m.NonConformanceId == nonConformanceId.Value);

        if (scarRequired.HasValue)
            query = query.Where(m => m.ScarRequired == scarRequired.Value);

        if (supplierCaused.HasValue)
            query = query.Where(m => m.SupplierCaused == supplierCaused.Value);

        if (!string.IsNullOrEmpty(dispositionDecision) &&
            Enum.TryParse<MrbDispositionDecision>(dispositionDecision, true, out var dd))
            query = query.Where(m => m.DispositionDecision == dd);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<MrbReviewSummaryDto>(
            items.Select(MapToSummary).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MrbReviewResponseDto>> GetById(Guid id)
    {
        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        return MapToDto(mrb);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<MrbReviewResponseDto>> Create(MrbReviewCreateDto dto)
    {
        var nc = await _db.NonConformances
            .Include(nc => nc.StepExecution).ThenInclude(se => se.Job)
            .Include(nc => nc.StepExecution).ThenInclude(se => se.ProcessStep)
            .Include(nc => nc.ContentBlock)
            .FirstOrDefaultAsync(nc => nc.Id == dto.NonConformanceId);

        if (nc is null) return BadRequest("NonConformance not found.");

        // One MRB per NC
        if (nc.MrbReviewId.HasValue)
            return Conflict($"This non-conformance already has an MRB review (Id: {nc.MrbReviewId}).");

        var mrb = new MrbReview
        {
            NonConformanceId          = dto.NonConformanceId,
            ItemDescription           = dto.ItemDescription.Trim(),
            ProblemStatement          = dto.ProblemStatement.Trim(),
            QuantityAffected          = dto.QuantityAffected?.Trim(),
            CustomerNotificationRequired = dto.CustomerNotificationRequired,
            ScarRequired              = dto.ScarRequired,
            SupplierCaused            = dto.SupplierCaused,
            RequiresRca               = dto.RequiresRca,
            Status                    = MrbStatus.Draft,
            CreatedBy                 = User.Identity?.Name
        };

        _db.MrbReviews.Add(mrb);
        await _db.SaveChangesAsync();

        // Back-link the NC to this MRB
        nc.MrbReviewId = mrb.Id;
        nc.MrbRequired = true;
        await _db.SaveChangesAsync();

        var result = await LoadMrb(mrb.Id);
        return CreatedAtAction(nameof(GetById), new { id = mrb.Id }, MapToDto(result!));
    }

    // ───── Update header ─────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MrbReviewResponseDto>> Update(Guid id, MrbReviewUpdateDto dto)
    {
        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status == MrbStatus.Closed)
            return Conflict("MRB review is closed and cannot be modified.");

        mrb.ItemDescription              = dto.ItemDescription.Trim();
        mrb.ProblemStatement             = dto.ProblemStatement.Trim();
        mrb.QuantityAffected             = dto.QuantityAffected?.Trim();
        mrb.CustomerNotificationRequired = dto.CustomerNotificationRequired;
        mrb.ScarRequired                 = dto.ScarRequired;
        mrb.SupplierCaused               = dto.SupplierCaused;
        mrb.RequiresRca                  = dto.RequiresRca;

        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    // ───── Status transitions ─────

    [HttpPost("{id:guid}/start-review")]
    public async Task<ActionResult<MrbReviewResponseDto>> StartReview(Guid id)
    {
        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status != MrbStatus.Draft)
            return Conflict($"Cannot start review — current status is '{mrb.Status}'.");

        mrb.Status = MrbStatus.UnderReview;
        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    [HttpPost("{id:guid}/decide")]
    public async Task<ActionResult<MrbReviewResponseDto>> Decide(Guid id, MrbDecisionDto dto)
    {
        if (!Enum.TryParse<MrbDispositionDecision>(dto.DispositionDecision, true, out var decision))
            return BadRequest($"Invalid DispositionDecision '{dto.DispositionDecision}'. Valid: {string.Join(", ", Enum.GetNames<MrbDispositionDecision>())}");

        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status == MrbStatus.Closed)
            return Conflict("MRB review is already closed.");
        if (mrb.Status == MrbStatus.Draft)
            return Conflict("MRB review must be started (UnderReview) before a decision can be recorded.");

        if (mrb.RequiresRca && mrb.LinkedRcaId is null)
            return Conflict("RequiresRca is set — link an RCA analysis before recording a decision.");

        mrb.DispositionDecision      = decision;
        mrb.DispositionJustification = dto.DispositionJustification?.Trim();
        mrb.DecidedBy                = dto.DecidedBy?.Trim();
        mrb.DecidedAt                = DateTime.UtcNow;
        mrb.Status                   = MrbStatus.Decided;

        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<MrbReviewResponseDto>> Close(Guid id)
    {
        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status == MrbStatus.Closed)
            return Conflict("MRB review is already closed.");
        if (mrb.Status != MrbStatus.Decided)
            return Conflict("MRB review must be in Decided status before it can be closed.");

        mrb.Status = MrbStatus.Closed;
        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<MrbReviewResponseDto>> Reopen(Guid id)
    {
        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status != MrbStatus.Closed)
            return Conflict("Only a Closed MRB review can be reopened.");

        mrb.Status = MrbStatus.UnderReview;
        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    // ───── Link RCA ─────

    [HttpPost("{id:guid}/link-rca")]
    public async Task<ActionResult<MrbReviewResponseDto>> LinkRca(Guid id, MrbLinkRcaDto dto)
    {
        if (!Enum.TryParse<MrbLinkedRcaType>(dto.LinkedRcaAnalysisType, true, out var rcaType))
            return BadRequest($"Invalid LinkedRcaAnalysisType '{dto.LinkedRcaAnalysisType}'. Valid: Ishikawa, FiveWhys");

        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status == MrbStatus.Closed)
            return Conflict("MRB review is closed and cannot be modified.");

        // Validate RCA exists
        bool rcaExists = rcaType == MrbLinkedRcaType.Ishikawa
            ? await _db.IshikawaDiagrams.AnyAsync(d => d.Id == dto.LinkedRcaId)
            : await _db.FiveWhysAnalyses.AnyAsync(a => a.Id == dto.LinkedRcaId);

        if (!rcaExists)
            return BadRequest($"No {rcaType} analysis found with Id {dto.LinkedRcaId}.");

        mrb.LinkedRcaAnalysisType = rcaType;
        mrb.LinkedRcaId           = dto.LinkedRcaId;
        await _db.SaveChangesAsync();
        return MapToDto(mrb);
    }

    // ───── Participants ─────

    [HttpGet("{id:guid}/participants")]
    public async Task<ActionResult<List<MrbParticipantDto>>> GetParticipants(Guid id)
    {
        if (!await _db.MrbReviews.AnyAsync(m => m.Id == id))
            return NotFound();

        var participants = await _db.MrbParticipants
            .Where(p => p.MrbReviewId == id)
            .OrderBy(p => p.Role.ToString())
            .ToListAsync();

        return participants.Select(MapParticipant).ToList();
    }

    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult<MrbReviewResponseDto>> AddParticipant(Guid id, MrbAddParticipantDto dto)
    {
        if (!Enum.TryParse<MrbParticipantRole>(dto.Role, true, out var role))
            return BadRequest($"Invalid Role '{dto.Role}'. Valid: {string.Join(", ", Enum.GetNames<MrbParticipantRole>())}");

        var mrb = await LoadMrb(id);
        if (mrb is null) return NotFound();
        if (mrb.Status == MrbStatus.Closed)
            return Conflict("MRB review is closed.");

        var participant = new MrbParticipant
        {
            MrbReviewId = id,
            UserId      = dto.UserId.Trim(),
            DisplayName = dto.DisplayName.Trim(),
            Role        = role,
            IsRequired  = dto.IsRequired
        };

        _db.MrbParticipants.Add(participant);
        await _db.SaveChangesAsync();

        var result = await LoadMrb(id);
        return MapToDto(result!);
    }

    [HttpPut("{id:guid}/participants/{participantId:guid}")]
    public async Task<ActionResult<MrbReviewResponseDto>> UpdateParticipantAssessment(
        Guid id, Guid participantId, MrbUpdateAssessmentDto dto)
    {
        var participant = await _db.MrbParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.MrbReviewId == id);
        if (participant is null) return NotFound();

        var mrb = await _db.MrbReviews.FindAsync(id);
        if (mrb?.Status == MrbStatus.Closed)
            return Conflict("MRB review is closed.");

        participant.Assessment  = dto.Assessment.Trim();
        participant.AssessedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = await LoadMrb(id);
        return MapToDto(result!);
    }

    [HttpDelete("{id:guid}/participants/{participantId:guid}")]
    public async Task<ActionResult<MrbReviewResponseDto>> RemoveParticipant(Guid id, Guid participantId)
    {
        var participant = await _db.MrbParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.MrbReviewId == id);
        if (participant is null) return NotFound();

        var mrb = await _db.MrbReviews.FindAsync(id);
        if (mrb?.Status == MrbStatus.Closed)
            return Conflict("MRB review is closed.");

        _db.MrbParticipants.Remove(participant);
        await _db.SaveChangesAsync();

        var result = await LoadMrb(id);
        return MapToDto(result!);
    }

    // ───── Helpers ─────

    private async Task<MrbReview?> LoadMrb(Guid id)
    {
        return await _db.MrbReviews
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc.StepExecution)
                    .ThenInclude(se => se.Job)
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc.StepExecution)
                    .ThenInclude(se => se.ProcessStep)
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc.ContentBlock)
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    private static MrbReviewSummaryDto MapToSummary(MrbReview m) => new(
        m.Id,
        m.NonConformanceId,
        m.NonConformance?.StepExecution?.Job?.Code,
        m.NonConformance?.StepExecution?.ProcessStep?.NameOverride
            ?? m.NonConformance?.StepExecution?.ProcessStep?.StepTemplate?.Name,
        m.NonConformance?.ActualValue,
        m.Status.ToString(),
        m.ItemDescription,
        m.QuantityAffected,
        m.CustomerNotificationRequired,
        m.ScarRequired,
        m.SupplierCaused,
        m.RequiresRca,
        m.DispositionDecision?.ToString(),
        m.DecidedBy,
        m.DecidedAt,
        m.Participants.Count,
        m.CreatedAt,
        m.CreatedBy
    );

    private static MrbReviewResponseDto MapToDto(MrbReview m) => new(
        m.Id,
        m.NonConformanceId,
        m.NonConformance?.StepExecution?.Job?.Code,
        m.NonConformance?.StepExecution?.ProcessStep?.NameOverride
            ?? m.NonConformance?.StepExecution?.ProcessStep?.StepTemplate?.Name,
        m.NonConformance?.ContentBlock?.Label,
        m.NonConformance?.ActualValue,
        m.NonConformance?.LimitType.ToString(),
        m.NonConformance?.DispositionStatus.ToString(),
        m.Status.ToString(),
        m.ItemDescription,
        m.QuantityAffected,
        m.ProblemStatement,
        m.CustomerNotificationRequired,
        m.ScarRequired,
        m.SupplierCaused,
        m.RequiresRca,
        m.LinkedRcaAnalysisType?.ToString(),
        m.LinkedRcaId,
        m.DispositionDecision?.ToString(),
        m.DispositionJustification,
        m.DecidedBy,
        m.DecidedAt,
        m.CreatedAt,
        m.CreatedBy,
        m.UpdatedAt,
        m.Participants.Select(MapParticipant).ToList()
    );

    private static MrbParticipantDto MapParticipant(MrbParticipant p) => new(
        p.Id,
        p.UserId,
        p.DisplayName,
        p.Role.ToString(),
        p.IsRequired,
        p.Assessment,
        p.AssessedAt
    );
}
