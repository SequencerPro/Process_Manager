namespace ProcessManager.Domain.Entities;

/// <summary>
/// Maps a Job to its corresponding WorkflowProcess node within a Workorder.
/// Enables dependency tracking: which workflow node does each job fulfill?
/// </summary>
public class WorkorderJob : BaseEntity
{
    /// <summary>The Workorder this job belongs to.</summary>
    public Guid WorkorderId { get; set; }

    /// <summary>The WorkflowProcess node this job fulfills.</summary>
    public Guid WorkflowProcessId { get; set; }

    /// <summary>The Job executing the process at this workflow node.</summary>
    public Guid JobId { get; set; }

    // Navigation properties
    public Workorder Workorder { get; set; } = null!;
    public WorkflowProcess WorkflowProcess { get; set; } = null!;
    public Job Job { get; set; } = null!;
}
