using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Tests;

public class BillingTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BillingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Signup creates trial subscription ────────────────────────────────────

    [Fact]
    public async Task Signup_CreatesTrialSubscription()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var signupDto = new PublicSignupDto(
            $"BillingCo-{pfx}", $"billing-{pfx}", OnboardingIndustry.General,
            $"billadmin-{pfx}", $"bill-{pfx}@test.local", "Password123!", "Bill Admin");

        var anonClient = _factory.CreateClient();
        var resp = await anonClient.PostAsJsonAsync("/api/public/signup", signupDto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PublicSignupResultDto>(JsonOptions);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var sub = await db.TenantSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == result!.TenantId);

        Assert.NotNull(sub);
        Assert.Equal(SubscriptionPlan.Trial, sub.PlanCode);
        Assert.Equal(SubscriptionStatus.Trial, sub.Status);
        Assert.NotNull(sub.TrialEndsAt);
        Assert.True(sub.TrialEndsAt > DateTime.UtcNow.AddDays(29));
    }

    // ── Billing dashboard returns subscription and usage ─────────────────────

    [Fact]
    public async Task BillingDashboard_ReturnsSubscriptionData()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var resp = await _client.GetAsync("/api/billing");
        resp.EnsureSuccessStatusCode();

        var dashboard = await resp.Content.ReadFromJsonAsync<BillingDashboardDto>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.NotNull(dashboard.Subscription);
        Assert.Equal(SubscriptionPlan.Starter, dashboard.Subscription.PlanCode);
        Assert.Equal(SubscriptionStatus.Active, dashboard.Subscription.Status);
    }

    // ── Get subscription endpoint ────────────────────────────────────────────

    [Fact]
    public async Task GetSubscription_ReturnsExistingSubscription()
    {
        await SeedSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active);

        var resp = await _client.GetAsync("/api/billing/subscription");
        resp.EnsureSuccessStatusCode();

        var sub = await resp.Content.ReadFromJsonAsync<TenantSubscriptionDto>(JsonOptions);
        Assert.NotNull(sub);
        Assert.Equal(SubscriptionPlan.Professional, sub.PlanCode);
    }

    [Fact]
    public async Task GetSubscription_Returns404_WhenNoneExists()
    {
        var tenantId = _factory.CreateTenant("no-sub-tenant");
        var client = _factory.CreateTenantClient(tenantId);

        var resp = await client.GetAsync("/api/billing/subscription");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Webhook: invalid signature rejected ──────────────────────────────────

    [Fact]
    public async Task StripeWebhook_InvalidSignature_Returns400()
    {
        var stripeService = GetTestStripeService();
        stripeService.VerifySignature = false;

        var anonClient = _factory.CreateClient();
        var content = new StringContent("{\"type\":\"test\"}", Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "invalid_sig");

        var resp = await anonClient.PostAsync("/api/platform/stripe-webhook", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        stripeService.VerifySignature = true;
    }

    // ── Webhook: duplicate event idempotently ignored ────────────────────────

    [Fact]
    public async Task StripeWebhook_DuplicateEvent_IdempotentlyIgnored()
    {
        var stripeService = GetTestStripeService();
        var eventId = $"evt_dup_{Guid.NewGuid().ToString()[..8]}";
        stripeService.NextEventId = eventId;
        stripeService.NextEventType = "customer.subscription.updated";

        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var anonClient = _factory.CreateClient();
        var content1 = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp1 = await anonClient.PostAsync("/api/platform/stripe-webhook", content1);
        resp1.EnsureSuccessStatusCode();

        using (var scope1 = _factory.Services.CreateScope())
        {
            var db1 = scope1.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var countAfterFirst = await db1.BillingEvents.IgnoreQueryFilters()
                .CountAsync(e => e.StripeEventId == eventId);
            Assert.True(countAfterFirst >= 1);
        }

        var content2 = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp2 = await anonClient.PostAsync("/api/platform/stripe-webhook", content2);
        var body = await resp2.Content.ReadAsStringAsync();
        resp2.EnsureSuccessStatusCode();
        Assert.Contains("Already processed", body);
    }

    // ── Webhook: payment succeeded reactivates subscription ──────────────────

    [Fact]
    public async Task StripeWebhook_PaymentSucceeded_ActivatesSubscription()
    {
        var sub = await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.PastDue);

        var stripeService = GetTestStripeService();
        var eventId = $"evt_pay_{Guid.NewGuid().ToString()[..8]}";
        stripeService.NextEventId = eventId;
        stripeService.NextEventType = "invoice.payment_succeeded";

        var anonClient = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await anonClient.PostAsync("/api/platform/stripe-webhook", content);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var updated = await db.TenantSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sub.Id);
        Assert.Equal(SubscriptionStatus.Active, updated!.Status);
        Assert.Equal(0, updated.FailedPaymentCount);
    }

    // ── Webhook: payment failed sets PastDue ─────────────────────────────────

    [Fact]
    public async Task StripeWebhook_PaymentFailed_SetsPastDueAndGrace()
    {
        var sub = await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var stripeService = GetTestStripeService();
        var eventId = $"evt_fail_{Guid.NewGuid().ToString()[..8]}";
        stripeService.NextEventId = eventId;
        stripeService.NextEventType = "invoice.payment_failed";

        var anonClient = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await anonClient.PostAsync("/api/platform/stripe-webhook", content);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var updated = await db.TenantSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sub.Id);
        Assert.Equal(SubscriptionStatus.PastDue, updated!.Status);
        Assert.True(updated.FailedPaymentCount >= 1);
        Assert.NotNull(updated.GraceEndsAt);
    }

    // ── Suspended tenant blocked from API (402) ──────────────────────────────

    [Fact]
    public async Task SuspendedTenant_CannotAccessApiEndpoints()
    {
        var tenantId = _factory.CreateTenant("suspended-test");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstAsync(t => t.Id == tenantId);
        tenant.Status = TenantStatus.Suspended;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var client = _factory.CreateTenantClient(tenantId);
        var resp = await client.GetAsync("/api/kinds");
        Assert.Equal(HttpStatusCode.PaymentRequired, resp.StatusCode);
    }

    // ── Suspended tenant can still access billing ────────────────────────────

    [Fact]
    public async Task SuspendedTenant_CanStillAccessBillingEndpoint()
    {
        var tenantId = _factory.CreateTenant("suspended-billing");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var tenant = await db.Tenants.IgnoreQueryFilters()
                .FirstAsync(t => t.Id == tenantId);
            tenant.Status = TenantStatus.Suspended;
            tenant.UpdatedAt = DateTime.UtcNow;

            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            using (tenantContext.BeginScope(tenantId))
            {
                db.TenantSubscriptions.Add(new TenantSubscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PlanCode = SubscriptionPlan.Starter,
                    Status = SubscriptionStatus.Suspended,
                    StripeCustomerId = "cus_test_suspended"
                });
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateTenantClient(tenantId);
        var resp = await client.GetAsync("/api/billing");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Usage metrics recorded ───────────────────────────────────────────────

    [Fact]
    public async Task UsageMetrics_CanBeQueried()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
            {
                db.UsageMetrics.Add(new UsageMetric
                {
                    Id = Guid.NewGuid(),
                    TenantId = TestWebApplicationFactory.DefaultTenantId,
                    MetricType = UsageMetricType.JobExecutions,
                    Count = 42,
                    PeriodStart = periodStart,
                    PeriodEnd = periodStart.AddMonths(1)
                });
                await db.SaveChangesAsync();
            }
        }

        var resp = await _client.GetAsync("/api/billing/usage");
        resp.EnsureSuccessStatusCode();

        var metrics = await resp.Content.ReadFromJsonAsync<List<UsageMetricDto>>(JsonOptions);
        Assert.NotNull(metrics);
        Assert.Contains(metrics, m => m.MetricType == UsageMetricType.JobExecutions && m.Count == 42);
    }

    // ── Billing events list ──────────────────────────────────────────────────

    [Fact]
    public async Task BillingEvents_ReturnsList()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
            {
                db.BillingEvents.Add(new BillingEvent
                {
                    Id = Guid.NewGuid(),
                    TenantId = TestWebApplicationFactory.DefaultTenantId,
                    StripeEventId = $"evt_list_{Guid.NewGuid().ToString()[..8]}",
                    EventType = BillingEventType.TrialStarted,
                    Description = "Trial started on signup",
                    ProcessedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        var resp = await _client.GetAsync("/api/billing/events");
        resp.EnsureSuccessStatusCode();

        var events = await resp.Content.ReadFromJsonAsync<List<BillingEventDto>>(JsonOptions);
        Assert.NotNull(events);
        Assert.True(events.Count >= 1);
    }

    // ── Billing requires authentication ──────────────────────────────────────

    [Fact]
    public async Task BillingEndpoints_RequireAuthentication()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/billing");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-tenant billing isolation ───────────────────────────────────────

    [Fact]
    public async Task BillingSubscription_IsolatedBetweenTenants()
    {
        var tenantAId = _factory.CreateTenant("billing-a");
        var tenantBId = _factory.CreateTenant("billing-b");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

            using (tenantContext.BeginScope(tenantAId))
            {
                db.TenantSubscriptions.Add(new TenantSubscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantAId,
                    PlanCode = SubscriptionPlan.Professional,
                    Status = SubscriptionStatus.Active,
                    StripeCustomerId = "cus_a"
                });
                await db.SaveChangesAsync();
            }
        }

        var clientB = _factory.CreateTenantClient(tenantBId);
        var resp = await clientB.GetAsync("/api/billing/subscription");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<TenantSubscription> SeedSubscription(
        SubscriptionPlan plan, SubscriptionStatus status, Guid? tenantId = null)
    {
        var tid = tenantId ?? TestWebApplicationFactory.DefaultTenantId;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(tid))
        {
            var existing = await db.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.TenantId == tid);
            if (existing is not null)
            {
                existing.PlanCode = plan;
                existing.Status = status;
                existing.StripeCustomerId ??= $"cus_test_{tid.ToString()[..8]}";
                await db.SaveChangesAsync();
                return existing;
            }

            var sub = new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tid,
                PlanCode = plan,
                Status = status,
                StripeCustomerId = $"cus_test_{tid.ToString()[..8]}",
                StripeSubscriptionId = $"sub_test_{Guid.NewGuid().ToString()[..8]}",
                TrialEndsAt = status == SubscriptionStatus.Trial ? DateTime.UtcNow.AddDays(30) : null,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };
            db.TenantSubscriptions.Add(sub);
            await db.SaveChangesAsync();
            return sub;
        }
    }

    private TestStripeService GetTestStripeService()
    {
        return (TestStripeService)_factory.Services.GetRequiredService<IStripeService>();
    }
}
