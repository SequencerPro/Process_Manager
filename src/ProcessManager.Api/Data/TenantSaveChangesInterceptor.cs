using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Data;

/// <summary>
/// Stamps <see cref="BaseEntity.TenantId"/> on every inserted row from the current
/// <see cref="ITenantContext"/>, and blocks cross-tenant updates by rejecting saves
/// where a modified row's TenantId does not match the current context (unless the
/// caller is a platform admin).
/// </summary>
public sealed class TenantSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenantStamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantStamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantStamp(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Stamp TenantId on every new row. If it's already set (e.g. by platform
                    // admin explicitly provisioning into another tenant) respect that.
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Entity.TenantId = tenantContext.CurrentTenantId;
                    break;

                case EntityState.Modified:
                    // Never let TenantId change on update — it's effectively immutable.
                    entry.Property(nameof(BaseEntity.TenantId)).IsModified = false;

                    // Defence in depth: block updates that somehow loaded a cross-tenant row
                    // (shouldn't happen because the query filter prevents the fetch, but if a
                    // platform admin bypasses the filter we still want writes guarded).
                    if (!tenantContext.IsPlatformAdmin
                        && entry.Entity.TenantId != tenantContext.CurrentTenantId
                        && tenantContext.HasTenant)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant update blocked: entity {entry.Entity.GetType().Name} " +
                            $"belongs to tenant {entry.Entity.TenantId} but current context is " +
                            $"{tenantContext.CurrentTenantId}.");
                    }
                    break;
            }
        }
    }
}
