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
[Route("api/five-whys")]
public class FiveWhysController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public FiveWhysController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FiveWhysAnalysisSummaryDto>>> GetAll(
        [FromQuery] string? linkedEntityType = null,
        [FromQuery] Guid? linkedEntityId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.FiveWhysAnalyses
            .Include(a => a.Nodes)
            .AsQueryable();

        if (!string.IsNullOrEmpty(linkedEntityType) &&
            Enum.TryParse<RcaLinkedEntityType>(linkedEntityType, true, out var letEnum))
            query = query.Where(a => a.LinkedEntityType == letEnum);

        if (linkedEntityId.HasValue)
            query = query.Where(a => a.LinkedEntityId == linkedEntityId.Value);

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<RcaStatus>(status, true, out var statusEnum))
            query = query.Where(a => a.Status == statusEnum);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<FiveWhysAnalysisSummaryDto>(
            items.Select(MapToSummary).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID (full detail) ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> GetById(Guid id)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();
        return MapToDto(analysis);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> Create(FiveWhysAnalysisCreateDto dto)
    {
        if (!Enum.TryParse<RcaLinkedEntityType>(dto.LinkedEntityType, true, out var let))
            return BadRequest($"Invalid LinkedEntityType '{dto.LinkedEntityType}'. Valid: {string.Join(", ", Enum.GetNames<RcaLinkedEntityType>())}");

        var analysis = new FiveWhysAnalysis
        {
            Title            = dto.Title.Trim(),
            ProblemStatement = dto.ProblemStatement.Trim(),
            LinkedEntityType = let,
            LinkedEntityId   = dto.LinkedEntityId,
            CreatedBy        = User.Identity?.Name,
            Status           = RcaStatus.Open
        };

        _db.FiveWhysAnalyses.Add(analysis);
        await _db.SaveChangesAsync();

        var result = await LoadAnalysis(analysis.Id);
        return CreatedAtAction(nameof(GetById), new { id = analysis.Id }, MapToDto(result!));
    }

    // ───── Update ─────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> Update(Guid id, FiveWhysAnalysisUpdateDto dto)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();
        if (analysis.Status == RcaStatus.Closed)
            return Conflict("Analysis is closed and cannot be modified.");

        analysis.Title            = dto.Title.Trim();
        analysis.ProblemStatement = dto.ProblemStatement.Trim();
        await _db.SaveChangesAsync();
        return MapToDto(analysis);
    }

    // ───── Close ─────

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> Close(Guid id, FiveWhysAnalysisCloseDto dto)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();
        if (analysis.Status == RcaStatus.Closed)
            return Conflict("Analysis is already closed.");

        analysis.Status       = RcaStatus.Closed;
        analysis.ClosedAt     = DateTime.UtcNow;
        analysis.ClosureNotes = dto.ClosureNotes?.Trim();
        await _db.SaveChangesAsync();
        return MapToDto(analysis);
    }

    // ───── Reopen ─────

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> Reopen(Guid id)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();

        analysis.Status   = RcaStatus.Open;
        analysis.ClosedAt = null;
        await _db.SaveChangesAsync();
        return MapToDto(analysis);
    }

    // ───── Delete ─────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var analysis = await _db.FiveWhysAnalyses.FindAsync(id);
        if (analysis is null) return NotFound();
        _db.FiveWhysAnalyses.Remove(analysis);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Node: Add ─────

    [HttpPost("{id:guid}/nodes")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> AddNode(Guid id, FiveWhysNodeCreateDto dto)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();
        if (analysis.Status == RcaStatus.Closed)
            return Conflict("Analysis is closed.");

        if (dto.ParentNodeId.HasValue &&
            !analysis.Nodes.Any(n => n.Id == dto.ParentNodeId.Value))
            return BadRequest("Parent node not found in this analysis.");

        var node = new FiveWhysNode
        {
            AnalysisId               = id,
            ParentNodeId             = dto.ParentNodeId,
            WhyStatement             = dto.WhyStatement.Trim(),
            RootCauseLibraryEntryId  = dto.RootCauseLibraryEntryId
        };
        _db.FiveWhysNodes.Add(node);
        await _db.SaveChangesAsync();

        var updated = await LoadAnalysis(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Node: Update ─────

    [HttpPut("{id:guid}/nodes/{nodeId:guid}")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> UpdateNode(Guid id, Guid nodeId, FiveWhysNodeUpdateDto dto)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();
        if (analysis.Status == RcaStatus.Closed)
            return Conflict("Analysis is closed.");

        var node = analysis.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node is null) return NotFound("Node not found.");

        var wasAlreadyRoot = node.IsRootCause;
        node.WhyStatement    = dto.WhyStatement.Trim();
        node.IsRootCause     = dto.IsRootCause;
        node.CorrectiveAction = dto.CorrectiveAction?.Trim();

        if (dto.RootCauseLibraryEntryId.HasValue)
        {
            // User explicitly linked an entry
            node.RootCauseLibraryEntryId = dto.RootCauseLibraryEntryId;
            if (dto.IsRootCause && !wasAlreadyRoot)
                await IncrementUsageCount(dto.RootCauseLibraryEntryId.Value);
        }
        else if (dto.IsRootCause)
        {
            // Try to auto-link to an existing entry by title (no category on 5 Whys nodes, so no auto-create)
            var titleLower = dto.WhyStatement.Trim().ToLower();
            var existing = await _db.RootCauseEntries
                .FirstOrDefaultAsync(r => r.Title.ToLower() == titleLower);
            if (existing is not null)
            {
                node.RootCauseLibraryEntryId = existing.Id;
                if (!wasAlreadyRoot)
                    await IncrementUsageCount(existing.Id);
            }
        }
        else
        {
            node.RootCauseLibraryEntryId = null;
        }

        await _db.SaveChangesAsync();

        var updated = await LoadAnalysis(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Node: Delete ─────

    [HttpDelete("{id:guid}/nodes/{nodeId:guid}")]
    public async Task<ActionResult<FiveWhysAnalysisResponseDto>> DeleteNode(Guid id, Guid nodeId)
    {
        var analysis = await LoadAnalysis(id);
        if (analysis is null) return NotFound();

        var node = analysis.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node is null) return NotFound("Node not found.");

        // Recursively remove all descendant nodes
        RemoveNodeSubtree(nodeId, analysis.Nodes.ToList());
        await _db.SaveChangesAsync();

        var updated = await LoadAnalysis(id);
        return Ok(MapToDto(updated!));
    }

    // ───── Helpers ─────

    private void RemoveNodeSubtree(Guid nodeId, List<FiveWhysNode> allNodes)
    {
        var children = allNodes.Where(n => n.ParentNodeId == nodeId).ToList();
        foreach (var child in children)
            RemoveNodeSubtree(child.Id, allNodes);

        var node = allNodes.First(n => n.Id == nodeId);
        _db.FiveWhysNodes.Remove(node);
    }

    private async Task<FiveWhysAnalysis?> LoadAnalysis(Guid id) =>
        await _db.FiveWhysAnalyses
            .Include(a => a.Nodes).ThenInclude(n => n.RootCauseLibraryEntry)
            .FirstOrDefaultAsync(a => a.Id == id);

    private async Task IncrementUsageCount(Guid entryId)
    {
        var entry = await _db.RootCauseEntries.FindAsync(entryId);
        if (entry is not null) { entry.UsageCount++; await _db.SaveChangesAsync(); }
    }

    private static FiveWhysAnalysisSummaryDto MapToSummary(FiveWhysAnalysis a)
    {
        var nodes = a.Nodes.ToList();
        // Incomplete leaves: non-root-cause nodes that have no children
        var leafNodeIds = nodes.Select(n => n.Id)
            .Except(nodes.Where(n => n.ParentNodeId.HasValue).Select(n => n.ParentNodeId!.Value))
            .ToHashSet();
        var hasIncomplete = nodes.Any(n => leafNodeIds.Contains(n.Id) && !n.IsRootCause);

        return new(a.Id, a.Title, a.ProblemStatement,
            a.LinkedEntityType.ToString(), a.LinkedEntityId,
            a.Status.ToString(), nodes.Count,
            nodes.Count(n => n.IsRootCause),
            hasIncomplete,
            a.CreatedAt, a.UpdatedAt);
    }

    private static FiveWhysAnalysisResponseDto MapToDto(FiveWhysAnalysis a)
    {
        var nodes = a.Nodes.ToList();
        var roots = nodes.Where(n => n.ParentNodeId == null).ToList();
        return new(
            a.Id, a.Title, a.ProblemStatement,
            a.LinkedEntityType.ToString(), a.LinkedEntityId,
            a.CreatedBy, a.Status.ToString(), a.ClosedAt, a.ClosureNotes,
            roots.Select(n => BuildNodeTree(n, nodes)).ToList(),
            a.CreatedAt, a.UpdatedAt);
    }

    private static FiveWhysNodeDto BuildNodeTree(FiveWhysNode node, List<FiveWhysNode> all) => new(
        node.Id, node.ParentNodeId, node.WhyStatement,
        node.IsRootCause, node.RootCauseLibraryEntryId,
        node.RootCauseLibraryEntry?.Title,
        node.CorrectiveAction,
        all.Where(n => n.ParentNodeId == node.Id)
           .Select(n => BuildNodeTree(n, all)).ToList());
}
