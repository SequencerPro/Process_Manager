using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using System.Security.Claims;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/action-items")]
public class ActionItemsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ActionItemsController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ActionItemSummaryDto>>> GetAll(
        [FromQuery] string?  status         = null,
        [FromQuery] string?  priority       = null,
        [FromQuery] string?  sourceType     = null,
        [FromQuery] string?  assignedToMe   = null,   // "true" to filter by current user
        [FromQuery] bool?    overdue        = null,
        [FromQuery] Guid?    sourceEntityId = null,
        [FromQuery] int      page           = 1,
        [FromQuery] int      pageSize       = 25)
    {
        var query = _db.ActionItems.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ActionItemStatus>(status, true, out var s))
            query = query.Where(a => a.Status == s);

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<ActionItemPriority>(priority, true, out var p))
            query = query.Where(a => a.Priority == p);

        if (!string.IsNullOrEmpty(sourceType) && Enum.TryParse<ActionItemSourceType>(sourceType, true, out var st))
            query = query.Where(a => a.SourceType == st);

        if (sourceEntityId.HasValue)
            query = query.Where(a => a.SourceEntityId == sourceEntityId.Value);

        if (assignedToMe == "true")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.AssignedToUserId == userId);
        }

        if (overdue == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(a =>
                (a.Status == ActionItemStatus.Open || a.Status == ActionItemStatus.InProgress)
                && a.DueDate < now);
        }

        var totalCount = await query.CountAsync();
        var now2 = DateTime.UtcNow;

        var items = await query
            .OrderBy(a => a.DueDate)
            .ThenByDescending(a => a.Priority)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<ActionItemSummaryDto>(
            items.Select(a => MapToSummary(a, now2)).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ActionItemDto>> GetById(Guid id)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();
        return MapToDto(item);
    }

    // ───── Create ─────

    [HttpPost]
    public async Task<ActionResult<ActionItemDto>> Create([FromBody] CreateActionItemDto dto)
    {
        if (!Enum.TryParse<ActionItemPriority>(dto.Priority, true, out var priority))
            return BadRequest($"Invalid priority: {dto.Priority}");

        if (!Enum.TryParse<ActionItemSourceType>(dto.SourceType, true, out var sourceType))
            return BadRequest($"Invalid source type: {dto.SourceType}");

        var assignerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var assignerName   = User.FindFirstValue("display_name")
                             ?? User.Identity?.Name ?? assignerUserId;

        var item = new ActionItem
        {
            Title                  = dto.Title,
            Description            = dto.Description,
            AssignedToUserId       = dto.AssignedToUserId,
            AssignedToDisplayName  = dto.AssignedToDisplayName,
            AssignedByUserId       = assignerUserId,
            AssignedByDisplayName  = assignerName,
            DueDate                = dto.DueDate,
            Priority               = priority,
            SourceType             = sourceType,
            SourceEntityId         = dto.SourceEntityId,
            Status                 = ActionItemStatus.Open,
        };

        _db.ActionItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, MapToDto(item));
    }

    // ───── Update ─────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ActionItemDto>> Update(Guid id, [FromBody] UpdateActionItemDto dto)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();

        if (item.Status is ActionItemStatus.Complete or ActionItemStatus.Verified or ActionItemStatus.Cancelled)
            return BadRequest("Cannot update a completed, verified, or cancelled action item.");

        if (!Enum.TryParse<ActionItemPriority>(dto.Priority, true, out var priority))
            return BadRequest($"Invalid priority: {dto.Priority}");

        item.Title                 = dto.Title;
        item.Description           = dto.Description;
        item.AssignedToUserId      = dto.AssignedToUserId;
        item.AssignedToDisplayName = dto.AssignedToDisplayName;
        item.DueDate               = dto.DueDate;
        item.Priority              = priority;

        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    // ───── Start ─────

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ActionItemDto>> Start(Guid id)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();
        if (item.Status != ActionItemStatus.Open)
            return BadRequest("Only Open action items can be started.");

        item.Status = ActionItemStatus.InProgress;
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    // ───── Complete ─────

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ActionItemDto>> Complete(Guid id, [FromBody] CompleteActionItemDto dto)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();

        if (item.Status is not (ActionItemStatus.Open or ActionItemStatus.InProgress))
            return BadRequest("Can only complete Open or InProgress action items.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var name   = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? userId;

        item.Status          = ActionItemStatus.Complete;
        item.CompletedBy     = name;
        item.CompletedAt     = DateTime.UtcNow;
        item.CompletionNotes = dto.CompletionNotes;

        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    // ───── Verify ─────

    [HttpPost("{id:guid}/verify")]
    public async Task<ActionResult<ActionItemDto>> Verify(Guid id, [FromBody] VerifyActionItemDto dto)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();

        if (item.Status != ActionItemStatus.Complete)
            return BadRequest("Only Complete action items can be verified.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var name   = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? userId;

        if (userId == item.AssignedToUserId)
            return BadRequest("The verifier cannot be the same as the assignee.");

        item.Status     = ActionItemStatus.Verified;
        item.VerifiedBy = name;
        item.VerifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    // ───── Cancel ─────

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ActionItemDto>> Cancel(Guid id)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();

        if (item.Status is ActionItemStatus.Verified or ActionItemStatus.Cancelled)
            return BadRequest("Cannot cancel a verified or already-cancelled action item.");

        item.Status = ActionItemStatus.Cancelled;
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    // ───── Delete ─────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.ActionItems.FindAsync(id);
        if (item is null) return NotFound();

        _db.ActionItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Scorecard ─────

    [HttpGet("scorecard")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<QualityScorecardDto>> GetScorecard()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var allItems = await _db.ActionItems.AsNoTracking().ToListAsync();

        var openItems      = allItems.Where(a => a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress).ToList();
        var overdueItems   = openItems.Where(a => a.DueDate < now).ToList();
        var awaitingVerify = allItems.Where(a => a.Status == ActionItemStatus.Complete).ToList();

        var recentItems    = allItems.Where(a => a.CreatedAt >= thirtyDaysAgo).ToList();
        var recentClosed   = recentItems.Where(a => a.Status is ActionItemStatus.Complete or ActionItemStatus.Verified).ToList();
        var closeRate      = recentItems.Count > 0 ? (double)recentClosed.Count / recentItems.Count * 100 : 0;

        var closedWithDuration = allItems
            .Where(a => a.CompletedAt.HasValue)
            .Select(a => (a.CompletedAt!.Value - a.CreatedAt).TotalDays)
            .ToList();
        var avgDaysToClose = closedWithDuration.Count > 0 ? closedWithDuration.Average() : 0;

        var openNcs = await _db.NonConformances
            .AsNoTracking()
            .CountAsync(nc => nc.DispositionStatus == DispositionStatus.Pending);

        var openMrbs = await _db.MrbReviews
            .AsNoTracking()
            .CountAsync(m => m.Status != MrbStatus.Closed);

        var mrbsOver30 = await _db.MrbReviews
            .AsNoTracking()
            .CountAsync(m => m.Status != MrbStatus.Closed && m.CreatedAt < now.AddDays(-30));

        var byPriority = Enum.GetValues<ActionItemPriority>().Select(p => new ActionItemAgeGroupDto(
            p.ToString(),
            openItems.Count(a => a.Priority == p),
            openItems.Count(a => a.Priority == p && a.DueDate < now)
        )).ToList();

        var bySource = allItems
            .GroupBy(a => a.SourceType)
            .Select(g => new ActionItemSourceBreakdownDto(
                g.Key.ToString(),
                g.Count(a => a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress),
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ToList();

        var topOverdue = overdueItems
            .OrderBy(a => a.DueDate)
            .Take(10)
            .Select(a => MapToSummary(a, now))
            .ToList();

        return new QualityScorecardDto(
            openItems.Count,
            overdueItems.Count,
            awaitingVerify.Count,
            Math.Round(closeRate, 1),
            Math.Round(avgDaysToClose, 1),
            openNcs,
            openMrbs,
            mrbsOver30,
            byPriority,
            bySource,
            topOverdue);
    }

    // ───── Mappers ─────

    private static ActionItemDto MapToDto(ActionItem a) => new(
        a.Id,
        a.Title,
        a.Description,
        a.AssignedToUserId,
        a.AssignedToDisplayName,
        a.AssignedByUserId,
        a.AssignedByDisplayName,
        a.DueDate,
        a.Priority.ToString(),
        a.Status.ToString(),
        a.SourceType.ToString(),
        a.SourceEntityId,
        a.CompletedBy,
        a.CompletedAt,
        a.CompletionNotes,
        a.VerifiedBy,
        a.VerifiedAt,
        a.CreatedAt,
        a.CreatedBy,
        a.UpdatedAt,
        IsOverdue: (a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress) && a.DueDate < DateTime.UtcNow
    );

    private static ActionItemSummaryDto MapToSummary(ActionItem a, DateTime now) => new(
        a.Id,
        a.Title,
        a.AssignedToDisplayName,
        a.AssignedByDisplayName,
        a.DueDate,
        a.Priority.ToString(),
        a.Status.ToString(),
        a.SourceType.ToString(),
        a.SourceEntityId,
        a.CreatedAt,
        IsOverdue: (a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress) && a.DueDate < now
    );
}
