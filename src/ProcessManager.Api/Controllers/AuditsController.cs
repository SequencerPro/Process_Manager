using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using System.Security.Claims;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/audits")]
public class AuditsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public AuditsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditSummaryDto>>> GetAll(
        [FromQuery] Guid? programId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Audits
            .Include(a => a.Findings)
            .AsQueryable();

        if (programId.HasValue)
            query = query.Where(a => a.ProgramId == programId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AuditStatus>(status, true, out var s))
            query = query.Where(a => a.Status == s);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PlannedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<AuditSummaryDto>(
            items.Select(MapToSummary).ToList(),
            totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditDto>> GetById(Guid id)
    {
        var audit = await _db.Audits
            .Include(a => a.Program)
            .Include(a => a.Findings)
                .ThenInclude(f => f.Clause)
            .Include(a => a.Findings)
                .ThenInclude(f => f.ActionItem)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (audit is null) return NotFound();
        return MapToDto(audit);
    }

    [HttpPost]
    public async Task<ActionResult<AuditDto>> Create([FromBody] CreateAuditDto dto)
    {
        var program = await _db.AuditPrograms.FindAsync(dto.ProgramId);
        if (program is null) return NotFound("Audit programme not found.");

        if (!Enum.TryParse<AuditType>(dto.AuditType, true, out var auditType))
            return BadRequest($"Invalid audit type: {dto.AuditType}");

        var audit = new Audit
        {
            ProgramId = dto.ProgramId,
            AuditType = auditType,
            Scope = dto.Scope,
            PlannedDate = dto.PlannedDate,
            LeadAuditor = dto.LeadAuditor,
            Status = AuditStatus.Planned
        };

        _db.Audits.Add(audit);
        await _db.SaveChangesAsync();

        audit.Program = program;
        return Created($"api/audits/{audit.Id}", MapToDto(audit));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AuditDto>> Update(Guid id, [FromBody] UpdateAuditDto dto)
    {
        var audit = await _db.Audits
            .Include(a => a.Program)
            .Include(a => a.Findings).ThenInclude(f => f.Clause)
            .Include(a => a.Findings).ThenInclude(f => f.ActionItem)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (audit is null) return NotFound();

        if (!Enum.TryParse<AuditType>(dto.AuditType, true, out var auditType))
            return BadRequest($"Invalid audit type: {dto.AuditType}");

        audit.AuditType = auditType;
        audit.Scope = dto.Scope;
        audit.PlannedDate = dto.PlannedDate;
        audit.ActualDate = dto.ActualDate;
        audit.LeadAuditor = dto.LeadAuditor;

        await _db.SaveChangesAsync();
        return MapToDto(audit);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<AuditDto>> Start(Guid id)
    {
        var audit = await LoadAudit(id);
        if (audit is null) return NotFound();
        if (audit.Status != AuditStatus.Planned)
            return BadRequest("Only Planned audits can be started.");

        audit.Status = AuditStatus.InProgress;
        audit.ActualDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(audit);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<AuditDto>> Complete(Guid id)
    {
        var audit = await LoadAudit(id);
        if (audit is null) return NotFound();
        if (audit.Status != AuditStatus.InProgress)
            return BadRequest("Only InProgress audits can be completed.");

        audit.Status = AuditStatus.Complete;
        await _db.SaveChangesAsync();
        return MapToDto(audit);
    }

    // ── Findings ────────────────────────────────────────────────────────

    [HttpGet("{auditId:guid}/findings")]
    public async Task<ActionResult<List<AuditFindingDto>>> GetFindings(Guid auditId)
    {
        var findings = await _db.AuditFindings
            .Include(f => f.Clause)
            .Include(f => f.ActionItem)
            .Where(f => f.AuditId == auditId)
            .OrderBy(f => f.FindingType)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync();

        return findings.Select(MapFindingToDto).ToList();
    }

    [HttpPost("{auditId:guid}/findings")]
    public async Task<ActionResult<AuditFindingDto>> AddFinding(
        Guid auditId,
        [FromBody] CreateAuditFindingDto dto)
    {
        var audit = await _db.Audits.FindAsync(auditId);
        if (audit is null) return NotFound("Audit not found.");

        var clause = await _db.StandardsClauses.FindAsync(dto.ClauseId);
        if (clause is null) return NotFound("Clause not found.");

        if (!Enum.TryParse<FindingType>(dto.FindingType, true, out var findingType))
            return BadRequest($"Invalid finding type: {dto.FindingType}");

        var finding = new AuditFinding
        {
            AuditId = auditId,
            ClauseId = dto.ClauseId,
            FindingType = findingType,
            Description = dto.Description,
            ObjectiveEvidence = dto.ObjectiveEvidence,
            Status = FindingStatus.Open
        };

        _db.AuditFindings.Add(finding);
        await _db.SaveChangesAsync();

        finding.Clause = clause;
        return Created($"api/audits/{auditId}/findings/{finding.Id}", MapFindingToDto(finding));
    }

    [HttpPost("{auditId:guid}/findings/{findingId:guid}/raise-ca")]
    public async Task<ActionResult<AuditFindingDto>> RaiseCorrectiveAction(
        Guid auditId, Guid findingId)
    {
        var finding = await _db.AuditFindings
            .Include(f => f.Clause)
            .FirstOrDefaultAsync(f => f.Id == findingId && f.AuditId == auditId);

        if (finding is null) return NotFound();

        if (finding.FindingType != FindingType.MajorNonconformance &&
            finding.FindingType != FindingType.MinorNonconformance)
            return BadRequest("Only Major/Minor nonconformance findings can have corrective actions raised.");

        if (finding.ActionItemId.HasValue)
            return BadRequest("A corrective action has already been raised for this finding.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userName = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? "System";

        var actionItem = new ActionItem
        {
            Title = $"CA: {finding.Clause.ClauseNumber} — {finding.Description[..Math.Min(80, finding.Description.Length)]}",
            Description = $"Corrective action for audit finding against clause {finding.Clause.ClauseNumber} ({finding.Clause.Title}).\n\nFinding: {finding.Description}\n\nObjective Evidence: {finding.ObjectiveEvidence}",
            AssignedToUserId = userId,
            AssignedToDisplayName = userName,
            AssignedByUserId = userId,
            AssignedByDisplayName = userName,
            DueDate = DateTime.UtcNow.AddDays(30),
            Priority = finding.FindingType == FindingType.MajorNonconformance
                ? ActionItemPriority.Critical
                : ActionItemPriority.High,
            SourceType = ActionItemSourceType.AuditFinding,
            SourceEntityId = finding.Id,
            Status = ActionItemStatus.Open
        };

        _db.ActionItems.Add(actionItem);
        finding.ActionItemId = actionItem.Id;
        finding.Status = FindingStatus.CorrectiveActionRaised;

        await _db.SaveChangesAsync();

        finding.ActionItem = actionItem;
        return MapFindingToDto(finding);
    }

    [HttpPost("{auditId:guid}/findings/{findingId:guid}/close")]
    public async Task<ActionResult<AuditFindingDto>> CloseFinding(
        Guid auditId, Guid findingId,
        [FromBody] CloseAuditFindingDto dto)
    {
        var finding = await _db.AuditFindings
            .Include(f => f.Clause)
            .Include(f => f.ActionItem)
            .FirstOrDefaultAsync(f => f.Id == findingId && f.AuditId == auditId);

        if (finding is null) return NotFound();
        if (finding.Status == FindingStatus.Closed)
            return BadRequest("Finding is already closed.");

        if (finding.ActionItemId.HasValue && finding.ActionItem?.Status != ActionItemStatus.Verified)
            return BadRequest("The linked corrective action must be verified before the finding can be closed.");

        finding.Status = FindingStatus.Closed;
        finding.ClosedAt = DateTime.UtcNow;
        finding.ClosureNotes = dto.ClosureNotes;

        await _db.SaveChangesAsync();
        return MapFindingToDto(finding);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Audit?> LoadAudit(Guid id) =>
        await _db.Audits
            .Include(a => a.Program)
            .Include(a => a.Findings).ThenInclude(f => f.Clause)
            .Include(a => a.Findings).ThenInclude(f => f.ActionItem)
            .FirstOrDefaultAsync(a => a.Id == id);

    private static AuditSummaryDto MapToSummary(Audit a) =>
        new(a.Id, a.AuditType.ToString(), a.Scope, a.PlannedDate, a.ActualDate,
            a.LeadAuditor, a.Status.ToString(), a.Findings.Count);

    private static AuditDto MapToDto(Audit a)
    {
        var findings = a.Findings.ToList();
        return new AuditDto(
            a.Id, a.ProgramId, a.Program?.Name ?? "",
            a.AuditType.ToString(), a.Scope, a.PlannedDate, a.ActualDate,
            a.LeadAuditor, a.Status.ToString(),
            findings.Count,
            findings.Count(f => f.FindingType == FindingType.MajorNonconformance),
            findings.Count(f => f.FindingType == FindingType.MinorNonconformance),
            findings.Count(f => f.FindingType == FindingType.Observation),
            findings.Count(f => f.FindingType == FindingType.OpportunityForImprovement),
            a.CreatedAt);
    }

    private static AuditFindingDto MapFindingToDto(AuditFinding f) =>
        new(f.Id, f.AuditId, f.ClauseId,
            f.Clause?.ClauseNumber ?? "", f.Clause?.Title ?? "",
            f.FindingType.ToString(), f.Description, f.ObjectiveEvidence,
            f.Status.ToString(), f.ActionItemId,
            f.ActionItem?.Title, f.ActionItem?.Status.ToString(),
            f.ClosedAt, f.ClosureNotes, f.CreatedAt);
}
