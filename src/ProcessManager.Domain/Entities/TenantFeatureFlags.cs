namespace ProcessManager.Domain.Entities;

/// <summary>
/// Per-tenant toggles controlling which modules of the application are surfaced
/// in navigation. Newly-signed-up tenants default to a reduced "MVP" surface so
/// the product does not overwhelm first-time users; advanced modules are opted
/// into from the /settings/modules page.
/// </summary>
public class TenantFeatureFlags : BaseEntity
{
    /// <summary>When true, reveals advanced modules (MRB, Factory Design, Webhooks, AI Audit, etc.) in the NavMenu.</summary>
    public bool ShowAdvancedModules { get; set; }

    /// <summary>Show quality engineering tools (PFMEA, C&amp;E, Control Plan, NC, RCA).</summary>
    public bool ShowQualityTools { get; set; }

    /// <summary>Show production management tools (Equipment, Maintenance, Production Dashboard).</summary>
    public bool ShowProductionTools { get; set; }

    /// <summary>Show warehouse/inventory tools (Locations, Pick Lists).</summary>
    public bool ShowWarehouseTools { get; set; }

    /// <summary>Show training and competency tools.</summary>
    public bool ShowTrainingTools { get; set; }
}
