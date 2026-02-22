using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A named connection point on a StepTemplate through which items flow.
/// Each port declares exactly one Item Type (Kind + Grade) and a quantity rule.
/// </summary>
public class Port : BaseEntity
{
    /// <summary>The StepTemplate this Port belongs to.</summary>
    public Guid StepTemplateId { get; set; }

    /// <summary>Human-readable name (e.g., "Good Part Out").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Input or Output.</summary>
    public PortDirection Direction { get; set; }

    /// <summary>The Kind of item this port flows.</summary>
    public Guid KindId { get; set; }

    /// <summary>The Grade of item this port flows.</summary>
    public Guid GradeId { get; set; }

    /// <summary>Quantity rule mode.</summary>
    public QuantityRuleMode QtyRuleMode { get; set; }

    /// <summary>The N value (for Exactly and ZeroOrN modes).</summary>
    public int? QtyRuleN { get; set; }

    /// <summary>Minimum (for Range and Unbounded modes).</summary>
    public int? QtyRuleMin { get; set; }

    /// <summary>Maximum (for Range mode; null for Unbounded).</summary>
    public int? QtyRuleMax { get; set; }

    /// <summary>Display ordering among ports of the same direction.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public StepTemplate StepTemplate { get; set; } = null!;
    public Kind Kind { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
}
