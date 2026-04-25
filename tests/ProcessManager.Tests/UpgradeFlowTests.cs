using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Tests;

public class UpgradeFlowTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public UpgradeFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Plan comparison returns all plans with current highlighted ───────────

    [Fact]
    public async Task GetPlanComparison_ReturnsAllPlansWithCurrentHighlighted()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var resp = await _client.GetAsync("/api/billing/plans");
        resp.EnsureSuccessStatusCode();

        var plans = await resp.Content.ReadFromJsonAsync<List<PlanComparisonDto>>(JsonOptions);
        Assert.NotNull(plans);
        Assert.Equal(4, plans.Count);

        var starter = plans.Single(p => p.Plan == SubscriptionPlan.Starter);
        Assert.True(starter.IsCurrent);
        Assert.Equal("$300/mo", starter.PriceLabel);
        Assert.Equal(25, starter.MaxUsers);

        var trial = plans.Single(p => p.Plan == SubscriptionPlan.Trial);
        Assert.False(trial.IsCurrent);
    }

    // ── Checkout session creation returns valid URL ──────────────────────────

    [Fact]
    public async Task CheckoutSession_ReturnsValidUrl()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);

        var dto = new CreateCheckoutSessionDto(
            SubscriptionPlan.Starter, "/billing?upgraded=true", "/billing/plans", null);

        var resp = await _client.PostAsJsonAsync("/api/billing/checkout-session", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<CheckoutSessionResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Contains("https://checkout.stripe.com", result.Url);
        Assert.StartsWith("cs_test_", result.SessionId);
    }

    // ── Checkout session rejects Trial and Enterprise targets ────────────────

    [Fact]
    public async Task CheckoutSession_RejectsTrialPlan()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var dto = new CreateCheckoutSessionDto(SubscriptionPlan.Trial, null, null, null);
        var resp = await _client.PostAsJsonAsync("/api/billing/checkout-session", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Plan change updates subscription and logs entry ──────────────────────

    [Fact]
    public async Task ChangePlan_UpdatesSubscriptionAndLogsEntry()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedFeatureFlags(showAdvancedModules: false);

        var dto = new ChangePlanDto(SubscriptionPlan.Starter, "User upgraded from trial");

        var resp = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ChangePlanResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(SubscriptionPlan.Trial, result.FromPlan);
        Assert.Equal(SubscriptionPlan.Starter, result.ToPlan);
        Assert.False(result.IsDowngrade);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var sub = await db.TenantSubscriptions.FirstOrDefaultAsync();
        Assert.NotNull(sub);
        Assert.Equal(SubscriptionPlan.Starter, sub.PlanCode);
        Assert.Equal(SubscriptionStatus.Active, sub.Status);

        var log = await db.PlanChangeLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(SubscriptionPlan.Trial, log.FromPlan);
        Assert.Equal(SubscriptionPlan.Starter, log.ToPlan);
        Assert.Equal("User upgraded from trial", log.Reason);
    }

    // ── Downgrade warning triggered when usage exceeds target limits ─────────

    [Fact]
    public async Task Downgrade_WarnsWhenUsageExceedsTargetLimits()
    {
        await SeedSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active);
        await SeedUsers(10);

        var dto = new ChangePlanDto(SubscriptionPlan.Trial, "Downgrade test");
        var resp = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ChangePlanResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsDowngrade);
        Assert.NotNull(result.DowngradeWarning);
        Assert.Contains("Users", result.DowngradeWarning);
    }

    // ── Feature flags synced on plan change ──────────────────────────────────

    [Fact]
    public async Task ChangePlan_SyncsFeatureFlags()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedFeatureFlags(showAdvancedModules: false);

        var dto = new ChangePlanDto(SubscriptionPlan.Professional, null);
        var resp = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var flags = await db.TenantFeatureFlags.FirstOrDefaultAsync();

        Assert.NotNull(flags);
        Assert.True(flags.ShowAdvancedModules);
    }

    // ── Coupon code applied at checkout ──────────────────────────────────────

    [Fact]
    public async Task CheckoutSession_AppliesCouponCode()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);

        var dto = new CreateCheckoutSessionDto(
            SubscriptionPlan.Starter, null, null, "SAVE20");

        var resp = await _client.PostAsJsonAsync("/api/billing/checkout-session", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var sub = await db.TenantSubscriptions.FirstOrDefaultAsync();
        Assert.Equal("SAVE20", sub?.CouponCode);
    }

    // ── Plan change logged in audit trail ────────────────────────────────────

    [Fact]
    public async Task PlanChangeHistory_ReturnsPriorChanges()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);

        var dto1 = new ChangePlanDto(SubscriptionPlan.Starter, "First upgrade");
        await _client.PostAsJsonAsync("/api/billing/change-plan", dto1, JsonOptions);

        var dto2 = new ChangePlanDto(SubscriptionPlan.Professional, "Second upgrade");
        await _client.PostAsJsonAsync("/api/billing/change-plan", dto2, JsonOptions);

        var resp = await _client.GetAsync("/api/billing/plan-changes");
        resp.EnsureSuccessStatusCode();

        var logs = await resp.Content.ReadFromJsonAsync<List<PlanChangeLogDto>>(JsonOptions);
        Assert.NotNull(logs);
        Assert.True(logs.Count >= 2);
        Assert.Equal(SubscriptionPlan.Professional, logs[0].ToPlan);
        Assert.Equal(SubscriptionPlan.Starter, logs[1].ToPlan);
    }

    // ── Non-admin cannot change plan ────────────────────────────────────────

    [Fact]
    public async Task NonAdmin_CannotChangePlan()
    {
        var participantClient = _factory.CreateAuthenticatedClient("participant-user", "Participant");

        var dto = new ChangePlanDto(SubscriptionPlan.Starter, null);
        var resp = await participantClient.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Cannot change to same plan ──────────────────────────────────────────

    [Fact]
    public async Task ChangePlan_RejectsSamePlan()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var dto = new ChangePlanDto(SubscriptionPlan.Starter, null);
        var resp = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Downgrade check endpoint returns correct warnings ───────────────────

    [Fact]
    public async Task DowngradeCheck_ReturnsWarningsForExcessUsage()
    {
        await SeedSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active);
        await SeedUsers(5);

        var resp = await _client.GetAsync("/api/billing/downgrade-check/Trial");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<DowngradeCheckDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.HasExcessUsage);
        Assert.Contains(result.Warnings, w => w.Resource == "Users" && w.TargetLimit == 3);
    }

    // ── Concurrent plan changes are idempotent ──────────────────────────────

    [Fact]
    public async Task ConcurrentPlanChanges_SecondReturnsAlreadyOnPlan()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);

        var dto = new ChangePlanDto(SubscriptionPlan.Starter, null);
        var resp1 = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        resp1.EnsureSuccessStatusCode();

        var resp2 = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
        var body = await resp2.Content.ReadAsStringAsync();
        Assert.Contains("Already on the requested plan", body);
    }

    // ── Enterprise plan change rejected ─────────────────────────────────────

    [Fact]
    public async Task ChangePlan_RejectsEnterprise()
    {
        await SeedSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active);

        var dto = new ChangePlanDto(SubscriptionPlan.Enterprise, null);
        var resp = await _client.PostAsJsonAsync("/api/billing/change-plan", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task SeedSubscription(SubscriptionPlan plan, SubscriptionStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            var existing = await db.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.TenantId == TestWebApplicationFactory.DefaultTenantId);
            if (existing is not null)
            {
                existing.PlanCode = plan;
                existing.Status = status;
                existing.StripeCustomerId ??= $"cus_test_{TestWebApplicationFactory.DefaultTenantId.ToString()[..8]}";
                existing.StripeSubscriptionId ??= $"sub_test_{Guid.NewGuid().ToString()[..8]}";
                existing.CouponCode = null;
                await db.SaveChangesAsync();
                return;
            }

            db.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = TestWebApplicationFactory.DefaultTenantId,
                PlanCode = plan,
                Status = status,
                StripeCustomerId = $"cus_test_{TestWebApplicationFactory.DefaultTenantId.ToString()[..8]}",
                StripeSubscriptionId = $"sub_test_{Guid.NewGuid().ToString()[..8]}",
                TrialEndsAt = status == SubscriptionStatus.Trial ? DateTime.UtcNow.AddDays(30) : null,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task SeedUsers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pfx = Guid.NewGuid().ToString("N")[..8];
            var dto = new RegisterRequestDto(
                $"upuser-{pfx}", $"up-{pfx}@test.local",
                "Password123!", "Participant", $"Upgrade User {i}");
            await _client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        }
    }

    private async Task SeedFeatureFlags(bool showAdvancedModules)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            var existing = await db.TenantFeatureFlags.FirstOrDefaultAsync();
            if (existing is not null)
            {
                existing.ShowAdvancedModules = showAdvancedModules;
                await db.SaveChangesAsync();
                return;
            }

            db.TenantFeatureFlags.Add(new TenantFeatureFlags
            {
                Id = Guid.NewGuid(),
                TenantId = TestWebApplicationFactory.DefaultTenantId,
                ShowAdvancedModules = showAdvancedModules,
                ShowQualityTools = true,
                ShowProductionTools = false,
                ShowWarehouseTools = false,
                ShowTrainingTools = false
            });
            await db.SaveChangesAsync();
        }
    }
}
