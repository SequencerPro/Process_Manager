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
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class PlanEnforcementTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PlanEnforcementTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Trial tenant blocked from creating 4th user ─────────────────────────

    [Fact]
    public async Task TrialTenant_BlockedFromCreating4thUser()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedUsers(3);

        var dto = new RegisterRequestDto(
            $"user4-{Guid.NewGuid():N}"[..20], $"u4-{Guid.NewGuid():N}@test.local",
            "Password123!", "Engineer", "User Four");

        var resp = await _client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        Assert.Equal((HttpStatusCode)402, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Plan limit reached", body);
        Assert.Contains("3-user limit", body);
    }

    // ── Starter tenant can create users beyond Trial limit ─────────────────

    [Fact]
    public async Task StarterTenant_CanCreateUsersAboveTrialLimit()
    {
        // Starter allows 25 users — just verify we can create one beyond the 3-user Trial limit
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);

        var dto = new RegisterRequestDto(
            $"starter-{Guid.NewGuid():N}"[..20], $"st-{Guid.NewGuid():N}@test.local",
            "Password123!", "Engineer", "Starter User");

        var resp = await _client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── Trial tenant blocked from creating 2nd process ──────────────────────

    [Fact]
    public async Task TrialTenant_BlockedFromCreating2ndProcess()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedProcess("EXISTING-PROC");

        var dto = new ProcessCreateDto($"PROC2-{Guid.NewGuid():N}"[..12], "Second Process", null);
        var resp = await _client.PostAsJsonAsync("/api/processes", dto, JsonOptions);
        Assert.Equal((HttpStatusCode)402, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("1-process limit", body);
    }

    // ── Starter tenant unlimited processes ───────────────────────────────────

    [Fact]
    public async Task StarterTenant_UnlimitedProcesses()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);
        await SeedProcess("STARTER-PROC");

        var dto = new ProcessCreateDto($"PROC-{Guid.NewGuid():N}"[..12], "Another Process", null);
        var resp = await _client.PostAsJsonAsync("/api/processes", dto, JsonOptions);
        Assert.True(resp.IsSuccessStatusCode, $"Expected success but got {resp.StatusCode}");
    }

    // ── Professional tenant auto-enables advanced modules ────────────────────

    [Fact]
    public async Task ProfessionalPlan_AutoEnablesAdvancedModules()
    {
        await SeedSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active);
        await SeedFeatureFlags(showAdvancedModules: false);

        var resp = await _client.PostAsync("/api/billing/sync-features", null);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var flags = await db.TenantFeatureFlags.FirstOrDefaultAsync();

        Assert.NotNull(flags);
        Assert.True(flags.ShowAdvancedModules);
        Assert.True(flags.ShowProductionTools);
        Assert.True(flags.ShowWarehouseTools);
        Assert.True(flags.ShowTrainingTools);
    }

    // ── Usage metering increments on job completion ──────────────────────────

    [Fact]
    public async Task UsageMetrics_IncrementedOnJobCompletion()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);
        var jobId = await SeedAndStartJob();

        // Complete all step executions first
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var steps = await db.StepExecutions.Where(se => se.JobId == jobId).ToListAsync();
            foreach (var step in steps)
            {
                step.Status = StepExecutionStatus.Completed;
                step.CompletedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        var completeResp = await _client.PostAsync($"/api/jobs/{jobId}/complete", null);
        Assert.True(completeResp.IsSuccessStatusCode, $"Job complete failed: {await completeResp.Content.ReadAsStringAsync()}");

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var metric = await db2.UsageMetrics
            .FirstOrDefaultAsync(m =>
                m.MetricType == UsageMetricType.JobExecutions &&
                m.PeriodStart == periodStart);

        Assert.NotNull(metric);
        Assert.True(metric.Count >= 1);
    }

    // ── Upgrade prompt returned in 402 response body ─────────────────────────

    [Fact]
    public async Task UpgradePrompt_ReturnedIn402ResponseBody()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedUsers(3);

        var dto = new RegisterRequestDto(
            $"blocked-{Guid.NewGuid():N}"[..20], $"bl-{Guid.NewGuid():N}@test.local",
            "Password123!", "Participant", null);

        var resp = await _client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        Assert.Equal((HttpStatusCode)402, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Starter", body);
    }

    // ── Enterprise plan has no limits ────────────────────────────────────────

    [Fact]
    public async Task EnterprisePlan_HasNoLimits()
    {
        await SeedSubscription(SubscriptionPlan.Enterprise, SubscriptionStatus.Active);

        var resp = await _client.GetAsync("/api/billing/plan");
        resp.EnsureSuccessStatusCode();

        var summary = await resp.Content.ReadFromJsonAsync<PlanUsageSummaryDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(SubscriptionPlan.Enterprise, summary.Plan);
        Assert.Null(summary.Limits.MaxUsers);
        Assert.Null(summary.Limits.MaxProcesses);
        Assert.Null(summary.Limits.MaxMonthlyExecutions);
        Assert.True(summary.Limits.AdvancedModulesEnabled);
    }

    // ── Plan check endpoint returns correct result ───────────────────────────

    [Fact]
    public async Task PlanCheckEndpoint_ReturnsResultForEnterprise()
    {
        await SeedSubscription(SubscriptionPlan.Enterprise, SubscriptionStatus.Active);

        var resp = await _client.GetAsync("/api/billing/plan/check/Users");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PlanCheckResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(PlanCheckOutcome.Allowed, result.Outcome);
        Assert.Null(result.Limit);
    }

    // ── Plan change propagates feature flags ─────────────────────────────────

    [Fact]
    public async Task PlanChange_PropagatesFeatureFlags()
    {
        // Start as Trial with no advanced modules
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);
        await SeedFeatureFlags(showAdvancedModules: false);

        // "Upgrade" to Professional
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var sub = await db.TenantSubscriptions.FirstOrDefaultAsync();
            if (sub is not null)
            {
                sub.PlanCode = SubscriptionPlan.Professional;
                sub.Status = SubscriptionStatus.Active;
                await db.SaveChangesAsync();
            }
        }

        var resp = await _client.PostAsync("/api/billing/sync-features", null);
        resp.EnsureSuccessStatusCode();

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var flags = await db2.TenantFeatureFlags.FirstOrDefaultAsync();

        Assert.NotNull(flags);
        Assert.True(flags.ShowAdvancedModules);
    }

    // ── Trial tenant blocked from exceeding monthly execution limit ──────────

    [Fact]
    public async Task TrialTenant_BlockedFromExceedingMonthlyExecutions()
    {
        await SeedSubscription(SubscriptionPlan.Trial, SubscriptionStatus.Trial);

        // Seed 50 job executions for this month
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
            {
                var existing = await db.UsageMetrics
                    .FirstOrDefaultAsync(m =>
                        m.MetricType == UsageMetricType.JobExecutions &&
                        m.PeriodStart == periodStart);
                if (existing is not null)
                {
                    existing.Count = 50;
                }
                else
                {
                    db.UsageMetrics.Add(new UsageMetric
                    {
                        Id = Guid.NewGuid(),
                        TenantId = TestWebApplicationFactory.DefaultTenantId,
                        MetricType = UsageMetricType.JobExecutions,
                        Count = 50,
                        PeriodStart = periodStart,
                        PeriodEnd = periodStart.AddMonths(1)
                    });
                }
                await db.SaveChangesAsync();
            }
        }

        // Attempt to create a job — should be blocked
        var processId = await SeedReleasedProcess("EXEC-LIMIT-PROC");
        var dto = new { Code = $"JOB-LIM-{Guid.NewGuid():N}"[..12], Name = "Limit Test Job", ProcessId = processId };
        var resp = await _client.PostAsJsonAsync("/api/jobs", dto, JsonOptions);
        Assert.Equal((HttpStatusCode)402, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("50 monthly execution limit", body);
    }

    // ── PDF export increments usage metric ───────────────────────────────────

    [Fact]
    public async Task PdfExport_IncrementsUsageMetric()
    {
        await SeedSubscription(SubscriptionPlan.Starter, SubscriptionStatus.Active);
        var pfmeaId = await SeedPfmea();

        var resp = await _client.GetAsync($"/api/pfmeas/{pfmeaId}/pdf");
        Assert.True(resp.IsSuccessStatusCode, $"PDF export failed: {resp.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var metric = await db.UsageMetrics
            .FirstOrDefaultAsync(m =>
                m.MetricType == UsageMetricType.PdfExports &&
                m.PeriodStart == periodStart);

        Assert.NotNull(metric);
        Assert.True(metric.Count >= 1);
    }

    // ── Billing plan endpoint requires auth ──────────────────────────────────

    [Fact]
    public async Task BillingPlan_RequiresAuth()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/billing/plan");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-tenant plan isolation ──────────────────────────────────────────

    [Fact]
    public async Task CrossTenant_PlanIsolation()
    {
        var tenantA = _factory.CreateTenant("plan-tenant-a");
        var tenantB = _factory.CreateTenant("plan-tenant-b");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(tenantA))
        {
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                PlanCode = SubscriptionPlan.Enterprise,
                Status = SubscriptionStatus.Active
            });
            await db.SaveChangesAsync();
        }

        using (tenantContext.BeginScope(tenantB))
        {
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                PlanCode = SubscriptionPlan.Trial,
                Status = SubscriptionStatus.Trial,
                TrialEndsAt = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }

        var clientA = _factory.CreateTenantClient(tenantA);
        var clientB = _factory.CreateTenantClient(tenantB);

        var respA = await clientA.GetAsync("/api/billing/plan");
        respA.EnsureSuccessStatusCode();
        var summaryA = await respA.Content.ReadFromJsonAsync<PlanUsageSummaryDto>(JsonOptions);

        var respB = await clientB.GetAsync("/api/billing/plan");
        respB.EnsureSuccessStatusCode();
        var summaryB = await respB.Content.ReadFromJsonAsync<PlanUsageSummaryDto>(JsonOptions);

        Assert.Equal(SubscriptionPlan.Enterprise, summaryA!.Plan);
        Assert.Equal(SubscriptionPlan.Trial, summaryB!.Plan);
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
                existing.TrialEndsAt = status == SubscriptionStatus.Trial ? DateTime.UtcNow.AddDays(30) : null;
                await db.SaveChangesAsync();
                return;
            }

            db.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = TestWebApplicationFactory.DefaultTenantId,
                PlanCode = plan,
                Status = status,
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
                $"planuser-{pfx}", $"pu-{pfx}@test.local",
                "Password123!", "Participant", $"Plan User {i}");
            var resp = await _client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
            // If the user limit is reached, that's fine for seeding
        }
    }

    private async Task SeedProcess(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            if (!await db.Processes.AnyAsync(p => p.Code == code))
            {
                db.Processes.Add(new ProcessManager.Domain.Entities.Process
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = code,
                    IsActive = true,
                    IsSystemContent = false,
                    ProcessRole = ProcessManager.Domain.Enums.ProcessRole.ManufacturingProcess,
                    Status = ProcessManager.Domain.Enums.ProcessStatus.Draft
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private async Task<Guid> SeedReleasedProcess(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            var existing = await db.Processes.FirstOrDefaultAsync(p => p.Code == code);
            if (existing is not null) return existing.Id;

            var process = new ProcessManager.Domain.Entities.Process
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code,
                IsActive = true,
                IsSystemContent = false,
                ProcessRole = ProcessManager.Domain.Enums.ProcessRole.ManufacturingProcess,
                Status = ProcessManager.Domain.Enums.ProcessStatus.Released,
                Version = 1
            };
            db.Processes.Add(process);
            await db.SaveChangesAsync();
            return process.Id;
        }
    }

    private async Task<Guid> SeedAndStartJob()
    {
        var processId = await SeedReleasedProcess($"METER-PROC-{Guid.NewGuid():N}"[..12]);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            var jobCode = $"METER-JOB-{Guid.NewGuid():N}"[..12];
            var job = new Job
            {
                Id = Guid.NewGuid(),
                Code = jobCode,
                Name = "Metering Test Job",
                ProcessId = processId,
                Status = JobStatus.InProgress,
                ProcessVersion = 1,
                StartedAt = DateTime.UtcNow
            };
            db.Jobs.Add(job);
            await db.SaveChangesAsync();
            return job.Id;
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

    private async Task<Guid> SeedPfmea()
    {
        var processId = await SeedReleasedProcess($"PFMEA-PROC-{Guid.NewGuid():N}"[..12]);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        using (tenantContext.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            var pfmea = new Pfmea
            {
                Id = Guid.NewGuid(),
                Code = $"PFMEA-{Guid.NewGuid():N}"[..12],
                Name = "Test PFMEA",
                ProcessId = processId,
                IsStale = true
            };
            db.Pfmeas.Add(pfmea);
            await db.SaveChangesAsync();
            return pfmea.Id;
        }
    }
}
