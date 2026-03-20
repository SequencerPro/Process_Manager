namespace ProcessManager.Domain.Entities;

/// <summary>
/// Many-to-many join between an ApplicationUser and an OrgUnit.
/// A user can belong to multiple OrgUnits; an OrgUnit can have multiple members.
/// </summary>
public class OrgUnitMember : BaseEntity
{
    /// <summary>The ApplicationUser Id (string, from ASP.NET Identity).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The OrgUnit this user belongs to.</summary>
    public Guid OrgUnitId { get; set; }

    // Navigation properties
    public OrgUnit OrgUnit { get; set; } = null!;
}
