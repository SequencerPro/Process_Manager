using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Per-port override for a ProcessStep. Each record overrides specific
/// properties of an original template Port for a particular process step.
/// Null values mean "use the template default".
/// </summary>
public class ProcessStepPortOverride : BaseEntity
{
    /// <summary>The process step this override belongs to.</summary>
    public Guid ProcessStepId { get; set; }

    /// <summary>The original template port being overridden.</summary>
    public Guid PortId { get; set; }

    public string? NameOverride { get; set; }
    public PortDirection? DirectionOverride { get; set; }
    public Guid? KindIdOverride { get; set; }
    public Guid? GradeIdOverride { get; set; }
    public QuantityRuleMode? QtyRuleModeOverride { get; set; }
    public int? QtyRuleNOverride { get; set; }
    public int? SortOrderOverride { get; set; }

    // Navigation properties
    public ProcessStep ProcessStep { get; set; } = null!;
    public Port Port { get; set; } = null!;
    public Kind? KindOverride { get; set; }
    public Grade? GradeOverride { get; set; }
}
