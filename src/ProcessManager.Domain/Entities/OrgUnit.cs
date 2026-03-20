using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Represents a department, work area, role, or individual within the organization.
/// Supports a self-referential hierarchy (e.g. "Quality" → "Incoming Inspection", "Final Inspection").
/// Used for assigning workflow process nodes to responsible parties.
/// </summary>
public class OrgUnit : BaseEntity
{
    /// <summary>Short identifier (e.g. "QC", "ASSY", "ENG").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What kind of organizational entity this represents.</summary>
    public OrgUnitType Type { get; set; } = OrgUnitType.Department;

    /// <summary>Parent OrgUnit in the hierarchy (null for top-level).</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Whether this OrgUnit is available for assignment.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public OrgUnit? Parent { get; set; }
    public ICollection<OrgUnit> Children { get; set; } = new List<OrgUnit>();
    public ICollection<OrgUnitMember> Members { get; set; } = new List<OrgUnitMember>();
}
