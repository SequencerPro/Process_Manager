using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Tests;

/// <summary>
/// Integration tests for tenant branding (logo, company name, primary color,
/// footer text) covering the authenticated CRUD endpoints, the unauthenticated
/// public endpoint used by the login page, and the response-cache headers that
/// keep the login page from showing stale branding after an admin update.
/// </summary>
public class BrandingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BrandingTests(TestWebApplicationFactory factory) => _factory = factory;

    // ──────────── Authenticated GET ─────────────────────────────────────────

    [Fact]
    public async Task GetBranding_WhenNoRowExists_Returns200WithSyntheticDefault()
    {
        // Use a fresh tenant so we know there's no prior branding row.
        var tenantId = _factory.CreateTenant($"brand-empty-{Guid.NewGuid().ToString()[..6]}", "Empty Co");
        using var client = _factory.CreateTenantClient(tenantId);

        var response = await client.GetAsync("/api/tenant-branding");

        // The pre-fix bug returned 404 here, which made the Branding settings
        // page flash a "Failed to load branding" toast on first visit.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<TenantBrandingResponseDto>(BrandingJson.Options))!;
        Assert.Equal("Empty Co", dto.CompanyName);
        Assert.Null(dto.LogoFileName);
        Assert.Null(dto.PrimaryColorHex);
        Assert.Null(dto.FooterText);
    }

    [Fact]
    public async Task GetBranding_WhenRowExists_ReturnsPersistedValues()
    {
        var tenantId = _factory.CreateTenant($"brand-get-{Guid.NewGuid().ToString()[..6]}", "Get Co");
        using var client = _factory.CreateTenantClient(tenantId);

        await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Persisted Co", "#abcdef", "Some footer"), BrandingJson.Options);

        var dto = await client.GetFromJsonAsync<TenantBrandingResponseDto>(
            "/api/tenant-branding", BrandingJson.Options);

        Assert.NotNull(dto);
        Assert.Equal("Persisted Co", dto!.CompanyName);
        Assert.Equal("#abcdef", dto.PrimaryColorHex);
        Assert.Equal("Some footer", dto.FooterText);
    }

    // ──────────── Authenticated PUT (round-trip) ────────────────────────────

    [Fact]
    public async Task PutBranding_CreatesRow_WhenNoneExists()
    {
        var tenantId = _factory.CreateTenant($"brand-create-{Guid.NewGuid().ToString()[..6]}", "Initial");
        using var client = _factory.CreateTenantClient(tenantId);

        var put = await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Created Co", "#ff0000", null), BrandingJson.Options);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        // Verify it landed in the DB scoped to this tenant.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var row = await db.TenantBrandings.IgnoreQueryFilters()
            .FirstAsync(b => b.TenantId == tenantId);
        Assert.Equal("Created Co", row.CompanyName);
        Assert.Equal("#ff0000", row.PrimaryColorHex);
    }

    [Fact]
    public async Task PutBranding_UpdatesRow_WhenOneExists()
    {
        var tenantId = _factory.CreateTenant($"brand-update-{Guid.NewGuid().ToString()[..6]}", "Initial");
        using var client = _factory.CreateTenantClient(tenantId);

        await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("First Co", "#111111", null), BrandingJson.Options);
        await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Second Co", "#222222", "Footer v2"), BrandingJson.Options);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var rows = await db.TenantBrandings.IgnoreQueryFilters()
            .Where(b => b.TenantId == tenantId).ToListAsync();
        Assert.Single(rows);
        Assert.Equal("Second Co", rows[0].CompanyName);
        Assert.Equal("#222222", rows[0].PrimaryColorHex);
        Assert.Equal("Footer v2", rows[0].FooterText);
    }

    [Fact]
    public async Task PutThenGet_RoundTrip_ReturnsLatestCompanyName()
    {
        var tenantId = _factory.CreateTenant($"brand-rt-{Guid.NewGuid().ToString()[..6]}", "RT Co");
        using var client = _factory.CreateTenantClient(tenantId);

        await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("After Update", null, null), BrandingJson.Options);

        var dto = await client.GetFromJsonAsync<TenantBrandingResponseDto>(
            "/api/tenant-branding", BrandingJson.Options);
        Assert.Equal("After Update", dto!.CompanyName);
    }

    // ──────────── Logo upload / delete ──────────────────────────────────────

    [Fact]
    public async Task UploadLogo_PersistsFileAndSetsLogoFileName()
    {
        var tenantId = _factory.CreateTenant($"brand-up-{Guid.NewGuid().ToString()[..6]}", "Upload Co");
        using var client = _factory.CreateTenantClient(tenantId);

        var dto = await BrandingTestHelpers.UploadTinyPngLogo(client);

        Assert.False(string.IsNullOrWhiteSpace(dto.LogoFileName));
        Assert.EndsWith(".png", dto.LogoFileName);

        var fullPath = BrandingTestHelpers.ResolveLogoPath(_factory, dto.LogoFileName!);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteLogo_RemovesFileAndClearsLogoFileName()
    {
        var tenantId = _factory.CreateTenant($"brand-del-{Guid.NewGuid().ToString()[..6]}", "Delete Co");
        using var client = _factory.CreateTenantClient(tenantId);

        var uploaded = await BrandingTestHelpers.UploadTinyPngLogo(client);
        var fullPath = BrandingTestHelpers.ResolveLogoPath(_factory, uploaded.LogoFileName!);
        Assert.True(File.Exists(fullPath));

        var del = await client.DeleteAsync("/api/tenant-branding/logo");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Row exists but LogoFileName is now null.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var row = await db.TenantBrandings.IgnoreQueryFilters()
            .FirstAsync(b => b.TenantId == tenantId);
        Assert.Null(row.LogoFileName);
        Assert.False(File.Exists(fullPath));
    }

    // ──────────── Public endpoint (login page) ──────────────────────────────

    [Fact]
    public async Task PublicEndpoint_WithMatchingSubdomain_ReturnsTenantBranding()
    {
        var subdomain = $"pub-match-{Guid.NewGuid().ToString()[..6]}";
        var tenantId = _factory.CreateTenant(subdomain, "Subdomain Co");
        using var client = _factory.CreateTenantClient(tenantId);
        await client.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Subdomain Co Branded", "#123456", null), BrandingJson.Options);

        // Unauthenticated client (no JWT).
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            $"/api/tenant-branding/public?subdomain={subdomain}", BrandingJson.Options);

        Assert.Equal("Subdomain Co Branded", dto!.CompanyName);
        Assert.Equal("#123456", dto.PrimaryColorHex);
    }

    /// <summary>
    /// The regression we're fixing: after an admin updates the company name,
    /// a subsequent call to the public endpoint must reflect the change. If
    /// any layer caches, stale data will appear on the login page.
    /// </summary>
    [Fact]
    public async Task PublicEndpoint_AfterPut_ReturnsLatestCompanyName()
    {
        var subdomain = $"pub-fresh-{Guid.NewGuid().ToString()[..6]}";
        var tenantId = _factory.CreateTenant(subdomain, "Initial Name");
        using var admin = _factory.CreateTenantClient(tenantId);

        // Initial save.
        await admin.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("First Name", null, null), BrandingJson.Options);

        using var anon = _factory.CreateClient();
        var firstDto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            $"/api/tenant-branding/public?subdomain={subdomain}", BrandingJson.Options);
        Assert.Equal("First Name", firstDto!.CompanyName);

        // Admin updates the name — even immediately, the next public read
        // must see the new value (this is what the no-cache headers protect).
        await admin.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Renamed Co", null, null), BrandingJson.Options);

        var secondDto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            $"/api/tenant-branding/public?subdomain={subdomain}", BrandingJson.Options);
        Assert.Equal("Renamed Co", secondDto!.CompanyName);
    }

    [Fact]
    public async Task PublicEndpoint_HasNoStoreCacheControlHeader()
    {
        using var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/tenant-branding/public");
        response.EnsureSuccessStatusCode();

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl!.NoStore,
            "Public branding endpoint must set Cache-Control: no-store so the login page never shows stale branding.");
    }

    [Fact]
    public async Task PublicEndpoint_IncludesLogoDataUrl_WhenLogoFileExists()
    {
        var subdomain = $"pub-logo-{Guid.NewGuid().ToString()[..6]}";
        var tenantId = _factory.CreateTenant(subdomain, "Logo Co");
        using var admin = _factory.CreateTenantClient(tenantId);

        await BrandingTestHelpers.UploadTinyPngLogo(admin);

        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            $"/api/tenant-branding/public?subdomain={subdomain}", BrandingJson.Options);

        Assert.False(string.IsNullOrWhiteSpace(dto!.LogoFileName));
        Assert.False(string.IsNullOrWhiteSpace(dto.LogoDataUrl));
        Assert.StartsWith("data:image/png;base64,", dto.LogoDataUrl);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// Public-endpoint fallback tests. These need controlled tenant counts, so
// each scenario uses a custom fixture that runs setup ONCE per test class
// (xUnit reuses the fixture across tests in the class, so the count stays
// stable even as tests run in arbitrary order).
// ────────────────────────────────────────────────────────────────────────────

/// <summary>Fixture: only the seeded "default" sentinel tenant exists.</summary>
public sealed class OnlyDefaultTenantFixture : TestWebApplicationFactory { }

public class BrandingPublicFallback_OnlyDefault : IClassFixture<OnlyDefaultTenantFixture>
{
    private readonly OnlyDefaultTenantFixture _factory;
    public BrandingPublicFallback_OnlyDefault(OnlyDefaultTenantFixture factory) => _factory = factory;

    [Fact]
    public async Task NoSubdomain_OnlyDefaultTenant_ReturnsGenericDefault()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public", BrandingJson.Options);

        // Single-tenant fallback explicitly excludes the seeded "default" tenant,
        // so a DB containing only the sentinel must return generic defaults.
        Assert.Equal("Process Manager", dto!.CompanyName);
        Assert.Null(dto.LogoFileName);
    }

    [Fact]
    public async Task UnknownSubdomain_OnlyDefaultTenant_ReturnsGenericDefault()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public?subdomain=does-not-exist", BrandingJson.Options);

        Assert.Equal("Process Manager", dto!.CompanyName);
    }
}

/// <summary>Fixture: default sentinel + exactly one real tenant.</summary>
public sealed class OneRealTenantFixture : TestWebApplicationFactory
{
    public Guid TheTenantId { get; }
    public const string TheSubdomain = "the-only-one";
    public const string TheName = "The Only Co";

    public OneRealTenantFixture()
    {
        TheTenantId = CreateTenant(TheSubdomain, TheName);
    }
}

public class BrandingPublicFallback_OneRealTenant : IClassFixture<OneRealTenantFixture>
{
    private readonly OneRealTenantFixture _factory;
    public BrandingPublicFallback_OneRealTenant(OneRealTenantFixture factory) => _factory = factory;

    [Fact]
    public async Task NoSubdomain_OneRealTenant_FallsBackToThatTenant()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public", BrandingJson.Options);

        Assert.Equal(OneRealTenantFixture.TheName, dto!.CompanyName);
    }

    [Fact]
    public async Task UnknownSubdomain_OneRealTenant_FallsBackToThatTenant()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public?subdomain=nope", BrandingJson.Options);

        Assert.Equal(OneRealTenantFixture.TheName, dto!.CompanyName);
    }

    [Fact]
    public async Task KnownSubdomain_OneRealTenant_ReturnsThatTenant()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            $"/api/tenant-branding/public?subdomain={OneRealTenantFixture.TheSubdomain}",
            BrandingJson.Options);

        Assert.Equal(OneRealTenantFixture.TheName, dto!.CompanyName);
    }
}

/// <summary>Fixture: default sentinel + multiple real tenants.</summary>
public sealed class MultipleRealTenantsFixture : TestWebApplicationFactory
{
    public MultipleRealTenantsFixture()
    {
        CreateTenant("multi-a", "Tenant A");
        CreateTenant("multi-b", "Tenant B");
    }
}

public class BrandingPublicFallback_MultipleRealTenants : IClassFixture<MultipleRealTenantsFixture>
{
    private readonly MultipleRealTenantsFixture _factory;
    public BrandingPublicFallback_MultipleRealTenants(MultipleRealTenantsFixture factory) => _factory = factory;

    [Fact]
    public async Task NoSubdomain_MultipleRealTenants_ReturnsGenericDefault()
    {
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public", BrandingJson.Options);

        // With multiple real tenants, fallback can't pick one unambiguously.
        Assert.Equal("Process Manager", dto!.CompanyName);
        Assert.Null(dto.LogoFileName);
    }

    [Fact]
    public async Task UnknownSubdomain_DefaultTenantNotConsideredInFallback()
    {
        // Even with two real tenants present, the seeded "default" sentinel
        // must not be returned when an unknown subdomain is asked for.
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public?subdomain=anything", BrandingJson.Options);

        Assert.NotEqual("Default Test Tenant", dto!.CompanyName);
    }
}

/// <summary>
/// Regression fixture: branding is configured against the seeded "default"
/// tenant (the common case when an admin account has no explicit tenant and
/// operates inside the default tenant). No other tenant has branding.
/// </summary>
public sealed class BrandingOnDefaultTenantFixture : TestWebApplicationFactory { }

/// <summary>
/// The bug this covers: an admin uploads a logo / sets a company name while
/// operating in the seeded "default" tenant. The login page's public endpoint
/// previously excluded the "default" tenant from its no-subdomain fallback, so
/// it resolved to a different (unbranded) tenant and the logo never showed.
/// The fix makes the fallback prefer whichever tenant actually has branding.
/// </summary>
public class BrandingPublicFallback_BrandingOnDefaultTenant
    : IClassFixture<BrandingOnDefaultTenantFixture>
{
    private readonly BrandingOnDefaultTenantFixture _factory;
    public BrandingPublicFallback_BrandingOnDefaultTenant(BrandingOnDefaultTenantFixture factory)
        => _factory = factory;

    [Fact]
    public async Task NoSubdomain_BrandingOnDefaultTenant_ReturnsThatBrandingWithLogo()
    {
        // Admin configures branding + logo while scoped to the default tenant.
        using var admin = _factory.CreateTenantClient(TestWebApplicationFactory.DefaultTenantId);
        await admin.PutAsJsonAsync("/api/tenant-branding",
            new UpdateTenantBrandingDto("Default Tenant Brand", "#0a0a0a", null), BrandingJson.Options);
        await BrandingTestHelpers.UploadTinyPngLogo(admin);

        // Login page hits the public endpoint with no subdomain (localhost dev /
        // single-tenant deployment). It must surface the configured branding.
        using var anon = _factory.CreateClient();
        var dto = await anon.GetFromJsonAsync<PublicTenantBrandingDto>(
            "/api/tenant-branding/public", BrandingJson.Options);

        Assert.Equal("Default Tenant Brand", dto!.CompanyName);
        Assert.False(string.IsNullOrWhiteSpace(dto.LogoFileName));
        Assert.StartsWith("data:image/png;base64,", dto.LogoDataUrl);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────

internal static class BrandingJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

internal static class BrandingTestHelpers
{
    /// <summary>Uploads a 1x1 PNG via the logo endpoint. Returns the resulting branding DTO.</summary>
    public static async Task<TenantBrandingResponseDto> UploadTinyPngLogo(HttpClient client)
    {
        // Smallest valid PNG: 1x1 transparent pixel (~67 bytes).
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "logo.png");

        var response = await client.PostAsync("/api/tenant-branding/logo", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantBrandingResponseDto>(BrandingJson.Options))!;
    }

    public static string ResolveLogoPath(TestWebApplicationFactory factory, string fileName)
    {
        using var scope = factory.Services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        return Path.Combine(env.WebRootPath, "uploads", "logos", fileName);
    }
}
