using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An instance of a StepTemplate placed at a specific position within a Process.
/// </summary>
public class ProcessStep : BaseEntity
{
    /// <summary>The Process this step belongs to.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>The StepTemplate being used.</summary>
    public Guid StepTemplateId { get; set; }

    /// <summary>Position in the process (1-based, contiguous).</summary>
    public int Sequence { get; set; }

    /// <summary>Optional override of the StepTemplate name.</summary>
    public string? NameOverride { get; set; }

    /// <summary>Optional override of the StepTemplate description.</summary>
    public string? DescriptionOverride { get; set; }

    /// <summary>Optional override of the StepTemplate pattern (Transform, Division, etc.).</summary>
    public StepPattern? PatternOverride { get; set; }

    // Navigation properties
    public Process Process { get; set; } = null!;
    public StepTemplate StepTemplate { get; set; } = null!;
    public ICollection<ProcessStepContent> Contents { get; set; } = new List<ProcessStepContent>();
    public ICollection<ProcessStepPortOverride> PortOverrides { get; set; } = new List<ProcessStepPortOverride>();
}
