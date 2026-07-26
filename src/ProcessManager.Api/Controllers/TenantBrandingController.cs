using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/tenant-branding")]
public class TenantBrandingController : ControllerBase
{
    // Maximum logo upload size. Kept in sync with:
    //  • UI hint in Components/Pages/Settings/Branding.razor ("max 5 MB")
    //  • ApiClient.UploadTenantLogoAsync stream max-allowed-size
    private const long MaxLogoBytes = 5 * 1024 * 1024;

    private readonly ProcessManagerDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ITenantContext _tenantContext;

    public TenantBrandingController(
        ProcessManagerDbContext db,
        IWebHostEnvironment env,
        ITenantContext tenantContext)
    {
        _db = db;
        _env = env;
        _tenantContext = tenantContext;
    }

    // Cap on inline base64 logo. Real company logos are tiny (typically a few KB);
    // anything bigger would just bloat the login page render.
    private const long MaxInlineLogoBytes = 1 * 1024 * 1024;

    /// <summary>
    /// Public, unauthenticated endpoint returning a minimal subset of branding
    /// for the tenant identified by <paramref name="subdomain"/>. Used by the
    /// login page to display the tenant's logo before sign-in.
    ///
    /// Resolution order:
    ///   1. If <paramref name="subdomain"/> matches a tenant, use that tenant.
    ///   2. Otherwise (no subdomain provided, or no match — e.g. localhost dev
    ///      and single-tenant production deployments), if exactly one tenant
    ///      exists in the database, use it.
    ///   3. Otherwise return generic defaults so the page still renders.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<PublicTenantBrandingDto>> GetPublic([FromQuery] string? subdomain)
    {
        var tenant = await ResolvePublicTenantAsync(subdomain);
        if (tenant is null)
            return new PublicTenantBrandingDto(null, null, null, "Process Manager");

        // TenantBranding has a global tenant query filter, so scope the lookup
        // explicitly via the tenant context rather than relying on the JWT.
        using var scope = _tenantContext.BeginScope(tenant.Id, isPlatformAdmin: false);
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();

        var logoDataUrl = await BuildLogoDataUrlAsync(branding?.LogoFileName);

        return new PublicTenantBrandingDto(
            LogoFileName: branding?.LogoFileName,
            LogoDataUrl: logoDataUrl,
            PrimaryColorHex: branding?.PrimaryColorHex,
            CompanyName: branding?.CompanyName ?? tenant.Name);
    }

    private async Task<string?> BuildLogoDataUrlAsync(string? logoFileName)
    {
        if (string.IsNullOrWhiteSpace(logoFileName)) return null;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "uploads", "logos", logoFileName);
            if (!System.IO.File.Exists(path)) return null;

            var info = new FileInfo(path);
            if (info.Length > MaxInlineLogoBytes) return null;

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            var ext = Path.GetExtension(logoFileName).ToLowerInvariant();
            var mime = ext switch
            {
                ".png"            => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg"            => "image/svg+xml",
                _                 => "application/octet-stream"
            };
            return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            // Best-effort — never fail the public endpoint over logo IO.
            return null;
        }
    }

    private async Task<TenantLookupResult?> ResolvePublicTenantAsync(string? subdomain)
    {
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            var normalized = subdomain.Trim().ToLowerInvariant();
            var bySubdomain = await _db.Tenants
                .Where(t => t.Subdomain == normalized)
                .Select(t => new TenantLookupResult(t.Id, t.Name))
                .FirstOrDefaultAsync();
            if (bySubdomain is not null) return bySubdomain;
        }

        // No subdomain match — typical for single-tenant deployments and
        // localhost dev where the request URL carries no tenant subdomain.
        // Prefer the tenant that actually has branding configured: when exactly
        // one tenant in the database has a TenantBranding row, that's
        // unambiguously the brand to show on the login page. This also handles
        // deployments where the admin operates inside the seeded "default"
        // tenant (which the real-tenant fallback below deliberately skips).
        var brandedTenantIds = await _db.TenantBrandings
            .IgnoreQueryFilters()
            .Select(b => b.TenantId)
            .Distinct()
            .Take(2)
            .ToListAsync();
        if (brandedTenantIds.Count == 1)
        {
            var branded = await _db.Tenants
                .Where(t => t.Id == brandedTenantIds[0])
                .Select(t => new TenantLookupResult(t.Id, t.Name))
                .FirstOrDefaultAsync();
            if (branded is not null) return branded;
        }

        // Otherwise fall back to tenant identity: in single-tenant deployments
        // there's exactly one real (non-sentinel) tenant. Sentinel "default"
        // tenants seeded by the API are filtered out so we don't pick them
        // over a real one.
        var realTenants = await _db.Tenants
            .Where(t => t.Subdomain != "default")
            .Select(t => new TenantLookupResult(t.Id, t.Name))
            .Take(2)
            .ToListAsync();

        return realTenants.Count == 1 ? realTenants[0] : null;
    }

    private sealed record TenantLookupResult(Guid Id, string Name);

    [HttpGet]
    public async Task<ActionResult<TenantBrandingResponseDto>> Get()
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is not null) return MapToDto(branding);

        // No branding has been configured for this tenant yet. Return a
        // synthetic default so the settings page can render an empty form
        // (pre-filled with the tenant's signup name when available).
        // The PUT endpoint will create the row on first save. Returning a
        // 200 here — instead of the previous 404 — also stops the page from
        // flashing a "Failed to load branding: 404" toast on first visit.
        // Tenant is not BaseEntity-derived, so no global tenant filter applies here.
        var tenantName = await _db.Tenants
            .Where(t => t.Id == _tenantContext.CurrentTenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        return new TenantBrandingResponseDto(
            Id: Guid.Empty,
            LogoFileName: null,
            PrimaryColorHex: null,
            CompanyName: tenantName ?? string.Empty,
            FooterText: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    [HttpPut]
    public async Task<ActionResult<TenantBrandingResponseDto>> Upsert(UpdateTenantBrandingDto dto)
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is null)
        {
            branding = new TenantBranding
            {
                CompanyName = dto.CompanyName,
                PrimaryColorHex = dto.PrimaryColorHex,
                FooterText = dto.FooterText,
            };
            _db.TenantBrandings.Add(branding);
        }
        else
        {
            branding.CompanyName = dto.CompanyName;
            branding.PrimaryColorHex = dto.PrimaryColorHex;
            branding.FooterText = dto.FooterText;
        }

        await _db.SaveChangesAsync();
        return MapToDto(branding);
    }

    [HttpPost("logo")]
    public async Task<ActionResult<TenantBrandingResponseDto>> UploadLogo(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("No file uploaded.");
        if (file.Length > MaxLogoBytes)
            return BadRequest("Logo must be under 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg"))
            return BadRequest("Logo must be PNG, JPG, or SVG.");

        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is null)
        {
            branding = new TenantBranding { CompanyName = "My Company" };
            _db.TenantBrandings.Add(branding);
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(branding.LogoFileName))
        {
            var oldPath = Path.Combine(_env.WebRootPath, "uploads", "logos", branding.LogoFileName);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        branding.LogoFileName = fileName;
        await _db.SaveChangesAsync();

        return MapToDto(branding);
    }

    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo()
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding?.LogoFileName is null) return NotFound();

        var path = Path.Combine(_env.WebRootPath, "uploads", "logos", branding.LogoFileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        branding.LogoFileName = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static TenantBrandingResponseDto MapToDto(TenantBranding b) => new(
        b.Id, b.LogoFileName, b.PrimaryColorHex, b.CompanyName,
        b.FooterText, b.CreatedAt, b.UpdatedAt);
}
