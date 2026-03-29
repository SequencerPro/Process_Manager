namespace ProcessManager.Domain.Entities;

/// <summary>
/// One component in a Kind's Bill of Materials.
/// Links a parent (assembly) Kind to a component (raw material / sub-assembly) Kind
/// with a quantity and line item number.
/// </summary>
public class BomLine : BaseEntity
{
    /// <summary>The assembly Kind this line belongs to.</summary>
    public Guid ParentKindId { get; set; }

    /// <summary>The component Kind used in the assembly.</summary>
    public Guid ComponentKindId { get; set; }

    /// <summary>Sequential line item number (unique per parent Kind).</summary>
    public int LineNumber { get; set; }

    /// <summary>How many of this component are needed.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Override of the component Kind's UoM (e.g. "Each", "Kg"). Null = use component default.</summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>Line-level notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Display ordering.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public Kind ParentKind { get; set; } = null!;
    public Kind ComponentKind { get; set; } = null!;
}
