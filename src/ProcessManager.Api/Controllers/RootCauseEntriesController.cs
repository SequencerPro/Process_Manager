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
