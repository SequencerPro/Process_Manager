using ProcessManager.Api.DTOs;

namespace ProcessManager.Web.Services;

/// <summary>
/// Scoped service that holds the active tenant's branding (company name, logo,
/// primary colour, footer text) for the duration of a Blazor Server circuit.
/// Loaded once per circuit via <c>MainLayout</c> and consumed by <c>NavMenu</c>
/// and any other component that wants to render the tenant's brand.
/// Properties expose sensible defaults so the UI never blanks out while the
/// fetch is in flight or if the API is unreachable.
/// </summary>
public class BrandingService
{
    private TenantBrandingResponseDto? _active;

    /// <summary>Raised when the cached branding changes (load, save, logo upload/delete).</summary>
    public event Action? OnChange;

    public bool IsLoaded { get; private set; }

    /// <summary>Tenant company name, or "Process Manager" as a safe default.</summary>
    public string CompanyName =>
        string.IsNullOrWhiteSpace(_active?.CompanyName) ? "Process Manager" : _active.CompanyName;

    /// <summary>Server-side filename of the uploaded logo (under <c>uploads/logos/</c>), or null.</summary>
    public string? LogoFileName => _active?.LogoFileName;

    /// <summary>True when a tenant logo has been uploaded.</summary>
    public bool HasLogo => !string.IsNullOrWhiteSpace(_active?.LogoFileName);

    /// <summary>Tenant primary brand colour, or the default orange.</summary>
    public string PrimaryColorHex =>
        string.IsNullOrWhiteSpace(_active?.PrimaryColorHex) ? "#f97316" : _active.PrimaryColorHex;

    /// <summary>Optional footer line shown on PDFs and reports.</summary>
    public string? FooterText => _active?.FooterText;

    /// <summary>Returns the absolute URL for the tenant logo, or null when none is uploaded.</summary>
    public string? GetLogoUrl(ApiClient api) =>
        HasLogo ? api.GetImageUrl($"uploads/logos/{LogoFileName}") : null;

    /// <summary>
    /// Loads branding from the API. Idempotent — only fetches once per circuit.
    /// Failures are swallowed so the UI quietly falls back to defaults.
    /// </summary>
    public async Task LoadAsync(ApiClient api)
    {
        if (IsLoaded) return;
        try
        {
            _active = await api.GetTenantBrandingAsync();
            IsLoaded = true;
            OnChange?.Invoke();
        }
        catch
        {
            // Best-effort. IsLoaded stays false so the next circuit init can retry.
        }
    }

    /// <summary>
    /// Immediately replaces the cached branding (called by the Branding settings
    /// page after a save, upload, or delete so the rest of the UI updates without
    /// a full page reload).
    /// </summary>
    public void Set(TenantBrandingResponseDto? branding)
    {
        _active = branding;
        IsLoaded = true;
        OnChange?.Invoke();
    }
}
