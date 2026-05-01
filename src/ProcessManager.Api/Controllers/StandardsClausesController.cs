using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/standards-clauses")]
public class StandardsClausesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public StandardsClausesController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<StandardsClauseSummaryDto>>> GetAll(
        [FromQuery] string? standard = null)
    {
        var query = _db.StandardsClauses
            .Include(c => c.EvidenceLinks)
            .Include(c => c.Findings)
            .AsQueryable();

        if (!string.IsNullOrEmpty(standard) && Enum.TryParse<ConformanceStandard>(standard, true, out var s))
            query = query.Where(c => c.Standard == s);

        var clauses = await query
            .OrderBy(c => c.Standard)
            .ThenBy(c => c.ClauseNumber)
            .ToListAsync();

        return clauses.Select(c => MapToSummary(c)).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StandardsClauseDto>> GetById(Guid id)
    {
        var clause = await _db.StandardsClauses
            .Include(c => c.EvidenceLinks)
            .Include(c => c.Findings)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clause is null) return NotFound();

        return MapToDto(clause);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ConformanceDashboardDto>> GetDashboard(
        [FromQuery] string? standard = null)
    {
        var query = _db.StandardsClauses
            .Include(c => c.EvidenceLinks)
            .Include(c => c.Findings)
            .AsQueryable();

        if (!string.IsNullOrEmpty(standard) && Enum.TryParse<ConformanceStandard>(standard, true, out var s))
            query = query.Where(c => c.Standard == s);

        var clauses = await query
            .OrderBy(c => c.Standard)
            .ThenBy(c => c.ClauseNumber)
            .ToListAsync();

        var summaries = clauses.Select(MapToSummary).ToList();

        var covered = summaries.Count(s => s.CoverageStatus == nameof(ClauseCoverageStatus.Covered));
        var partial = summaries.Count(s => s.CoverageStatus == nameof(ClauseCoverageStatus.PartialCoverage));
        var gap = summaries.Count(s => s.CoverageStatus == nameof(ClauseCoverageStatus.Gap));
        var majorCount = summaries.Count(s => s.CoverageStatus == nameof(ClauseCoverageStatus.OpenMajorFinding));

        var openMinorCount = await _db.AuditFindings
            .Where(f => f.FindingType == FindingType.MinorNonconformance && f.Status != FindingStatus.Closed)
            .CountAsync();

        var nextAudit = await _db.Audits
            .Where(a => a.Status == AuditStatus.Planned && a.PlannedDate >= DateTime.UtcNow)
            .OrderBy(a => a.PlannedDate)
            .Select(a => (DateTime?)a.PlannedDate)
            .FirstOrDefaultAsync();

        return new ConformanceDashboardDto(
            summaries.Count,
            covered,
            partial,
            gap,
            majorCount,
            openMinorCount,
            nextAudit,
            summaries);
    }

    // ── Evidence Links ──────────────────────────────────────────────────

    [HttpGet("{clauseId:guid}/evidence")]
    public async Task<ActionResult<List<ClauseEvidenceLinkDto>>> GetEvidenceLinks(Guid clauseId)
    {
        var links = await _db.ClauseEvidenceLinks
            .Include(l => l.Clause)
            .Where(l => l.ClauseId == clauseId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var result = new List<ClauseEvidenceLinkDto>();
        foreach (var l in links)
        {
            var entityName = await ResolveEntityName(l.EntityType, l.EntityId);
            result.Add(new ClauseEvidenceLinkDto(
                l.Id, l.ClauseId, l.Clause.ClauseNumber, l.Clause.Title,
                l.EntityType.ToString(), l.EntityId, entityName,
                l.EvidenceNote, l.IsAutoLinked, l.CreatedAt));
        }
        return result;
    }

    [HttpPost("{clauseId:guid}/evidence")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ClauseEvidenceLinkDto>> AddEvidenceLink(
        Guid clauseId,
        [FromBody] CreateClauseEvidenceLinkDto dto)
    {
        var clause = await _db.StandardsClauses.FindAsync(clauseId);
        if (clause is null) return NotFound("Clause not found.");

        if (!Enum.TryParse<ClauseEvidenceEntityType>(dto.EntityType, true, out var entityType))
            return BadRequest($"Invalid entity type: {dto.EntityType}");

        var exists = await _db.ClauseEvidenceLinks
            .AnyAsync(l => l.ClauseId == clauseId && l.EntityType == entityType && l.EntityId == dto.EntityId);
        if (exists) return Conflict("Evidence link already exists for this clause and entity.");

        var link = new Domain.Entities.ClauseEvidenceLink
        {
            ClauseId = clauseId,
            EntityType = entityType,
            EntityId = dto.EntityId,
            EvidenceNote = dto.EvidenceNote,
            IsAutoLinked = false
        };

        _db.ClauseEvidenceLinks.Add(link);
        await _db.SaveChangesAsync();

        var entityName = await ResolveEntityName(entityType, dto.EntityId);
        return Created($"api/standards-clauses/{clauseId}/evidence/{link.Id}",
            new ClauseEvidenceLinkDto(
                link.Id, clauseId, clause.ClauseNumber, clause.Title,
                entityType.ToString(), dto.EntityId, entityName,
                dto.EvidenceNote, false, link.CreatedAt));
    }

    [HttpDelete("{clauseId:guid}/evidence/{linkId:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> DeleteEvidenceLink(Guid clauseId, Guid linkId)
    {
        var link = await _db.ClauseEvidenceLinks
            .FirstOrDefaultAsync(l => l.Id == linkId && l.ClauseId == clauseId);
        if (link is null) return NotFound();

        _db.ClauseEvidenceLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ClauseCoverageStatus ComputeCoverage(
        Domain.Entities.StandardsClause clause)
    {
        var hasOpenMajor = clause.Findings.Any(f =>
            f.FindingType == FindingType.MajorNonconformance && f.Status != FindingStatus.Closed);
        if (hasOpenMajor) return ClauseCoverageStatus.OpenMajorFinding;

        var hasEvidence = clause.EvidenceLinks.Any();
        if (!hasEvidence) return ClauseCoverageStatus.Gap;

        var hasOpenMinorOrObs = clause.Findings.Any(f =>
            (f.FindingType == FindingType.MinorNonconformance || f.FindingType == FindingType.Observation)
            && f.Status != FindingStatus.Closed);
        if (hasOpenMinorOrObs) return ClauseCoverageStatus.PartialCoverage;

        return ClauseCoverageStatus.Covered;
    }

    private static StandardsClauseSummaryDto MapToSummary(Domain.Entities.StandardsClause c)
    {
        var coverage = ComputeCoverage(c);
        return new StandardsClauseSummaryDto(
            c.Id, c.Standard.ToString(), c.ClauseNumber, c.Title,
            c.IsAs9100Addition, c.EvidenceLinks.Count, coverage.ToString());
    }

    private static StandardsClauseDto MapToDto(Domain.Entities.StandardsClause c)
    {
        var coverage = ComputeCoverage(c);
        var openFindings = c.Findings.Count(f => f.Status != FindingStatus.Closed);
        return new StandardsClauseDto(
            c.Id, c.Standard.ToString(), c.ClauseNumber, c.Title,
            c.RequirementSummary, c.IsAs9100Addition,
            c.EvidenceLinks.Count, openFindings, coverage.ToString());
    }

    private async Task<string?> ResolveEntityName(ClauseEvidenceEntityType entityType, Guid entityId)
    {
        return entityType switch
        {
            ClauseEvidenceEntityType.Process or ClauseEvidenceEntityType.QmsDocument =>
                await _db.Processes.Where(p => p.Id == entityId).Select(p => p.Name).FirstOrDefaultAsync(),
            ClauseEvidenceEntityType.ControlPlan =>
                await _db.ControlPlans.Where(c => c.Id == entityId).Select(c => c.Name).FirstOrDefaultAsync(),
            ClauseEvidenceEntityType.Pfmea =>
                await _db.Pfmeas.Where(p => p.Id == entityId).Select(p => p.Name).FirstOrDefaultAsync(),
            ClauseEvidenceEntityType.ManagementReview =>
                await _db.ManagementReviews.Where(r => r.Id == entityId).Select(r => r.Title).FirstOrDefaultAsync(),
            ClauseEvidenceEntityType.NonConformance =>
                await _db.NonConformances.Where(n => n.Id == entityId).Select(n => n.ActualValue ?? "NC").FirstOrDefaultAsync(),
            _ => null
        };
    }
}
