using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Services;

/// <summary>
/// Per-request tenant context. Populated by <see cref="Middleware.TenantContextMiddleware"/>
/// from the authenticated user's JWT <c>tenant_id</c> claim, and consumed by the EF Core
/// global query filter and the tenant-stamping <c>SaveChanges</c> interceptor.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The tenant Id for the current request. Falls back to <see cref="Tenant.DefaultTenantId"/>
    /// for unauthenticated requests and background operations so that query filters behave
    /// deterministically; authenticated cross-tenant access is gated by <see cref="IsPlatformAdmin"/>.
    /// </summary>
    Guid CurrentTenantId { get; }

    /// <summary>True when the current principal has the platform-admin flag — bypasses tenant filtering.</summary>
    bool IsPlatformAdmin { get; }

    /// <summary>
    /// True once the context has been populated from an authenticated request. False for background
    /// work, test setup, or unauthenticated endpoints. Repositories that must require an authenticated
    /// tenant (e.g. writes) check this flag.
    /// </summary>
    bool HasTenant { get; }

    /// <summary>Set the context for this request. Called by the middleware; not for controller use.</summary>
    void SetTenant(Guid tenantId, bool isPlatformAdmin);

    /// <summary>
    /// Explicitly override the tenant for a block of work — used by platform admins, background
    /// seeding, and tests. The caller is responsible for restoring (via the returned disposable).
    /// </summary>
    IDisposable BeginScope(Guid tenantId, bool isPlatformAdmin = false);
}

/// <summary>Default scoped implementation. Registered per-request in DI.</summary>
public sealed class TenantContext : ITenantContext
{
    private Guid _tenantId = Tenant.DefaultTenantId;
    private bool _isPlatformAdmin;
    private bool _hasTenant;

    public Guid CurrentTenantId => _tenantId;
    public bool IsPlatformAdmin => _isPlatformAdmin;
    public bool HasTenant => _hasTenant;

    public void SetTenant(Guid tenantId, bool isPlatformAdmin)
    {
        _tenantId = tenantId;
        _isPlatformAdmin = isPlatformAdmin;
        _hasTenant = true;
    }

    public IDisposable BeginScope(Guid tenantId, bool isPlatformAdmin = false)
    {
        var prev = (_tenantId, _isPlatformAdmin, _hasTenant);
        _tenantId = tenantId;
        _isPlatformAdmin = isPlatformAdmin;
        _hasTenant = true;
        return new Restorer(() =>
        {
            _tenantId = prev._tenantId;
            _isPlatformAdmin = prev._isPlatformAdmin;
            _hasTenant = prev._hasTenant;
        });
    }

    private sealed class Restorer(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
