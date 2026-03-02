using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A named connection point on a StepTemplate.
/// The PortType determines what the port represents and which additional fields apply.
/// </summary>
public class Port : BaseEntity
{
    /// <summary>The StepTemplate this Port belongs to.</summary>
    public Guid StepTemplateId { get; set; }

    /// <summary>Human-readable name (e.g., "Good Part Out").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Input or Output.</summary>
    public PortDirection Direction { get; set; }

    /// <summary>Classifies the port: Material, Parameter, Characteristic, or Condition.</summary>
    public PortType PortType { get; set; }

    // ── Material-only fields ────────────────────────────────────────────────

    /// <summary>The Kind of item this port flows. Required when PortType = Material.</summary>
    public Guid? KindId { get; set; }

    /// <summary>The Grade of item this port flows. Required when PortType = Material.</summary>
    public Guid? GradeId { get; set; }

    /// <summary>Quantity rule mode. Required when PortType = Material.</summary>
    public QuantityRuleMode? QtyRuleMode { get; set; }

    /// <summary>The N value (for Exactly and ZeroOrN modes).</summary>
    public int? QtyRuleN { get; set; }

    /// <summary>Minimum (for Range and Unbounded modes).</summary>
    public int? QtyRuleMin { get; set; }

    /// <summary>Maximum (for Range mode; null for Unbounded).</summary>
    public int? QtyRuleMax { get; set; }

    // ── Parameter / Characteristic fields ──────────────────────────────────

    /// <summary>Data type. Required when PortType = Parameter or Characteristic.</summary>
    public DataValueType? DataType { get; set; }

    /// <summary>Unit of measure (e.g., RPM, °C, mm). Applies to Parameter and Characteristic.</summary>
    public string? Units { get; set; }

    /// <summary>Target value stored as a string. Applies to Parameter and Characteristic.</summary>
    public string? NominalValue { get; set; }

    /// <summary>Lower allowable deviation from nominal. Applies to Parameter and Characteristic.</summary>
    public string? LowerTolerance { get; set; }

    /// <summary>Upper allowable deviation from nominal. Applies to Parameter and Characteristic.</summary>
    public string? UpperTolerance { get; set; }

    // ── Common ─────────────────────────────────────────────────────────────

    /// <summary>Display ordering among ports of the same direction.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public StepTemplate StepTemplate { get; set; } = null!;
    public Kind? Kind { get; set; }
    public Grade? Grade { get; set; }
}
