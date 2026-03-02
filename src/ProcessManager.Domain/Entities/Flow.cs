namespace ProcessManager.Domain.Entities;

/// <summary>
/// A connection from an output port on one ProcessStep to an input port
/// on the next ProcessStep within a Process.
/// </summary>
public class Flow : BaseEntity
{
    /// <summary>The Process this flow belongs to.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>The upstream ProcessStep.</summary>
    public Guid SourceProcessStepId { get; set; }

    /// <summary>The output port on the source step.</summary>
    public Guid SourcePortId { get; set; }

    /// <summary>The downstream ProcessStep.</summary>
    public Guid TargetProcessStepId { get; set; }

    /// <summary>The input port on the target step.</summary>
    public Guid TargetPortId { get; set; }

    // Navigation properties
    public Process Process { get; set; } = null!;
    public ProcessStep SourceProcessStep { get; set; } = null!;
    public Port SourcePort { get; set; } = null!;
    public ProcessStep TargetProcessStep { get; set; } = null!;
    public Port TargetPort { get; set; } = null!;
}
