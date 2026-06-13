namespace ProcessManager.Domain.Entities;

/// <summary>
/// Attaches an existing operational Process or Workflow to a strategic objective:
/// "this work realizes this objective". Exactly one of ProcessId/WorkflowId is set.
/// </summary>
public class ObjectiveProcessLink : BaseEntity
{
    public Guid ObjectiveId { get; set; }
    public StrategicObjective Objective { get; set; } = null!;

    public Guid? ProcessId { get; set; }
    public Process? Process { get; set; }

    public Guid? WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }

    /// <summary>How this work serves the objective.</summary>
    public string? Note { get; set; }
}
