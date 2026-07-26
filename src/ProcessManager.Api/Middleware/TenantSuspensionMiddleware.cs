using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Middleware;

public sealed class TenantSuspensionMiddleware
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/billing",
        "/api/billing/subscription",
        "/api/billing/portal-session",
        "/api/billing/events",
        "/api/billing/usage",
        "/api/auth/logout",
        "/api/auth/me",
        "/api/platform/stripe-webhook",
        "/health"
    };

    private readonly RequestDelegate _next;

    public TenantSuspensionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ProcessManagerDbContext db)
    {
        if (!tenantContext.HasTenant || tenantContext.IsPlatformAdmin)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (path.StartsWith("/api/public/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantContext.CurrentTenantId);

        if (tenant?.Status == TenantStatus.Suspended)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Payment required",
                message = "Your subscription is suspended. Please update your billing information.",
                billingUrl = "/api/billing"
            });
            return;
        }

        await _next(context);
    }
}
