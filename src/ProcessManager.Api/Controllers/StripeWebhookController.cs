using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/platform/stripe-webhook")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IStripeService _stripe;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ProcessManagerDbContext db,
        IStripeService stripe,
        ITenantContext tenantContext,
        ILogger<StripeWebhookController> logger)
    {
        _db = db;
        _stripe = stripe;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        string payload;
        using (var reader = new StreamReader(HttpContext.Request.Body))
            payload = await reader.ReadToEndAsync();

        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        if (!_stripe.VerifyWebhookSignature(payload, signature,
                out var eventId, out var eventType, out var rawEvent))
        {
            return BadRequest("Invalid webhook signature.");
        }

        if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(eventType))
            return BadRequest("Missing event data.");

        var alreadyProcessed = await _db.BillingEvents
            .IgnoreQueryFilters()
            .AnyAsync(e => e.StripeEventId == eventId);
        if (alreadyProcessed)
            return Ok("Already processed.");

        var result = eventType switch
        {
            "invoice.payment_succeeded" => await HandlePaymentSucceeded(eventId, rawEvent),
            "invoice.payment_failed" => await HandlePaymentFailed(eventId, rawEvent),
            "customer.subscription.deleted" => await HandleSubscriptionDeleted(eventId, rawEvent),
            "customer.subscription.updated" => await HandleSubscriptionUpdated(eventId, rawEvent),
            _ => Ok("Event type not handled.")
        };

        return result;
    }

    private async Task<IActionResult> HandlePaymentSucceeded(string eventId, string? rawEvent)
    {
        var subscriptions = await _db.TenantSubscriptions.IgnoreQueryFilters().ToListAsync();
        foreach (var sub in subscriptions)
        {
            if (sub.Status == SubscriptionStatus.PastDue || sub.Status == SubscriptionStatus.Trial)
            {
                using (_tenantContext.BeginScope(sub.TenantId))
                {
                    sub.Status = SubscriptionStatus.Active;
                    sub.FailedPaymentCount = 0;
                    sub.GraceEndsAt = null;

                    var tenant = await _db.Tenants.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Id == sub.TenantId);
                    if (tenant is { Status: TenantStatus.Suspended })
                    {
                        tenant.Status = TenantStatus.Active;
                        tenant.UpdatedAt = DateTime.UtcNow;
                    }

                    await BillingController.SyncFeatureFlagsForPlan(_db, sub.TenantId, sub.PlanCode);

                    await RecordBillingEvent(sub.TenantId, eventId, BillingEventType.PaymentSucceeded,
                        "Payment succeeded — subscription reactivated.", rawEvent);
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok();
    }

    private async Task<IActionResult> HandlePaymentFailed(string eventId, string? rawEvent)
    {
        var graceDays = 3;

        var subscriptions = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            using (_tenantContext.BeginScope(sub.TenantId))
            {
                sub.FailedPaymentCount++;
                sub.Status = SubscriptionStatus.PastDue;
                sub.GraceEndsAt ??= DateTime.UtcNow.AddDays(graceDays);

                await RecordBillingEvent(sub.TenantId, eventId, BillingEventType.PaymentFailed,
                    $"Payment failed (attempt {sub.FailedPaymentCount}).", rawEvent);

                if (sub.GraceEndsAt <= DateTime.UtcNow)
                {
                    sub.Status = SubscriptionStatus.Suspended;
                    var tenant = await _db.Tenants.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Id == sub.TenantId);
                    if (tenant is not null)
                    {
                        tenant.Status = TenantStatus.Suspended;
                        tenant.UpdatedAt = DateTime.UtcNow;
                    }

                    await RecordBillingEvent(sub.TenantId, $"{eventId}-suspend", BillingEventType.TenantSuspended,
                        "Tenant suspended after grace period expired.", null);
                }

                await _db.SaveChangesAsync();
            }
        }

        return Ok();
    }

    private async Task<IActionResult> HandleSubscriptionDeleted(string eventId, string? rawEvent)
    {
        var subscriptions = await _db.TenantSubscriptions.IgnoreQueryFilters().ToListAsync();
        foreach (var sub in subscriptions)
        {
            using (_tenantContext.BeginScope(sub.TenantId))
            {
                sub.Status = SubscriptionStatus.Cancelled;

                var tenant = await _db.Tenants.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == sub.TenantId);
                if (tenant is not null)
                {
                    tenant.Status = TenantStatus.Suspended;
                    tenant.UpdatedAt = DateTime.UtcNow;
                }

                await RecordBillingEvent(sub.TenantId, eventId, BillingEventType.SubscriptionCancelled,
                    "Subscription cancelled.", rawEvent);
                await _db.SaveChangesAsync();
            }
        }

        return Ok();
    }

    private async Task<IActionResult> HandleSubscriptionUpdated(string eventId, string? rawEvent)
    {
        var subscriptions = await _db.TenantSubscriptions.IgnoreQueryFilters().ToListAsync();
        foreach (var sub in subscriptions)
        {
            using (_tenantContext.BeginScope(sub.TenantId))
            {
                await RecordBillingEvent(sub.TenantId, eventId, BillingEventType.SubscriptionUpdated,
                    "Subscription updated.", rawEvent);
                await _db.SaveChangesAsync();
            }
        }

        return Ok();
    }

    private async Task RecordBillingEvent(Guid tenantId, string stripeEventId, BillingEventType eventType,
        string? description, string? rawPayload)
    {
        _db.BillingEvents.Add(new BillingEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StripeEventId = stripeEventId,
            EventType = eventType,
            Description = description,
            RawPayload = rawPayload?.Length > 2000 ? rawPayload[..2000] : rawPayload,
            ProcessedAt = DateTime.UtcNow
        });
    }
}
