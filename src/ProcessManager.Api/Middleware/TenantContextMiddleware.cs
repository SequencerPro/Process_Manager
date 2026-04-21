using System.Security.Claims;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Middleware;

/// <summary>
/// Reads the <c>tenant_id</c> and <c>platform_admin</c> claims from the authenticated
/// principal and populates the per-request <see cref="ITenantContext"/>. Runs after
/// <c>UseAuthentication</c> so the principal is available, and before <c>UseAuthorization</c>
/// and controller execution so the tenant filter is in effect by the time queries run.
/// </summary>
public sealed class TenantContextMiddleware
{
    public const string TenantIdClaim = "tenant_id";
    public const string PlatformAdminClaim = "platform_admin";

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var isPlatformAdmin = string.Equals(
                user.FindFirstValue(PlatformAdminClaim), "true",
                StringComparison.OrdinalIgnoreCase);

            var tenantClaim = user.FindFirstValue(TenantIdClaim);
            if (Guid.TryParse(tenantClaim, out var tenantId))
            {
                tenantContext.SetTenant(tenantId, isPlatformAdmin);
            }
            else if (isPlatformAdmin)
            {
                // Platform admin with no explicit tenant — default sentinel so query filter
                // does something deterministic; IsPlatformAdmin bypasses the filter anyway.
                tenantContext.SetTenant(Tenant.DefaultTenantId, isPlatformAdmin: true);
            }
        }

        await _next(context);
    }
}
