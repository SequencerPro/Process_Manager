using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// CRUD for webhook subscriptions and delivery log viewer.
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Authorize(Roles = "Admin")]
public class WebhooksController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IWebhookEventPublisher? _publisher;

    public WebhooksController(ProcessManagerDbContext db, IWebhookEventPublisher? publisher = null)
    {
        _db = db;
        _publisher = publisher;
    }

    // ── List subscriptions ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.WebhookSubscriptions.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new WebhookSubscriptionDto(
                s.Id, s.Url, s.Secret != null,
                s.EventTypes, s.Description, s.IsActive,
                s.Deliveries.Count,
                s.CreatedAt, s.UpdatedAt))
            .ToListAsync();

        return Ok(new PaginatedResponse<WebhookSubscriptionDto>(items, totalCount, page, pageSize));
    }

    // ── Get single ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var s = await _db.WebhookSubscriptions
            .AsNoTracking()
            .Include(s => s.Deliveries)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s is null) return NotFound();

        return Ok(new WebhookSubscriptionDto(
            s.Id, s.Url, s.Secret != null,
            s.EventTypes, s.Description, s.IsActive,
            s.Deliveries.Count,
            s.CreatedAt, s.UpdatedAt));
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookSubscriptionDto dto)
    {
        var sub = new WebhookSubscription
        {
            Url = dto.Url,
            Secret = dto.Secret,
            EventTypes = dto.EventTypes,
            Description = dto.Description,
            IsActive = true,
        };

        _db.WebhookSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = sub.Id }, new WebhookSubscriptionDto(
            sub.Id, sub.Url, sub.Secret != null,
            sub.EventTypes, sub.Description, sub.IsActive,
            0, sub.CreatedAt, sub.UpdatedAt));
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWebhookSubscriptionDto dto)
    {
        var sub = await _db.WebhookSubscriptions.FindAsync(id);
        if (sub is null) return NotFound();

        sub.Url = dto.Url;
        sub.EventTypes = dto.EventTypes;
        sub.Description = dto.Description;
        sub.IsActive = dto.IsActive;

        // Only update secret if a new value is explicitly provided
        if (dto.Secret is not null)
            sub.Secret = dto.Secret;

        sub.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var deliveryCount = await _db.WebhookDeliveries.CountAsync(d => d.WebhookSubscriptionId == id);

        return Ok(new WebhookSubscriptionDto(
            sub.Id, sub.Url, sub.Secret != null,
            sub.EventTypes, sub.Description, sub.IsActive,
            deliveryCount, sub.CreatedAt, sub.UpdatedAt));
    }

    // ── Delete (deactivate) ─────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var sub = await _db.WebhookSubscriptions.FindAsync(id);
        if (sub is null) return NotFound();

        sub.IsActive = false;
        sub.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Delivery log ────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> Deliveries(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var exists = await _db.WebhookSubscriptions.AnyAsync(s => s.Id == id);
        if (!exists) return NotFound();

        var query = _db.WebhookDeliveries
            .AsNoTracking()
            .Where(d => d.WebhookSubscriptionId == id);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new WebhookDeliveryDto(
                d.Id, d.WebhookSubscriptionId, d.EventType, d.Payload,
                d.StatusCode, d.ResponseBody, d.ErrorMessage,
                d.IsSuccess, d.AttemptNumber,
                d.CreatedAt, d.DeliveredAt))
            .ToListAsync();

        return Ok(new PaginatedResponse<WebhookDeliveryDto>(items, totalCount, page, pageSize));
    }

    // ── Test webhook ────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id)
    {
        var sub = await _db.WebhookSubscriptions.FindAsync(id);
        if (sub is null) return NotFound();
        if (!sub.IsActive) return BadRequest("Subscription is inactive.");

        _publisher?.Publish("webhook.test", new
        {
            subscriptionId = id,
            message = "This is a test webhook event from Process Manager.",
            timestamp = DateTime.UtcNow,
        });

        return Ok(new { message = "Test event published. Check deliveries for result." });
    }
}
