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
[Route("api/root-cause-entries")]
public class RootCauseEntriesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public RootCauseEntriesController(ProcessManagerDbContext db) => _db = db;

    // ───── List / Search ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RootCauseEntryResponseDto>>> GetAll(
        [FromQuery] string? q = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.RootCauseEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(search) ||
                (r.Description != null && r.Description.ToLower().Contains(search)) ||
                (r.Tags != null && r.Tags.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(category) &&
            Enum.TryParse<RootCauseCategory>(category, true, out var cat))
            query = query.Where(r => r.Category == cat);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.UsageCount)
            .ThenBy(r => r.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<RootCauseEntryResponseDto>(
            items.Select(MapToDto).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Typeahead ─────

    /// <summary>Returns up to 10 entries matching the search term — for typeahead in analysis tools.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<RootCauseEntryResponseDto>>> Search(
        [FromQuery] string q = "")
    {
        if (string.IsNullOrWhiteSpace(q))
            return new List<RootCauseEntryResponseDto>();

        var search = q.Trim().ToLower();
        var results = await _db.RootCauseEntries
            .Where(r =>
                r.Title.ToLower().Contains(search) ||
                (r.Tags != null && r.Tags.ToLower().Contains(search)))
            .OrderByDescending(r => r.UsageCount)
            .ThenBy(r => r.Title)
            .Take(10)
            .ToListAsync();

        return results.Select(MapToDto).ToList();
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RootCauseEntryResponseDto>> GetById(Guid id)
    {
        var entry = await _db.RootCauseEntries.FindAsync(id);
        if (entry is null) return NotFound();
        return MapToDto(entry);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<RootCauseEntryResponseDto>> Create(RootCauseEntryCreateDto dto)
    {
        if (!Enum.TryParse<RootCauseCategory>(dto.Category, true, out var category))
            return BadRequest($"Invalid category '{dto.Category}'. Valid values: {string.Join(", ", Enum.GetNames<RootCauseCategory>())}");

        var entry = new RootCauseEntry
        {
            Title                    = dto.Title.Trim(),
            Description              = dto.Description?.Trim(),
            Category                 = category,
            Tags                     = dto.Tags?.Trim(),
            CorrectiveActionTemplate = dto.CorrectiveActionTemplate?.Trim()
        };

        _db.RootCauseEntries.Add(entry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, MapToDto(entry));
    }

    // ───── Update ─────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RootCauseEntryResponseDto>> Update(Guid id, RootCauseEntryUpdateDto dto)
    {
        if (!Enum.TryParse<RootCauseCategory>(dto.Category, true, out var category))
            return BadRequest($"Invalid category '{dto.Category}'. Valid values: {string.Join(", ", Enum.GetNames<RootCauseCategory>())}");

        var entry = await _db.RootCauseEntries.FindAsync(id);
        if (entry is null) return NotFound();

        entry.Title                    = dto.Title.Trim();
        entry.Description              = dto.Description?.Trim();
        entry.Category                 = category;
        entry.Tags                     = dto.Tags?.Trim();
        entry.CorrectiveActionTemplate = dto.CorrectiveActionTemplate?.Trim();

        await _db.SaveChangesAsync();
        return MapToDto(entry);
    }

    // ───── Delete ─────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entry = await _db.RootCauseEntries.FindAsync(id);
        if (entry is null) return NotFound();

        _db.RootCauseEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Unified Analysis Index ─────

    /// <summary>Search/browse the full index of 5 Whys and Ishikawa analyses.</summary>
    [HttpGet("analyses")]
    public async Task<ActionResult<PaginatedResponse<RcaAnalysisIndexItemDto>>> GetAnalysisIndex(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var items = new List<RcaAnalysisIndexItemDto>();

        if (string.IsNullOrEmpty(type) || type.Equals("FiveWhys", StringComparison.OrdinalIgnoreCase))
        {
            var fwQuery = _db.FiveWhysAnalyses.Include(a => a.Nodes).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLower();
                fwQuery = fwQuery.Where(a => a.Title.ToLower().Contains(s) || a.ProblemStatement.ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RcaStatus>(status, true, out var fwStatus))
                fwQuery = fwQuery.Where(a => a.Status == fwStatus);

            var fwResults = await fwQuery.ToListAsync();
            items.AddRange(fwResults.Select(a => new RcaAnalysisIndexItemDto(
                a.Id, "FiveWhys", a.Title, a.ProblemStatement,
                a.LinkedEntityType.ToString(), a.LinkedEntityId,
                a.Status.ToString(),
                a.Nodes.Count,
                a.Nodes.Count(n => n.IsRootCause),
                a.CreatedAt)));
        }

        if (string.IsNullOrEmpty(type) || type.Equals("Ishikawa", StringComparison.OrdinalIgnoreCase))
        {
            var iqQuery = _db.IshikawaDiagrams.Include(d => d.Causes).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLower();
                iqQuery = iqQuery.Where(d => d.Title.ToLower().Contains(s) || d.ProblemStatement.ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RcaStatus>(status, true, out var iqStatus))
                iqQuery = iqQuery.Where(d => d.Status == iqStatus);

            var iqResults = await iqQuery.ToListAsync();
            items.AddRange(iqResults.Select(d => new RcaAnalysisIndexItemDto(
                d.Id, "Ishikawa", d.Title, d.ProblemStatement,
                d.LinkedEntityType.ToString(), d.LinkedEntityId,
                d.Status.ToString(),
                d.Causes.Count,
                d.Causes.Count(c => c.IsSelectedRootCause),
                d.CreatedAt)));
        }

        var totalCount = items.Count;
        var paged = items
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponse<RcaAnalysisIndexItemDto>(paged, totalCount, page, pageSize);
    }

    /// <summary>Returns all analyses (5 Whys + Ishikawa) that have referenced this library entry.</summary>
    [HttpGet("{id:guid}/analyses")]
    public async Task<ActionResult<List<RcaAnalysisIndexItemDto>>> GetCitingAnalyses(Guid id)
    {
        var entry = await _db.RootCauseEntries.FindAsync(id);
        if (entry is null) return NotFound();

        var items = new List<RcaAnalysisIndexItemDto>();

        var fwAnalyses = await _db.FiveWhysAnalyses
            .Include(a => a.Nodes)
            .Where(a => a.Nodes.Any(n => n.RootCauseLibraryEntryId == id))
            .ToListAsync();
        items.AddRange(fwAnalyses.Select(a => new RcaAnalysisIndexItemDto(
            a.Id, "FiveWhys", a.Title, a.ProblemStatement,
            a.LinkedEntityType.ToString(), a.LinkedEntityId,
            a.Status.ToString(),
            a.Nodes.Count,
            a.Nodes.Count(n => n.IsRootCause),
            a.CreatedAt)));

        var iqDiagrams = await _db.IshikawaDiagrams
            .Include(d => d.Causes)
            .Where(d => d.Causes.Any(c => c.RootCauseLibraryEntryId == id))
            .ToListAsync();
        items.AddRange(iqDiagrams.Select(d => new RcaAnalysisIndexItemDto(
            d.Id, "Ishikawa", d.Title, d.ProblemStatement,
            d.LinkedEntityType.ToString(), d.LinkedEntityId,
            d.Status.ToString(),
            d.Causes.Count,
            d.Causes.Count(c => c.IsSelectedRootCause),
            d.CreatedAt)));

        return items.OrderByDescending(i => i.CreatedAt).ToList();
    }

    // ───── Helper ─────

    private static RootCauseEntryResponseDto MapToDto(RootCauseEntry r) => new(
        r.Id,
        r.Title,
        r.Description,
        r.Category.ToString(),
        r.Tags,
        r.CorrectiveActionTemplate,
        r.UsageCount,
        r.CreatedAt,
        r.UpdatedAt
    );
}
