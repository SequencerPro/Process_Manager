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
[Route("api/management-reviews")]
public class ManagementReviewsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ManagementReviewsController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ManagementReviewSummaryDto>>> GetAll(
        [FromQuery] string? status     = null,
        [FromQuery] string? reviewType = null,
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 25)
    {
        var query = _db.ManagementReviews.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ManagementReviewStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        if (!string.IsNullOrEmpty(reviewType) && Enum.TryParse<ManagementReviewType>(reviewType, true, out var rt))
            query = query.Where(r => r.ReviewType == rt);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.ScheduledDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // For each review, count linked action items
        var reviewIds = items.Select(r => r.Id).ToList();
        var counts = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.ManagementReview
                     && a.SourceEntityId.HasValue
                     && reviewIds.Contains(a.SourceEntityId.Value))
            .GroupBy(a => a.SourceEntityId!.Value)
            .Select(g => new { ReviewId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ReviewId, x => x.Count);

        return new PaginatedResponse<ManagementReviewSummaryDto>(
            items.Select(r => MapToSummary(r, counts.GetValueOrDefault(r.Id, 0))).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ManagementReviewDto>> GetById(Guid id)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        var actionCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.ManagementReview
                          && a.SourceEntityId == id);

        return MapToDto(review, actionCount);
    }

    // ───── Create ─────

    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ManagementReviewDto>> Create([FromBody] CreateManagementReviewDto dto)
    {
        if (!Enum.TryParse<ManagementReviewType>(dto.ReviewType, true, out var reviewType))
            return BadRequest($"Invalid review type: {dto.ReviewType}");

        var review = new ManagementReview
        {
            Title         = dto.Title,
            ReviewType    = reviewType,
            ScheduledDate = dto.ScheduledDate,
            Status        = ManagementReviewStatus.Scheduled,
        };

        _db.ManagementReviews.Add(review);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, MapToDto(review, 0));
    }

    // ───── Update ─────

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ManagementReviewDto>> Update(Guid id, [FromBody] UpdateManagementReviewDto dto)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        if (review.Status == ManagementReviewStatus.Complete)
            return BadRequest("Cannot update a completed management review.");

        if (!Enum.TryParse<ManagementReviewType>(dto.ReviewType, true, out var reviewType))
            return BadRequest($"Invalid review type: {dto.ReviewType}");

        review.Title                    = dto.Title;
        review.ReviewType               = reviewType;
        review.ScheduledDate            = dto.ScheduledDate;
        review.ConductedBy              = dto.ConductedBy;
        review.CustomerComplaintsNotes  = dto.CustomerComplaintsNotes;
        review.SupplierQualityNotes     = dto.SupplierQualityNotes;
        review.InternalAuditStatus      = dto.InternalAuditStatus;
        review.PriorActionsSummary      = dto.PriorActionsSummary;
        review.Decisions                = dto.Decisions;
        review.NextCycleTargets         = dto.NextCycleTargets;

        await _db.SaveChangesAsync();

        var actionCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.ManagementReview
                          && a.SourceEntityId == id);

        return MapToDto(review, actionCount);
    }

    // ───── Start ─────

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ManagementReviewDto>> Start(Guid id)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        if (review.Status != ManagementReviewStatus.Scheduled)
            return BadRequest("Only Scheduled reviews can be started.");

        // Auto-populate inputs from live data
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var openNcs = await _db.NonConformances
            .AsNoTracking()
            .CountAsync(nc => nc.DispositionStatus == DispositionStatus.Pending);

        var recentNcs = await _db.NonConformances
            .AsNoTracking()
            .CountAsync(nc => nc.CreatedAt >= thirtyDaysAgo);

        review.NcSummary = $"Open pending NCs: {openNcs}. New NCs in last 30 days: {recentNcs}.";

        var allActionItems = await _db.ActionItems.AsNoTracking().ToListAsync();
        var recentItems    = allActionItems.Where(a => a.CreatedAt >= thirtyDaysAgo).ToList();
        var recentClosed   = recentItems.Where(a => a.Status is ActionItemStatus.Complete or ActionItemStatus.Verified).ToList();
        var closeRate      = recentItems.Count > 0
            ? Math.Round((double)recentClosed.Count / recentItems.Count * 100, 1)
            : 0;
        var totalOpen      = allActionItems.Count(a => a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress);
        var totalOverdue   = allActionItems.Count(a =>
            (a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress) && a.DueDate < now);

        review.ActionCloseRateSummary =
            $"30-day close rate: {closeRate}%. Open: {totalOpen}. Overdue: {totalOverdue}.";

        var openMrbs   = await _db.MrbReviews.AsNoTracking().CountAsync(m => m.Status != MrbStatus.Closed);
        var mrbsOver30 = await _db.MrbReviews.AsNoTracking()
            .CountAsync(m => m.Status != MrbStatus.Closed && m.CreatedAt < now.AddDays(-30));

        review.MrbSummary = $"Open MRBs: {openMrbs}. MRBs open > 30 days: {mrbsOver30}.";

        // Training compliance snapshot
        var allCompetency = await _db.CompetencyRecords.AsNoTracking().ToListAsync();
        var trainingTotal   = allCompetency.Count(c => c.Status != CompetencyStatus.Superseded);
        var trainingCurrent = allCompetency.Count(c => c.Status == CompetencyStatus.Current);
        var trainingExpired = allCompetency.Count(c => c.Status == CompetencyStatus.Expired);
        var trainingExpiringSoon = allCompetency.Count(c =>
            c.Status == CompetencyStatus.Current
            && c.ExpiresAt.HasValue
            && c.ExpiresAt.Value <= now.AddDays(30));
        var trainingPct = trainingTotal > 0
            ? Math.Round((double)trainingCurrent / trainingTotal * 100, 1)
            : 0;
        review.TrainingComplianceSummary =
            $"Competency records: {trainingCurrent}/{trainingTotal} current ({trainingPct}%). Expired: {trainingExpired}. Expiring within 30 days: {trainingExpiringSoon}.";

        review.Status = ManagementReviewStatus.InProgress;

        await _db.SaveChangesAsync();

        var actionCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.ManagementReview
                          && a.SourceEntityId == id);

        return MapToDto(review, actionCount);
    }

    // ───── Complete ─────

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ManagementReviewDto>> Complete(Guid id)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        if (review.Status != ManagementReviewStatus.InProgress)
            return BadRequest("Only InProgress reviews can be completed.");

        review.Status = ManagementReviewStatus.Complete;
        await _db.SaveChangesAsync();

        var actionCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.ManagementReview
                          && a.SourceEntityId == id);

        return MapToDto(review, actionCount);
    }

    // ───── Action Items for a review ─────

    [HttpGet("{id:guid}/action-items")]
    public async Task<ActionResult<List<ActionItemSummaryDto>>> GetActionItems(Guid id)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        var now   = DateTime.UtcNow;
        var items = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.ManagementReview
                     && a.SourceEntityId == id)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        return items.Select(a => MapActionItemToSummary(a, now)).ToList();
    }

    [HttpPost("{id:guid}/action-items")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ActionItemDto>> AddActionItem(Guid id, [FromBody] CreateActionItemDto dto)
    {
        var review = await _db.ManagementReviews.FindAsync(id);
        if (review is null) return NotFound();

        if (!Enum.TryParse<ActionItemPriority>(dto.Priority, true, out var priority))
            return BadRequest($"Invalid priority: {dto.Priority}");

        var assignerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var assignerName   = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? assignerUserId;

        var item = new ActionItem
        {
            Title                 = dto.Title,
            Description           = dto.Description,
            AssignedToUserId      = dto.AssignedToUserId,
            AssignedToDisplayName = dto.AssignedToDisplayName,
            AssignedByUserId      = assignerUserId,
            AssignedByDisplayName = assignerName,
            DueDate               = dto.DueDate,
            Priority              = priority,
            SourceType            = ActionItemSourceType.ManagementReview,
            SourceEntityId        = id,
            Status                = ActionItemStatus.Open,
        };

        _db.ActionItems.Add(item);
        await _db.SaveChangesAsync();

        return MapActionItemToDto(item);
    }

    // ───── Mappers ─────

    private static ManagementReviewSummaryDto MapToSummary(ManagementReview r, int actionCount) => new(
        r.Id,
        r.Title,
        r.ReviewType.ToString(),
        r.ScheduledDate,
        r.Status.ToString(),
        r.ConductedBy,
        actionCount,
        r.CreatedAt,
        r.CreatedBy
    );

    private static ManagementReviewDto MapToDto(ManagementReview r, int actionCount) => new(
        r.Id,
        r.Title,
        r.ReviewType.ToString(),
        r.ScheduledDate,
        r.Status.ToString(),
        r.ConductedBy,
        r.NcSummary,
        r.ActionCloseRateSummary,
        r.MrbSummary,
        r.TrainingComplianceSummary,
        r.CustomerComplaintsNotes,
        r.SupplierQualityNotes,
        r.InternalAuditStatus,
        r.PriorActionsSummary,
        r.Decisions,
        r.NextCycleTargets,
        r.CreatedAt,
        r.CreatedBy,
        r.UpdatedAt,
        actionCount
    );

    private static ActionItemDto MapActionItemToDto(ActionItem a) => new(
        a.Id, a.Title, a.Description,
        a.AssignedToUserId, a.AssignedToDisplayName,
        a.AssignedByUserId, a.AssignedByDisplayName,
        a.DueDate, a.Priority.ToString(), a.Status.ToString(),
        a.SourceType.ToString(), a.SourceEntityId,
        a.CompletedBy, a.CompletedAt, a.CompletionNotes,
        a.VerifiedBy, a.VerifiedAt,
        a.CreatedAt, a.CreatedBy, a.UpdatedAt,
        IsOverdue: (a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress)
                 && a.DueDate < DateTime.UtcNow
    );

    private static ActionItemSummaryDto MapActionItemToSummary(ActionItem a, DateTime now) => new(
        a.Id, a.Title, a.AssignedToDisplayName, a.AssignedByDisplayName,
        a.DueDate, a.Priority.ToString(), a.Status.ToString(),
        a.SourceType.ToString(), a.SourceEntityId, a.CreatedAt,
        IsOverdue: (a.Status is ActionItemStatus.Open or ActionItemStatus.InProgress)
                 && a.DueDate < now
    );
}
