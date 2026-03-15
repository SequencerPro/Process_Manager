using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ControlPlanEntry : BaseEntity
{
    public Guid ControlPlanId { get; set; }
    public Guid ProcessStepId { get; set; }
    public string CharacteristicName { get; set; } = string.Empty;
    public CharacteristicType CharacteristicType { get; set; } = CharacteristicType.Product;
    public string? SpecificationOrTolerance { get; set; }
    public string? MeasurementTechnique { get; set; }
    public string? SampleSize { get; set; }
    public string? SampleFrequency { get; set; }
    public string? ControlMethod { get; set; }
    public string? ReactionPlan { get; set; }
    public Guid? LinkedPfmeaFailureModeId { get; set; }
    public Guid? LinkedPortId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public ControlPlan ControlPlan { get; set; } = null!;
    public ProcessStep ProcessStep { get; set; } = null!;
    public PfmeaFailureMode? LinkedPfmeaFailureMode { get; set; }
    public Port? LinkedPort { get; set; }
}
