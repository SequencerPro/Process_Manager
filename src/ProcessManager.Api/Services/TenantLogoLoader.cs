using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Services;

/// <summary>
/// Resolves the on-disk path of a tenant's uploaded logo and reads its bytes
/// for embedding in PDF exports. Returns <c>null</c> for any failure (no logo,
/// missing file, IO error) so callers can fall back to text-only headers.
/// </summary>
public static class TenantLogoLoader
{
    public static async Task<byte[]?> LoadAsync(IWebHostEnvironment env, TenantBranding? branding)
    {
        if (branding is null || string.IsNullOrWhiteSpace(branding.LogoFileName))
            return null;

        // QuestPDF's Image() expects raster bytes (PNG/JPEG). SVGs would need
        // .Svg() instead — for now we skip SVG in PDFs and let the header fall
        // back to the company-name text. The on-screen UI (NavMenu, Login)
        // still renders SVG normally via <img>.
        var ext = Path.GetExtension(branding.LogoFileName).ToLowerInvariant();
        if (ext is ".svg") return null;

        try
        {
            var path = Path.Combine(env.WebRootPath, "uploads", "logos", branding.LogoFileName);
            if (!File.Exists(path)) return null;
            return await File.ReadAllBytesAsync(path);
        }
        catch
        {
            // Best-effort — never fail the export if the logo can't be read.
            return null;
        }
    }
}
