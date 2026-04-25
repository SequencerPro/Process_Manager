using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Roles = "Admin")]
public class BillingController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IStripeService _stripe;
    private readonly IPlanEnforcementService _planEnforcement;
    private readonly IConfiguration _configuration;

    public BillingController(ProcessManagerDbContext db, ITenantContext tenantContext, IStripeService stripe, IPlanEnforcementService planEnforcement, IConfiguration configuration)
    {
        _db = db;
        _tenantContext = tenantContext;
        _stripe = stripe;
        _planEnforcement = planEnforcement;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<BillingDashboardDto>> GetBillingDashboard()
    {
        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var usage = await _db.UsageMetrics
            .Where(m => m.PeriodStart >= periodStart && m.PeriodStart < periodEnd)
            .OrderBy(m => m.MetricType)
            .ToListAsync();

        var events = await _db.BillingEvents
            .OrderByDescending(e => e.ProcessedAt)
            .Take(20)
            .ToListAsync();

        return Ok(new BillingDashboardDto(
            subscription is null ? null : MapSubscription(subscription),
            usage.Select(MapUsage).ToList(),
            events.Select(MapEvent).ToList()));
    }

    [HttpGet("subscription")]
    public async Task<ActionResult<TenantSubscriptionDto>> GetSubscription()
    {
        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        if (subscription is null)
            return NotFound("No subscription found for this tenant.");

        return Ok(MapSubscription(subscription));
    }

    [HttpPost("portal-session")]
    public async Task<ActionResult<PortalSessionResultDto>> CreatePortalSession([FromBody] CreatePortalSessionDto dto)
    {
        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        if (subscription?.StripeCustomerId is null)
            return BadRequest("No Stripe customer found for this tenant.");

        var returnUrl = string.IsNullOrWhiteSpace(dto.ReturnUrl) ? "/" : dto.ReturnUrl;
        var url = await _stripe.CreateBillingPortalSessionAsync(subscription.StripeCustomerId, returnUrl);

        return Ok(new PortalSessionResultDto(url));
    }

    [HttpGet("usage")]
    public async Task<ActionResult<List<UsageMetricDto>>> GetUsageMetrics(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.UsageMetrics.AsQueryable();
        if (from.HasValue) query = query.Where(m => m.PeriodStart >= from.Value);
        if (to.HasValue) query = query.Where(m => m.PeriodEnd <= to.Value);

        var metrics = await query.OrderByDescending(m => m.PeriodStart).Take(100).ToListAsync();
        return Ok(metrics.Select(MapUsage).ToList());
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<BillingEventDto>>> GetBillingEvents([FromQuery] int limit = 50)
    {
        var events = await _db.BillingEvents
            .OrderByDescending(e => e.ProcessedAt)
            .Take(Math.Min(limit, 200))
            .ToListAsync();
        return Ok(events.Select(MapEvent).ToList());
    }

    [HttpGet("plan")]
    public async Task<ActionResult<PlanUsageSummaryDto>> GetPlanUsage()
    {
        var plan = await _planEnforcement.GetCurrentPlanAsync();
        var limits = _planEnforcement.GetLimitsForPlan(plan);

        var userCount = await _db.Users
            .Where(u => u.TenantId == _tenantContext.CurrentTenantId)
            .CountAsync();

        var processCount = await _db.Processes
            .Where(p => !p.IsSystemContent)
            .CountAsync();

        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var execMetric = await _db.UsageMetrics
            .FirstOrDefaultAsync(m =>
                m.MetricType == UsageMetricType.JobExecutions &&
                m.PeriodStart == periodStart);

        return Ok(new PlanUsageSummaryDto(
            plan,
            new PlanLimitsDto(plan, limits.MaxUsers, limits.MaxProcesses, limits.MaxSites, limits.MaxMonthlyExecutions, limits.AdvancedModulesEnabled),
            userCount,
            processCount,
            execMetric?.Count ?? 0));
    }

    [HttpGet("plan/check/{resource}")]
    public async Task<ActionResult<PlanCheckResultDto>> CheckPlanLimit(PlanResource resource)
    {
        var result = await _planEnforcement.CheckAsync(resource);
        return Ok(new PlanCheckResultDto(result.Outcome, result.Message, result.CurrentCount, result.Limit, result.SuggestedUpgrade));
    }

    [HttpPost("sync-features")]
    public async Task<ActionResult> SyncFeatureFlags()
    {
        var plan = await _planEnforcement.GetCurrentPlanAsync();
        await SyncFeatureFlagsForPlan(_db, _tenantContext.CurrentTenantId, plan);
        return Ok();
    }

    // ── F16: Plan Comparison ────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<ActionResult<List<PlanComparisonDto>>> GetPlanComparison()
    {
        var currentPlan = await _planEnforcement.GetCurrentPlanAsync();

        var plans = new List<PlanComparisonDto>();
        foreach (SubscriptionPlan plan in Enum.GetValues<SubscriptionPlan>())
        {
            var limits = _planEnforcement.GetLimitsForPlan(plan);
            var priceLabel = plan switch
            {
                SubscriptionPlan.Trial => "Free (30 days)",
                SubscriptionPlan.Starter => "$300/mo",
                SubscriptionPlan.Professional => "$600/mo",
                SubscriptionPlan.Enterprise => "Custom",
                _ => ""
            };

            plans.Add(new PlanComparisonDto(
                plan, plan.ToString(), priceLabel,
                limits.MaxUsers, limits.MaxProcesses, limits.MaxSites,
                limits.MaxMonthlyExecutions, limits.AdvancedModulesEnabled,
                plan == currentPlan));
        }

        return Ok(plans);
    }

    // ── F16: Checkout Session ───────────────────────────────────────────────

    [HttpPost("checkout-session")]
    public async Task<ActionResult<CheckoutSessionResultDto>> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
    {
        if (dto.TargetPlan == SubscriptionPlan.Trial || dto.TargetPlan == SubscriptionPlan.Enterprise)
            return BadRequest("Cannot create a checkout session for Trial or Enterprise plans.");

        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        if (subscription?.StripeCustomerId is null)
            return BadRequest("No Stripe customer found for this tenant.");

        var priceId = _configuration[$"Stripe:Prices:{dto.TargetPlan}"];
        if (string.IsNullOrEmpty(priceId))
            return BadRequest($"No price configured for plan {dto.TargetPlan}.");

        var successUrl = string.IsNullOrWhiteSpace(dto.SuccessUrl) ? "/billing?upgraded=true" : dto.SuccessUrl;
        var cancelUrl = string.IsNullOrWhiteSpace(dto.CancelUrl) ? "/billing/plans" : dto.CancelUrl;

        var result = await _stripe.CreateCheckoutSessionAsync(
            subscription.StripeCustomerId, priceId, successUrl, cancelUrl, dto.CouponCode);

        if (!string.IsNullOrEmpty(dto.CouponCode))
        {
            subscription.CouponCode = dto.CouponCode;
            await _db.SaveChangesAsync();
        }

        return Ok(new CheckoutSessionResultDto(result.Url, result.SessionId));
    }

    // ── F16: Change Plan ────────────────────────────────────────────────────

    [HttpPost("change-plan")]
    public async Task<ActionResult<ChangePlanResultDto>> ChangePlan([FromBody] ChangePlanDto dto)
    {
        if (dto.TargetPlan == SubscriptionPlan.Enterprise)
            return BadRequest("Enterprise plan changes require contacting sales.");

        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        if (subscription is null)
            return NotFound("No subscription found for this tenant.");

        var currentPlan = subscription.PlanCode;
        if (dto.TargetPlan == currentPlan)
            return BadRequest("Already on the requested plan.");

        var isDowngrade = dto.TargetPlan < currentPlan;
        string? downgradeWarning = null;

        if (isDowngrade)
        {
            var check = await CheckDowngradeSafety(dto.TargetPlan);
            if (check.HasExcessUsage)
            {
                var warnings = string.Join("; ", check.Warnings.Select(w =>
                    $"{w.Resource}: {w.CurrentUsage} in use, target limit {w.TargetLimit}"));
                downgradeWarning = $"Warning: current usage exceeds target plan limits. {warnings}. Limits will be enforced on next resource creation.";
            }
        }

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            var priceId = _configuration[$"Stripe:Prices:{dto.TargetPlan}"];
            if (!string.IsNullOrEmpty(priceId))
                await _stripe.UpdateSubscriptionPlanAsync(subscription.StripeSubscriptionId, priceId, isDowngrade);
        }

        var fromPlan = subscription.PlanCode;
        subscription.PlanCode = dto.TargetPlan;
        if (dto.TargetPlan != SubscriptionPlan.Trial)
            subscription.Status = SubscriptionStatus.Active;

        _db.PlanChangeLogs.Add(new PlanChangeLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.CurrentTenantId,
            FromPlan = fromPlan,
            ToPlan = dto.TargetPlan,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Reason = dto.Reason
        });

        await _db.SaveChangesAsync();
        await SyncFeatureFlagsForPlan(_db, _tenantContext.CurrentTenantId, dto.TargetPlan);

        return Ok(new ChangePlanResultDto(fromPlan, dto.TargetPlan, isDowngrade, downgradeWarning));
    }

    // ── F16: Plan Change History ────────────────────────────────────────────

    [HttpGet("plan-changes")]
    public async Task<ActionResult<List<PlanChangeLogDto>>> GetPlanChangeHistory([FromQuery] int limit = 50)
    {
        var logs = await _db.PlanChangeLogs
            .OrderByDescending(l => l.ChangedAt)
            .Take(Math.Min(limit, 200))
            .ToListAsync();

        return Ok(logs.Select(l => new PlanChangeLogDto(
            l.Id, l.FromPlan, l.ToPlan, l.ChangedAt, l.ChangedByUserId, l.Reason)).ToList());
    }

    // ── F16: Downgrade Safety Check ─────────────────────────────────────────

    [HttpGet("downgrade-check/{plan}")]
    public async Task<ActionResult<DowngradeCheckDto>> GetDowngradeCheck(SubscriptionPlan plan)
    {
        var result = await CheckDowngradeSafety(plan);
        return Ok(result);
    }

    private async Task<DowngradeCheckDto> CheckDowngradeSafety(SubscriptionPlan targetPlan)
    {
        var limits = _planEnforcement.GetLimitsForPlan(targetPlan);
        var warnings = new List<DowngradeWarningItem>();

        if (limits.MaxUsers is not null)
        {
            var userCount = await _db.Users
                .Where(u => u.TenantId == _tenantContext.CurrentTenantId)
                .CountAsync();
            if (userCount > limits.MaxUsers)
                warnings.Add(new DowngradeWarningItem("Users", userCount, limits.MaxUsers.Value));
        }

        if (limits.MaxProcesses is not null)
        {
            var processCount = await _db.Processes.Where(p => !p.IsSystemContent).CountAsync();
            if (processCount > limits.MaxProcesses)
                warnings.Add(new DowngradeWarningItem("Processes", processCount, limits.MaxProcesses.Value));
        }

        if (limits.MaxMonthlyExecutions is not null)
        {
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var metric = await _db.UsageMetrics
                .FirstOrDefaultAsync(m =>
                    m.MetricType == UsageMetricType.JobExecutions &&
                    m.PeriodStart == periodStart);
            var execCount = metric?.Count ?? 0;
            if (execCount > limits.MaxMonthlyExecutions)
                warnings.Add(new DowngradeWarningItem("MonthlyExecutions", execCount, limits.MaxMonthlyExecutions.Value));
        }

        return new DowngradeCheckDto(warnings.Count > 0, warnings);
    }

    internal static async Task SyncFeatureFlagsForPlan(ProcessManagerDbContext db, Guid tenantId, SubscriptionPlan plan)
    {
        var flags = await db.TenantFeatureFlags
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId);

        if (flags is null) return;

        var enableAdvanced = plan == SubscriptionPlan.Professional || plan == SubscriptionPlan.Enterprise;
        flags.ShowAdvancedModules = enableAdvanced;
        flags.ShowProductionTools = enableAdvanced || plan == SubscriptionPlan.Starter;
        flags.ShowWarehouseTools = enableAdvanced || plan == SubscriptionPlan.Starter;
        flags.ShowTrainingTools = enableAdvanced || plan == SubscriptionPlan.Starter;

        await db.SaveChangesAsync();
    }

    private static TenantSubscriptionDto MapSubscription(TenantSubscription s) => new(
        s.Id, s.PlanCode, s.Status, s.StripeCustomerId, s.StripeSubscriptionId,
        s.TrialEndsAt, s.CurrentPeriodEnd, s.GraceEndsAt, s.FailedPaymentCount);

    private static UsageMetricDto MapUsage(UsageMetric m) => new(
        m.Id, m.MetricType, m.Count, m.PeriodStart, m.PeriodEnd);

    private static BillingEventDto MapEvent(BillingEvent e) => new(
        e.Id, e.StripeEventId, e.EventType, e.Description, e.ProcessedAt);
}
