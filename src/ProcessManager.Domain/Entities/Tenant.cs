namespace ProcessManager.Domain.Entities;

/// <summary>
/// A tenant (customer/organisation). Root of the multi-tenant hierarchy —
/// every other domain entity belongs to exactly one tenant via BaseEntity.TenantId.
/// Tenant itself is not tenant-owned (it is the tenant) so it does not inherit BaseEntity.
/// </summary>
public class Tenant
{
    /// <summary>Sentinel tenant used for seeded/demo data and for backfilling rows created before tenancy existed.</summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }

    /// <summary>Short URL-safe identifier (e.g. "acme", "widget-co"). Used as subdomain in SaaS mode.</summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>Human-readable company/organisation name.</summary>
    public string Name { get; set; } = string.Empty;

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum TenantStatus
{
    /// <summary>In trial period — full feature access, no billing.</summary>
    Trial,
    /// <summary>Active paying (or sentinel/default) tenant.</summary>
    Active,
    /// <summary>Temporarily blocked (payment failure, admin action). Login allowed only to /billing.</summary>
    Suspended,
    /// <summary>Permanently closed. Data retained for legal hold but login blocked.</summary>
    Archived
}
