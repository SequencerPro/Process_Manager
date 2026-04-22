using ProcessManager.Api.DTOs;

namespace ProcessManager.Web.Services;

/// <summary>
/// Scoped service that holds the active tenant's feature flags for the duration
/// of a Blazor Server circuit. Loaded once per circuit via MainLayout (or the
/// first component that needs them). Consumers bind to individual bool properties
/// and subscribe to <see cref="OnChange"/> so they re-render when the flags change.
/// </summary>
public class FeatureFlagService
{
    private TenantFeatureFlagsDto? _active;

    public event Action? OnChange;

    public bool IsLoaded { get; private set; }

    /// <summary>
    /// When the flags haven't loaded yet we default to showing everything —
    /// this prevents the UI from flickering modules off and on during the first
    /// render. Legacy tenants that pre-exist M2 have all modules enabled server-side.
    /// </summary>
    public bool ShowAdvancedModules  => _active?.ShowAdvancedModules  ?? true;
    public bool ShowQualityTools     => _active?.ShowQualityTools     ?? true;
    public bool ShowProductionTools  => _active?.ShowProductionTools  ?? true;
    public bool ShowWarehouseTools   => _active?.ShowWarehouseTools   ?? true;
    public bool ShowTrainingTools    => _active?.ShowTrainingTools    ?? true;

    /// <summary>Loads the tenant's flags from the API. Idempotent — only fetches once per circuit.</summary>
    public async Task LoadAsync(ApiClient api)
    {
        if (IsLoaded) return;
        try
        {
            _active = await api.GetTenantFeatureFlagsAsync();
            IsLoaded = true;
            OnChange?.Invoke();
        }
        catch
        {
            // Best-effort — fall through to safe defaults.
        }
    }

    /// <summary>Immediately replaces the cached flags (used after Settings → Modules save).</summary>
    public void Set(TenantFeatureFlagsDto? flags)
    {
        _active = flags;
        IsLoaded = true;
        OnChange?.Invoke();
    }
}
