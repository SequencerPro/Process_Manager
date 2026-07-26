using System.IdentityModel.Tokens.Jwt;
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

/// <summary>
/// Integration tests for M1 multi-tenant isolation. Verifies that tenants cannot
/// read or write each other's data, that the SaveChanges interceptor stamps
/// TenantId, that platform-admin endpoints are properly gated, and that JWTs
/// carry the tenant_id claim end-to-end.
/// </summary>
public class MultiTenancyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public MultiTenancyTests(TestWebApplicationFactory factory) => _factory = factory;

    // ──────────── JWT claim shape ─────────────────────────────────────────────

    [Fact]
    public void GeneratedJwt_ContainsTenantIdClaim()
    {
        var token = TestWebApplicationFactory.GenerateAdminJwt();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id");
        Assert.NotNull(tenantClaim);
        Assert.Equal(TestWebApplicationFactory.DefaultTenantId.ToString(), tenantClaim!.Value);
    }

    [Fact]
    public void PlatformAdminJwt_ContainsPlatformAdminClaim()
    {
        var token = TestWebApplicationFactory.GeneratePlatformAdminJwt();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "platform_admin" && c.Value == "true");
    }

    // ──────────── Interceptor stamps TenantId on insert ──────────────────────

    [Fact]
    public async Task CreateKind_StampsCurrentTenantId()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new KindCreateDto($"STAMP-{Guid.NewGuid().ToString()[..6]}", "Stamp Test", null, false, false);
        var response = await client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var kind = (await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var dbKind = await db.Kinds.IgnoreQueryFilters().FirstAsync(k => k.Id == kind.Id);
        Assert.Equal(TestWebApplicationFactory.DefaultTenantId, dbKind.TenantId);
    }

    // ──────────── Cross-tenant read isolation ────────────────────────────────

    [Fact]
    public async Task TenantB_CannotSeeTenantA_Kinds()
    {
        var tenantB = _factory.CreateTenant($"tenant-b-{Guid.NewGuid().ToString()[..6]}");

        // Tenant A (default) creates a kind
        var clientA = _factory.CreateAuthenticatedClient();
        var codeA = $"ISO-A-{Guid.NewGuid().ToString()[..6]}";
        await clientA.PostAsJsonAsync("/api/kinds",
            new KindCreateDto(codeA, "A Kind", null, false, false),
            JsonOptions);

        // Tenant B listing kinds should NOT see tenant A's kind
        var clientB = _factory.CreateTenantClient(tenantB);
        var listResponse = await clientB.GetAsync("/api/kinds");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<KindResponseDto>>(JsonOptions);

        Assert.NotNull(page);
        Assert.DoesNotContain(page!.Items, k => k.Code == codeA);
    }

    [Fact]
    public async Task TenantB_CannotReadTenantA_KindById()
    {
        var tenantB = _factory.CreateTenant($"tenant-b-{Guid.NewGuid().ToString()[..6]}");

        var clientA = _factory.CreateAuthenticatedClient();
        var createResp = await clientA.PostAsJsonAsync("/api/kinds",
            new KindCreateDto($"XREAD-{Guid.NewGuid().ToString()[..6]}", "Cross-read Test", null, false, false),
            JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var kindA = (await createResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;

        var clientB = _factory.CreateTenantClient(tenantB);
        var readResp = await clientB.GetAsync($"/api/kinds/{kindA.Id}");
        Assert.Equal(HttpStatusCode.NotFound, readResp.StatusCode);
    }

    // ──────────── Cross-tenant write isolation ────────────────────────────────

    [Fact]
    public async Task TenantB_CannotDeleteTenantA_Kind()
    {
        var tenantB = _factory.CreateTenant($"tenant-b-{Guid.NewGuid().ToString()[..6]}");

        var clientA = _factory.CreateAuthenticatedClient();
        var createResp = await clientA.PostAsJsonAsync("/api/kinds",
            new KindCreateDto($"XDEL-{Guid.NewGuid().ToString()[..6]}", "Cross-delete Test", null, false, false),
            JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var kindA = (await createResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;

        var clientB = _factory.CreateTenantClient(tenantB);
        var deleteResp = await clientB.DeleteAsync($"/api/kinds/{kindA.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResp.StatusCode);

        // Verify it still exists for tenant A
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        Assert.True(await db.Kinds.IgnoreQueryFilters().AnyAsync(k => k.Id == kindA.Id));
    }

    // ──────────── Platform admin bypass ────────────────────────────────────────

    [Fact]
    public async Task PlatformAdmin_CanListAllTenants()
    {
        var platformClient = _factory.CreatePlatformAdminClient();
        var response = await platformClient.GetAsync("/api/platform/tenants");
        response.EnsureSuccessStatusCode();

        var tenants = await response.Content.ReadFromJsonAsync<List<TenantResponseDto>>(JsonOptions);
        Assert.NotNull(tenants);
        Assert.Contains(tenants!, t => t.Id == TestWebApplicationFactory.DefaultTenantId);
    }

    [Fact]
    public async Task NonPlatformAdmin_CannotAccessPlatformTenantsEndpoint()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/platform/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotAccessPlatformTenantsEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/platform/tenants");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ──────────── Tenant provisioning API ─────────────────────────────────────

    [Fact]
    public async Task PlatformAdmin_CanProvisionNewTenant()
    {
        var platformClient = _factory.CreatePlatformAdminClient();
        var subdomain = $"prov-{Guid.NewGuid().ToString()[..8]}";
        var response = await platformClient.PostAsJsonAsync("/api/platform/tenants",
            new CreateTenantDto(subdomain, "Provisioned Tenant"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TenantResponseDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(subdomain, created!.Subdomain);
        Assert.Equal(TenantStatus.Trial, created.Status);
    }

    [Fact]
    public async Task CreateTenant_DuplicateSubdomain_ReturnsConflict()
    {
        var platformClient = _factory.CreatePlatformAdminClient();
        var subdomain = $"dup-{Guid.NewGuid().ToString()[..8]}";

        var first = await platformClient.PostAsJsonAsync("/api/platform/tenants",
            new CreateTenantDto(subdomain, "First"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await platformClient.PostAsJsonAsync("/api/platform/tenants",
            new CreateTenantDto(subdomain, "Second"), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task UpdateTenantStatus_ChangesStatus()
    {
        var platformClient = _factory.CreatePlatformAdminClient();
        var createResp = await platformClient.PostAsJsonAsync("/api/platform/tenants",
            new CreateTenantDto($"stat-{Guid.NewGuid().ToString()[..8]}", "Status Test"),
            JsonOptions);
        var created = (await createResp.Content.ReadFromJsonAsync<TenantResponseDto>(JsonOptions))!;

        var patchResp = await platformClient.PatchAsJsonAsync($"/api/platform/tenants/{created.Id}/status",
            new UpdateTenantStatusDto(TenantStatus.Suspended), JsonOptions);
        patchResp.EnsureSuccessStatusCode();

        var updated = await patchResp.Content.ReadFromJsonAsync<TenantResponseDto>(JsonOptions);
        Assert.Equal(TenantStatus.Suspended, updated!.Status);
    }

    // ──────────── Interceptor defence-in-depth ────────────────────────────────

    [Fact]
    public async Task Interceptor_BlocksCrossTenantUpdate_WhenQueryFilterBypassed()
    {
        var tenantB = _factory.CreateTenant($"tenant-b-{Guid.NewGuid().ToString()[..6]}");

        var clientA = _factory.CreateAuthenticatedClient();
        var createResp = await clientA.PostAsJsonAsync("/api/kinds",
            new KindCreateDto($"XINT-{Guid.NewGuid().ToString()[..6]}", "Interceptor Test", null, false, false),
            JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var kindA = (await createResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;

        // Simulate a write attempt against tenant A's row while running as tenant B.
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        ctx.SetTenant(tenantB, isPlatformAdmin: false);

        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var row = await db.Kinds.IgnoreQueryFilters().FirstAsync(k => k.Id == kindA.Id);
        row.Name = "Hacked";

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    // ──────────── TenantId immutability on update ────────────────────────────

    [Fact]
    public async Task Interceptor_IgnoresTenantIdChange_OnUpdate()
    {
        var clientA = _factory.CreateAuthenticatedClient();
        var createResp = await clientA.PostAsJsonAsync("/api/kinds",
            new KindCreateDto($"IMM-{Guid.NewGuid().ToString()[..6]}", "Immutability Test", null, false, false),
            JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var kind = (await createResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        ctx.SetTenant(TestWebApplicationFactory.DefaultTenantId, isPlatformAdmin: false);
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var row = await db.Kinds.FirstAsync(k => k.Id == kind.Id);

        var attackerTenant = Guid.NewGuid();
        row.TenantId = attackerTenant;
        row.Name = "Rename";
        await db.SaveChangesAsync();

        var reread = await db.Kinds.IgnoreQueryFilters().AsNoTracking().FirstAsync(k => k.Id == kind.Id);
        Assert.Equal(TestWebApplicationFactory.DefaultTenantId, reread.TenantId);
        Assert.Equal("Rename", reread.Name);
    }
}
