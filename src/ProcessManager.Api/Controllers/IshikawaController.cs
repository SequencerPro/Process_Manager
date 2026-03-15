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
[Route("api/ishikawa")]
public class IshikawaController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public IshikawaController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<IshikawaDiagramSummaryDto>>> GetAll(
        [FromQuery] string? linkedEntityType = null,
        [FromQuery] Guid? linkedEntityId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.IshikawaDiagrams
            .Include(d => d.Causes)
            .AsQueryable();

        if (!string.IsNullOrEmpty(linkedEntityType) &&
            Enum.TryParse<RcaLinkedEntityType>(linkedEntityType, true, out var letEnum))
            query = query.Where(d => d.LinkedEntityType == letEnum);

        if (linkedEntityId.HasValue)
            query = query.Where(d => d.LinkedEntityId == linkedEntityId.Value);

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<RcaStatus>(status, true, out var statusEnum))
            query = query.Where(d => d.Status == statusEnum);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<IshikawaDiagramSummaryDto>(
            items.Select(MapToSummary).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID (full detail) ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> GetById(Guid id)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();
        return MapToDto(diagram);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> Create(IshikawaDiagramCreateDto dto)
    {
        if (!Enum.TryParse<RcaLinkedEntityType>(dto.LinkedEntityType, true, out var let))
            return BadRequest($"Invalid LinkedEntityType '{dto.LinkedEntityType}'. Valid: {string.Join(", ", Enum.GetNames<RcaLinkedEntityType>())}");

        var diagram = new IshikawaDiagram
        {
            Title              = dto.Title.Trim(),
            ProblemStatement   = dto.ProblemStatement.Trim(),
            LinkedEntityType   = let,
            LinkedEntityId     = dto.LinkedEntityId,
            CreatedBy          = User.Identity?.Name,
            Status             = RcaStatus.Open
        };

        _db.IshikawaDiagrams.Add(diagram);
        await _db.SaveChangesAsync();

        var result = await LoadDiagram(diagram.Id);
        return CreatedAtAction(nameof(GetById), new { id = diagram.Id }, MapToDto(result!));
    }

    // ───── Update header ─────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> Update(Guid id, IshikawaDiagramUpdateDto dto)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();
        if (diagram.Status == RcaStatus.Closed)
            return Conflict("Diagram is closed and cannot be modified.");

        diagram.Title            = dto.Title.Trim();
        diagram.ProblemStatement = dto.ProblemStatement.Trim();
        await _db.SaveChangesAsync();
        return MapToDto(diagram);
    }

    // ───── Close ─────

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> Close(Guid id, IshikawaDiagramCloseDto dto)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();
        if (diagram.Status == RcaStatus.Closed)
            return Conflict("Diagram is already closed.");

        diagram.Status       = RcaStatus.Closed;
        diagram.ClosedAt     = DateTime.UtcNow;
        diagram.ClosureNotes = dto.ClosureNotes?.Trim();
        await _db.SaveChangesAsync();
        return MapToDto(diagram);
    }

    // ───── Reopen ─────

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> Reopen(Guid id)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();

        diagram.Status   = RcaStatus.Open;
        diagram.ClosedAt = null;
        await _db.SaveChangesAsync();
        return MapToDto(diagram);
    }

    // ───── Delete ─────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var diagram = await _db.IshikawaDiagrams.FindAsync(id);
        if (diagram is null) return NotFound();
        _db.IshikawaDiagrams.Remove(diagram);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Cause: Add ─────

    [HttpPost("{id:guid}/causes")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> AddCause(Guid id, IshikawaCauseCreateDto dto)
    {
        if (!Enum.TryParse<RootCauseCategory>(dto.Category, true, out var cat))
            return BadRequest($"Invalid category '{dto.Category}'. Valid: {string.Join(", ", Enum.GetNames<RootCauseCategory>())}");

        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();
        if (diagram.Status == RcaStatus.Closed)
            return Conflict("Diagram is closed.");

        if (dto.ParentCauseId.HasValue)
        {
            var parent = diagram.Causes.FirstOrDefault(c => c.Id == dto.ParentCauseId.Value);
            if (parent is null) return BadRequest("Parent cause not found in this diagram.");
            if (parent.ParentCauseId.HasValue) return BadRequest("Sub-causes cannot have further sub-causes (one level of nesting only).");
        }

        var cause = new IshikawaCause
        {
            DiagramId                = id,
            Category                 = cat,
            CauseText                = dto.CauseText.Trim(),
            ParentCauseId            = dto.ParentCauseId,
            RootCauseLibraryEntryId  = dto.RootCauseLibraryEntryId
        };
        _db.IshikawaCauses.Add(cause);
        await _db.SaveChangesAsync();

        var updated = await LoadDiagram(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Cause: Update ─────

    [HttpPut("{id:guid}/causes/{causeId:guid}")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> UpdateCause(Guid id, Guid causeId, IshikawaCauseUpdateDto dto)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();
        if (diagram.Status == RcaStatus.Closed)
            return Conflict("Diagram is closed.");

        var cause = diagram.Causes.FirstOrDefault(c => c.Id == causeId);
        if (cause is null) return NotFound("Cause not found.");

        cause.CauseText           = dto.CauseText.Trim();
        cause.IsSelectedRootCause = dto.IsSelectedRootCause;

        if (dto.RootCauseLibraryEntryId.HasValue)
        {
            // User explicitly linked an existing entry
            cause.RootCauseLibraryEntryId = dto.RootCauseLibraryEntryId;
            if (dto.IsSelectedRootCause)
                await IncrementUsageCount(dto.RootCauseLibraryEntryId.Value);
        }
        else if (dto.IsSelectedRootCause)
        {
            // Auto find-or-create a library entry from the confirmed cause text + category
            var entryId = await FindOrCreateLibraryEntryAsync(dto.CauseText.Trim(), cause.Category);
            cause.RootCauseLibraryEntryId = entryId;
            await IncrementUsageCount(entryId);
        }
        else
        {
            cause.RootCauseLibraryEntryId = null;
        }

        await _db.SaveChangesAsync();

        var updated = await LoadDiagram(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Cause: Delete ─────

    [HttpDelete("{id:guid}/causes/{causeId:guid}")]
    public async Task<ActionResult<IshikawaDiagramResponseDto>> DeleteCause(Guid id, Guid causeId)
    {
        var diagram = await LoadDiagram(id);
        if (diagram is null) return NotFound();

        var cause = diagram.Causes.FirstOrDefault(c => c.Id == causeId);
        if (cause is null) return NotFound("Cause not found.");

        // Remove sub-causes first (EF will do this via DeleteBehavior.Cascade but let's be explicit for nested case)
        var subCauses = diagram.Causes.Where(c => c.ParentCauseId == causeId).ToList();
        _db.IshikawaCauses.RemoveRange(subCauses);
        _db.IshikawaCauses.Remove(cause);
        await _db.SaveChangesAsync();

        var updated = await LoadDiagram(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Helpers ─────

    private async Task<IshikawaDiagram?> LoadDiagram(Guid id) =>
        await _db.IshikawaDiagrams
            .Include(d => d.Causes).ThenInclude(c => c.RootCauseLibraryEntry)
            .Include(d => d.Causes).ThenInclude(c => c.SubCauses).ThenInclude(sc => sc.RootCauseLibraryEntry)
            .FirstOrDefaultAsync(d => d.Id == id);

    private async Task IncrementUsageCount(Guid entryId)
    {
        var entry = await _db.RootCauseEntries.FindAsync(entryId);
        if (entry is not null) { entry.UsageCount++; await _db.SaveChangesAsync(); }
    }

    private async Task<Guid> FindOrCreateLibraryEntryAsync(string title, RootCauseCategory category)
    {
        var titleLower = title.ToLower();
        var entry = await _db.RootCauseEntries
            .FirstOrDefaultAsync(r => r.Title.ToLower() == titleLower && r.Category == category);
        if (entry is not null) return entry.Id;

        entry = new RootCauseEntry { Title = title, Category = category };
        _db.RootCauseEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    private static IshikawaDiagramSummaryDto MapToSummary(IshikawaDiagram d) => new(
        d.Id, d.Title, d.ProblemStatement,
        d.LinkedEntityType.ToString(), d.LinkedEntityId,
        d.Status.ToString(),
        d.Causes.Count,
        d.Causes.Count(c => c.IsSelectedRootCause),
        d.CreatedAt, d.UpdatedAt);

    private static IshikawaDiagramResponseDto MapToDto(IshikawaDiagram d)
    {
        var topLevel = d.Causes.Where(c => c.ParentCauseId == null).ToList();
        return new(
            d.Id, d.Title, d.ProblemStatement,
            d.LinkedEntityType.ToString(), d.LinkedEntityId,
            d.CreatedBy, d.Status.ToString(), d.ClosedAt, d.ClosureNotes,
            topLevel.Select(c => MapCauseDto(c, d.Causes.ToList())).ToList(),
            d.CreatedAt, d.UpdatedAt);
    }

    private static IshikawaCauseSummaryDto MapCauseDto(IshikawaCause c, List<IshikawaCause> all) => new(
        c.Id, c.Category.ToString(), c.CauseText,
        c.ParentCauseId, c.RootCauseLibraryEntryId,
        c.RootCauseLibraryEntry?.Title,
        c.IsSelectedRootCause,
        all.Where(s => s.ParentCauseId == c.Id)
           .Select(s => MapCauseDto(s, all)).ToList());
}
