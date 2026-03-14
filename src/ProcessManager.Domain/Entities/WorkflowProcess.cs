namespace ProcessManager.Domain.Entities;

/// <summary>
/// A placement of a Process within a Workflow (a node in the graph).
/// </summary>
public class WorkflowProcess : BaseEntity
{
    /// <summary>The Workflow this placement belongs to.</summary>
    public Guid WorkflowId { get; set; }

    /// <summary>The Process being placed.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>Is this a starting point in the workflow?</summary>
    public bool IsEntryPoint { get; set; }

    /// <summary>Display ordering.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public Process Process { get; set; } = null!;

    /// <summary>Links where this node is the source.</summary>
    public ICollection<WorkflowLink> OutgoingLinks { get; set; } = new List<WorkflowLink>();

    /// <summary>Links where this node is the target.</summary>
    public ICollection<WorkflowLink> IncomingLinks { get; set; } = new List<WorkflowLink>();
}
