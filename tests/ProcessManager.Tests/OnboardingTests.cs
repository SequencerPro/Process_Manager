using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Integration tests for the M2 Onboarding Wizard: public signup, onboarding state
/// transitions, sample process seeding, and tenant feature-flag management.
/// </summary>
public class OnboardingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OnboardingTests(TestWebApplicationFactory factory) => _factory = factory;

    // ──────────── Public signup ─────────────────────────────────────────────

    [Fact]
    public async Task PublicSignup_CreatesTenantAdminFlagsAndOnboardingState()
    {
        using var client = _factory.CreateClient();
        var subdomain = $"signup-{Guid.NewGuid().ToString()[..8]}";
        var dto = new PublicSignupDto(
            "Signup Test Co", subdomain, OnboardingIndustry.CNC,
            $"admin-{Guid.NewGuid().ToString()[..8]}",
            "admin@signup-test.local",
            "Passw0rd!2024",
            "Sam Admin");

        var response = await client.PostAsJsonAsync("/api/public/signup", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PublicSignupResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(subdomain, result!.Subdomain);
        Assert.NotEqual(Guid.Empty, result.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(result.Token.Token));
        Assert.Equal("Admin", result.Token.Role);

        // Verify the tenant/flags/onboarding state rows exist.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == result.TenantId);
        Assert.Equal(TenantStatus.Trial, tenant.Status);

        var flags = await db.TenantFeatureFlags.IgnoreQueryFilters().FirstAsync(f => f.TenantId == result.TenantId);
        Assert.True(flags.ShowQualityTools);
        Assert.False(flags.ShowAdvancedModules);
        Assert.False(flags.ShowWarehouseTools);

        var state = await db.TenantOnboardingStates.IgnoreQueryFilters().FirstAsync(s => s.TenantId == result.TenantId);
        Assert.Equal(OnboardingIndustry.CNC, state.Industry);
        Assert.Equal(0, state.CurrentStep);
        Assert.Null(state.CompletedAt);

        var vocab = await db.DomainVocabularies.IgnoreQueryFilters().FirstAsync(v => v.TenantId == result.TenantId);
        Assert.True(vocab.IsActive);
        Assert.Equal("CNC Machining", vocab.Name);
    }

    [Fact]
    public async Task PublicSignup_DuplicateSubdomain_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var subdomain = $"dup-{Guid.NewGuid().ToString()[..8]}";
        var first = new PublicSignupDto("First", subdomain, OnboardingIndustry.General,
            $"u1-{Guid.NewGuid().ToString()[..6]}", "u1@test.local", "Passw0rd!2024", null);
        var resp1 = await client.PostAsJsonAsync("/api/public/signup", first, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var second = new PublicSignupDto("Second", subdomain, OnboardingIndustry.General,
            $"u2-{Guid.NewGuid().ToString()[..6]}", "u2@test.local", "Passw0rd!2024", null);
        var resp2 = await client.PostAsJsonAsync("/api/public/signup", second, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task PublicSignup_InvalidSubdomain_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var dto = new PublicSignupDto("Bad", "NOT-valid!", OnboardingIndustry.General,
            "admin", "admin@test.local", "Passw0rd!2024", null);
        var resp = await client.PostAsJsonAsync("/api/public/signup", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PublicSignup_WeakPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var dto = new PublicSignupDto("Weak", $"weak-{Guid.NewGuid().ToString()[..8]}", OnboardingIndustry.General,
            "admin-weak", "weak@test.local", "short", null);
        var resp = await client.PostAsJsonAsync("/api/public/signup", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PublicSignup_IsAnonymous()
    {
        // Even without any bearer token the endpoint must succeed.
        using var anon = _factory.CreateClient();
        var dto = new PublicSignupDto("Anon", $"anon-{Guid.NewGuid().ToString()[..8]}", OnboardingIndustry.Medical,
            $"anon-{Guid.NewGuid().ToString()[..6]}", "anon@test.local", "Passw0rd!2024", null);
        var resp = await anon.PostAsJsonAsync("/api/public/signup", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ──────────── Onboarding state lifecycle ───────────────────────────────

    [Fact]
    public async Task GetOnboardingState_LazyCreatesForLegacyTenant()
    {
        // Use the default tenant (pre-existing; has no TenantOnboardingState row).
        var client = _factory.CreateAuthenticatedClient();
        var resp = await client.GetAsync("/api/onboarding/state");
        resp.EnsureSuccessStatusCode();
        var state = await resp.Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);
        Assert.NotNull(state);
        // Legacy tenants are marked completed — they should not be pushed into the wizard.
        Assert.True(state!.IsCompleted);
    }

    [Fact]
    public async Task PatchOnboardingState_AdvancesStep()
    {
        var tenant = _factory.CreateTenant($"obstep-{Guid.NewGuid().ToString()[..6]}");
        // Provision the state row directly so this test is independent of signup.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                Industry = OnboardingIndustry.General,
                CurrentStep = 0,
                SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateTenantClient(tenant);
        var update = new UpdateOnboardingStepDto(2, null, null, null, null);
        var resp = await client.PatchAsJsonAsync("/api/onboarding/state", update, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var state = await resp.Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);
        Assert.Equal(2, state!.CurrentStep);
        Assert.False(state.IsCompleted);
    }

    [Fact]
    public async Task PatchOnboardingState_Step5_MarksCompleted()
    {
        var tenant = _factory.CreateTenant($"obfinish-{Guid.NewGuid().ToString()[..6]}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenant,
                Industry = OnboardingIndustry.General, CurrentStep = 0, SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateTenantClient(tenant);
        var update = new UpdateOnboardingStepDto(5, null, null, null, null);
        var resp = await client.PatchAsJsonAsync("/api/onboarding/state", update, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var state = await resp.Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);
        Assert.True(state!.IsCompleted);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public async Task PatchOnboardingState_InvalidStep_ReturnsBadRequest()
    {
        var tenant = _factory.CreateTenant($"oobound-{Guid.NewGuid().ToString()[..6]}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenant,
                Industry = OnboardingIndustry.General, CurrentStep = 0, SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateTenantClient(tenant);
        var resp = await client.PatchAsJsonAsync("/api/onboarding/state",
            new UpdateOnboardingStepDto(99, null, null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ──────────── Skip + sample seeding ────────────────────────────────────

    [Fact]
    public async Task SkipOnboarding_WithSample_SeedsProcessAndMarksCompleted()
    {
        var tenant = _factory.CreateTenant($"obskip-{Guid.NewGuid().ToString()[..6]}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenant,
                Industry = OnboardingIndustry.PCBA, CurrentStep = 0, SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateTenantClient(tenant);
        var resp = await client.PostAsJsonAsync("/api/onboarding/skip",
            new SkipOnboardingDto(true), JsonOptions);
        resp.EnsureSuccessStatusCode();
        var state = await resp.Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);
        Assert.True(state!.IsCompleted);
        Assert.True(state.IsSkipped);
        Assert.NotNull(state.FirstKindId);
        Assert.NotNull(state.FirstStepTemplateId);
        Assert.NotNull(state.FirstProcessId);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var kind = await db2.Kinds.IgnoreQueryFilters().FirstAsync(k => k.Id == state.FirstKindId);
        Assert.Equal(tenant, kind.TenantId);
        // PCBA code prefix verification.
        Assert.StartsWith("SAMPLE-PCB", kind.Code);
        var proc = await db2.Processes.IgnoreQueryFilters()
            .Include(p => p.ProcessSteps)
            .FirstAsync(p => p.Id == state.FirstProcessId);
        Assert.Equal(ProcessStatus.Released, proc.Status);
        Assert.Single(proc.ProcessSteps);
    }

    [Fact]
    public async Task SeedSample_IsIndustrySpecific()
    {
        var tenant = _factory.CreateTenant($"obind-{Guid.NewGuid().ToString()[..6]}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenant,
                Industry = OnboardingIndustry.Medical, CurrentStep = 0, SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateTenantClient(tenant);
        var resp = await client.PostAsync("/api/onboarding/seed-sample", null);
        resp.EnsureSuccessStatusCode();
        var state = await resp.Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var kind = await db2.Kinds.IgnoreQueryFilters().FirstAsync(k => k.Id == state!.FirstKindId);
        Assert.StartsWith("SAMPLE-DEV", kind.Code);
    }

    // ──────────── Feature-flag CRUD ────────────────────────────────────────

    [Fact]
    public async Task GetFeatureFlags_LazyCreatesForLegacyTenant()
    {
        // Default tenant had no flags row until this lazy create.
        var client = _factory.CreateAuthenticatedClient();
        var resp = await client.GetAsync("/api/onboarding/feature-flags");
        resp.EnsureSuccessStatusCode();
        var flags = await resp.Content.ReadFromJsonAsync<TenantFeatureFlagsDto>(JsonOptions);
        Assert.NotNull(flags);
        // Legacy tenants see the full UI — we must not silently hide existing surface area.
        Assert.True(flags!.ShowAdvancedModules);
        Assert.True(flags.ShowQualityTools);
        Assert.True(flags.ShowProductionTools);
    }

    [Fact]
    public async Task UpdateFeatureFlags_PersistsChanges()
    {
        var tenant = _factory.CreateTenant($"flags-{Guid.NewGuid().ToString()[..6]}");
        var client = _factory.CreateTenantClient(tenant);

        var updated = new TenantFeatureFlagsDto(
            ShowAdvancedModules: false,
            ShowQualityTools: true,
            ShowProductionTools: false,
            ShowWarehouseTools: true,
            ShowTrainingTools: false);

        var resp = await client.PutAsJsonAsync("/api/onboarding/feature-flags", updated, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var echoed = await resp.Content.ReadFromJsonAsync<TenantFeatureFlagsDto>(JsonOptions);
        Assert.False(echoed!.ShowAdvancedModules);
        Assert.True(echoed.ShowWarehouseTools);

        // Re-read and confirm persistence.
        var reread = await client.GetAsync("/api/onboarding/feature-flags");
        var flags = await reread.Content.ReadFromJsonAsync<TenantFeatureFlagsDto>(JsonOptions);
        Assert.False(flags!.ShowAdvancedModules);
        Assert.False(flags.ShowProductionTools);
        Assert.True(flags.ShowWarehouseTools);
        Assert.False(flags.ShowTrainingTools);
    }

    [Fact]
    public async Task UpdateFeatureFlags_NonAdmin_IsForbidden()
    {
        var tenant = _factory.CreateTenant($"flags-ro-{Guid.NewGuid().ToString()[..6]}");
        var client = _factory.CreateTenantClient(tenant, role: "Engineer");
        var payload = new TenantFeatureFlagsDto(false, false, false, false, false);
        var resp = await client.PutAsJsonAsync("/api/onboarding/feature-flags", payload, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ──────────── Industry list endpoint (anonymous) ───────────────────────

    [Fact]
    public async Task GetIndustries_IsAnonymous_AndReturnsAllFour()
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/onboarding/industries");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<OnboardingIndustryOptionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(4, list!.Count);
        Assert.Contains(list, i => i.Value == OnboardingIndustry.CNC);
        Assert.Contains(list, i => i.Value == OnboardingIndustry.PCBA);
        Assert.Contains(list, i => i.Value == OnboardingIndustry.Medical);
        Assert.Contains(list, i => i.Value == OnboardingIndustry.General);
    }

    // ──────────── Tenant isolation for onboarding state ────────────────────

    [Fact]
    public async Task OnboardingState_IsIsolatedBetweenTenants()
    {
        var tenantA = _factory.CreateTenant($"obiso-a-{Guid.NewGuid().ToString()[..6]}");
        var tenantB = _factory.CreateTenant($"obiso-b-{Guid.NewGuid().ToString()[..6]}");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenantA,
                Industry = OnboardingIndustry.CNC, CurrentStep = 3, SignupAt = DateTime.UtcNow
            });
            db.TenantOnboardingStates.Add(new TenantOnboardingState
            {
                Id = Guid.NewGuid(), TenantId = tenantB,
                Industry = OnboardingIndustry.PCBA, CurrentStep = 1, SignupAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var clientA = _factory.CreateTenantClient(tenantA);
        var clientB = _factory.CreateTenantClient(tenantB);

        var stateA = await (await clientA.GetAsync("/api/onboarding/state"))
            .Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);
        var stateB = await (await clientB.GetAsync("/api/onboarding/state"))
            .Content.ReadFromJsonAsync<OnboardingStateDto>(JsonOptions);

        Assert.Equal(OnboardingIndustry.CNC,  stateA!.Industry);
        Assert.Equal(3, stateA.CurrentStep);
        Assert.Equal(OnboardingIndustry.PCBA, stateB!.Industry);
        Assert.Equal(1, stateB.CurrentStep);
    }
}
