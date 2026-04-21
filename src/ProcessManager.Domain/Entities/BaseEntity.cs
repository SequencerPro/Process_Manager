namespace ProcessManager.Domain.Entities;

/// <summary>
/// Base class for all tenant-owned entities. Provides audit fields and the
/// <see cref="TenantId"/> that scopes every row to a single tenant.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this row belongs to. Stamped automatically by the SaveChanges
    /// interceptor on insert from <c>ITenantContext.CurrentTenantId</c>.
    /// Rows are filtered by this column via a global EF query filter so that
    /// one tenant cannot read or write another tenant's data.
    /// </summary>
    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Username of the user who created this record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Username of the user who last updated this record.</summary>
    public string? UpdatedBy { get; set; }
}
