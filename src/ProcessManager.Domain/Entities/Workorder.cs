using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Groups multiple Jobs under a single tracking number, driven by a Workflow.
/// When created, Jobs are generated for each entry-point process in the Workflow.
/// As Jobs complete, successor Jobs are auto-created following WorkflowLinks.
/// </summary>
public class Workorder : BaseEntity
{
    /// <summary>Short identifier (e.g., "WO-2026-001").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Purpose/scope of this workorder.</summary>
    public string? Description { get; set; }

    /// <summary>The Workflow driving this workorder.</summary>
    public Guid WorkflowId { get; set; }

    /// <summary>The Workflow version pinned at creation.</summary>
    public int WorkflowVersion { get; set; } = 1;

    /// <summary>Current lifecycle state.</summary>
    public WorkorderStatus Status { get; set; } = WorkorderStatus.Created;

    /// <summary>Relative priority (higher = more urgent). Inherited by child Jobs.</summary>
    public int Priority { get; set; }

    /// <summary>When work actually began.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When workorder finished (completed or cancelled).</summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public ICollection<WorkorderJob> WorkorderJobs { get; set; } = new List<WorkorderJob>();
}
