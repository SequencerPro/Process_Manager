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
[Route("api/quality-costs")]
public class QualityCostsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public QualityCostsController(ProcessManagerDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<QualityCostSummaryDto>>> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] Guid? kindId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.QualityCosts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<QualityCostCategory>(category, true, out var cat))
            query = query.Where(q => q.CostCategory == cat);

        if (!string.IsNullOrWhiteSpace(sourceType) && Enum.TryParse<QualityCostSourceType>(sourceType, true, out var st))
            query = query.Where(q => q.SourceType == st);

        if (kindId.HasValue)
            query = query.Where(q => q.KindId == kindId.Value);

        if (dateFrom.HasValue)
            query = query.Where(q => q.RecordedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(q => q.RecordedAt <= dateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(q => q.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummaryDto).ToList();

        return new PaginatedResponse<QualityCostSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QualityCostResponseDto>> GetById(Guid id)
    {
        var cost = await _db.QualityCosts.FirstOrDefaultAsync(q => q.Id == id);
        if (cost is null) return NotFound();
        return MapToResponseDto(cost);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<QualityCostResponseDto>> Create([FromBody] CreateQualityCostDto dto)
    {
        if (!Enum.TryParse<QualityCostSourceType>(dto.SourceType, true, out var sourceType))
            return BadRequest($"Invalid source type: {dto.SourceType}");

        if (!Enum.TryParse<QualityCostCategory>(dto.CostCategory, true, out var costCategory))
            return BadRequest($"Invalid cost category: {dto.CostCategory}");

        var cost = new QualityCost
        {
            SourceType = sourceType,
            SourceEntityId = dto.SourceEntityId,
            SourceEntityCode = dto.SourceEntityCode,
            Amount = dto.Amount,
            Currency = dto.Currency ?? "USD",
            CostCategory = costCategory,
            KindId = dto.KindId,
            KindName = dto.KindName,
            JobId = dto.JobId,
            Description = dto.Description,
            RecordedByUserId = dto.RecordedByUserId,
            RecordedByDisplayName = dto.RecordedByDisplayName,
            RecordedAt = DateTime.UtcNow
        };

        _db.QualityCosts.Add(cost);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cost.Id }, MapToResponseDto(cost));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QualityCostResponseDto>> Update(Guid id, [FromBody] UpdateQualityCostDto dto)
    {
        var cost = await _db.QualityCosts.FirstOrDefaultAsync(q => q.Id == id);
        if (cost is null) return NotFound();

        if (dto.SourceType is not null)
        {
            if (!Enum.TryParse<QualityCostSourceType>(dto.SourceType, true, out var st))
                return BadRequest($"Invalid source type: {dto.SourceType}");
            cost.SourceType = st;
        }

        if (dto.CostCategory is not null)
        {
            if (!Enum.TryParse<QualityCostCategory>(dto.CostCategory, true, out var cat))
                return BadRequest($"Invalid cost category: {dto.CostCategory}");
            cost.CostCategory = cat;
        }

        if (dto.SourceEntityId.HasValue) cost.SourceEntityId = dto.SourceEntityId;
        if (dto.SourceEntityCode is not null) cost.SourceEntityCode = dto.SourceEntityCode;
        if (dto.Amount.HasValue) cost.Amount = dto.Amount.Value;
        if (dto.Currency is not null) cost.Currency = dto.Currency;
        if (dto.KindId.HasValue) cost.KindId = dto.KindId;
        if (dto.KindName is not null) cost.KindName = dto.KindName;
        if (dto.JobId.HasValue) cost.JobId = dto.JobId;
        if (dto.Description is not null) cost.Description = dto.Description;

        await _db.SaveChangesAsync();

        return MapToResponseDto(cost);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cost = await _db.QualityCosts.FirstOrDefaultAsync(q => q.Id == id);
        if (cost is null) return NotFound();

        _db.QualityCosts.Remove(cost);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<CoqDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var quarterMonth = ((now.Month - 1) / 3) * 3 + 1;
        var quarterStart = new DateTime(now.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var allCosts = await _db.QualityCosts.ToListAsync();

        var totalMonth = allCosts.Where(c => c.RecordedAt >= monthStart).Sum(c => c.Amount);
        var totalQuarter = allCosts.Where(c => c.RecordedAt >= quarterStart).Sum(c => c.Amount);
        var totalYear = allCosts.Where(c => c.RecordedAt >= yearStart).Sum(c => c.Amount);

        var byCategory = allCosts
            .GroupBy(c => c.CostCategory.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var bySourceType = allCosts
            .GroupBy(c => c.SourceType.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var trendStart = yearStart.AddMonths(-11);
        var monthlyTrend = allCosts
            .Where(c => c.RecordedAt >= trendStart)
            .GroupBy(c => new { c.RecordedAt.Year, c.RecordedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new CoqTrendPointDto(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Where(c => c.CostCategory == QualityCostCategory.Prevention).Sum(c => c.Amount),
                g.Where(c => c.CostCategory == QualityCostCategory.Appraisal).Sum(c => c.Amount),
                g.Where(c => c.CostCategory == QualityCostCategory.InternalFailure).Sum(c => c.Amount),
                g.Where(c => c.CostCategory == QualityCostCategory.ExternalFailure).Sum(c => c.Amount),
                g.Sum(c => c.Amount)))
            .ToList();

        var topDrivers = allCosts
            .Where(c => c.KindName is not null)
            .GroupBy(c => c.KindName!)
            .OrderByDescending(g => g.Sum(c => c.Amount))
            .Take(10)
            .Select(g => new CoqTopDriverDto(g.Key, g.Sum(c => c.Amount), g.Count()))
            .ToList();

        return new CoqDashboardDto(
            totalMonth, totalQuarter, totalYear,
            byCategory, bySourceType, monthlyTrend, topDrivers,
            allCosts.Count);
    }

    // ── Rules ─────────────────────────────────────────────────────────────────

    [HttpGet("rules")]
    public async Task<ActionResult<List<QualityCostRuleResponseDto>>> GetRules([FromQuery] bool? activeOnly = null)
    {
        var query = _db.QualityCostRules.AsQueryable();
        if (activeOnly == true)
            query = query.Where(r => r.IsActive);

        var rules = await query.OrderBy(r => r.TriggerEvent).ToListAsync();
        return rules.Select(MapRuleToDto).ToList();
    }

    [HttpGet("rules/{id:guid}")]
    public async Task<ActionResult<QualityCostRuleResponseDto>> GetRuleById(Guid id)
    {
        var rule = await _db.QualityCostRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();
        return MapRuleToDto(rule);
    }

    [HttpPost("rules")]
    public async Task<ActionResult<QualityCostRuleResponseDto>> CreateRule([FromBody] CreateQualityCostRuleDto dto)
    {
        if (!Enum.TryParse<QualityCostTriggerEvent>(dto.TriggerEvent, true, out var trigger))
            return BadRequest($"Invalid trigger event: {dto.TriggerEvent}");

        if (!Enum.TryParse<QualityCostCategory>(dto.DefaultCategory, true, out var category))
            return BadRequest($"Invalid cost category: {dto.DefaultCategory}");

        if (!Enum.TryParse<QualityCostSourceType>(dto.DefaultSourceType, true, out var sourceType))
            return BadRequest($"Invalid source type: {dto.DefaultSourceType}");

        var existing = await _db.QualityCostRules
            .AnyAsync(r => r.TriggerEvent == trigger && r.IsActive);
        if (existing)
            return Conflict($"An active rule already exists for trigger event: {dto.TriggerEvent}");

        var rule = new QualityCostRule
        {
            TriggerEvent = trigger,
            DefaultCategory = category,
            DefaultSourceType = sourceType,
            DefaultAmount = dto.DefaultAmount,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _db.QualityCostRules.Add(rule);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRuleById), new { id = rule.Id }, MapRuleToDto(rule));
    }

    [HttpPut("rules/{id:guid}")]
    public async Task<ActionResult<QualityCostRuleResponseDto>> UpdateRule(Guid id, [FromBody] UpdateQualityCostRuleDto dto)
    {
        var rule = await _db.QualityCostRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();

        if (dto.TriggerEvent is not null)
        {
            if (!Enum.TryParse<QualityCostTriggerEvent>(dto.TriggerEvent, true, out var trigger))
                return BadRequest($"Invalid trigger event: {dto.TriggerEvent}");
            rule.TriggerEvent = trigger;
        }

        if (dto.DefaultCategory is not null)
        {
            if (!Enum.TryParse<QualityCostCategory>(dto.DefaultCategory, true, out var cat))
                return BadRequest($"Invalid cost category: {dto.DefaultCategory}");
            rule.DefaultCategory = cat;
        }

        if (dto.DefaultSourceType is not null)
        {
            if (!Enum.TryParse<QualityCostSourceType>(dto.DefaultSourceType, true, out var st))
                return BadRequest($"Invalid source type: {dto.DefaultSourceType}");
            rule.DefaultSourceType = st;
        }

        if (dto.DefaultAmount.HasValue) rule.DefaultAmount = dto.DefaultAmount.Value;
        if (dto.Description is not null) rule.Description = dto.Description;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();

        return MapRuleToDto(rule);
    }

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await _db.QualityCostRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();

        _db.QualityCostRules.Remove(rule);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static QualityCostResponseDto MapToResponseDto(QualityCost c) => new(
        c.Id,
        c.SourceType.ToString(),
        c.SourceEntityId,
        c.SourceEntityCode,
        c.Amount,
        c.Currency,
        c.CostCategory.ToString(),
        c.KindId,
        c.KindName,
        c.JobId,
        c.Description,
        c.RecordedByUserId,
        c.RecordedByDisplayName,
        c.RecordedAt,
        c.CreatedAt,
        c.UpdatedAt);

    private static QualityCostSummaryDto MapToSummaryDto(QualityCost c) => new(
        c.Id,
        c.SourceType.ToString(),
        c.SourceEntityCode,
        c.Amount,
        c.Currency,
        c.CostCategory.ToString(),
        c.KindName,
        c.Description,
        c.RecordedByDisplayName,
        c.RecordedAt);

    private static QualityCostRuleResponseDto MapRuleToDto(QualityCostRule r) => new(
        r.Id,
        r.TriggerEvent.ToString(),
        r.DefaultCategory.ToString(),
        r.DefaultSourceType.ToString(),
        r.DefaultAmount,
        r.Description,
        r.IsActive,
        r.CreatedAt,
        r.UpdatedAt);
}
