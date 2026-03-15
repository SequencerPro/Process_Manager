using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A directed edge from one WorkflowProcess to another, with routing rules.
/// </summary>
public class WorkflowLink : BaseEntity
{
    /// <summary>The Workflow this link belongs to.</summary>
    public Guid WorkflowId { get; set; }

    /// <summary>Source node (items leave from).</summary>
    public Guid SourceWorkflowProcessId { get; set; }

    /// <summary>Target node (items arrive at).</summary>
    public Guid TargetWorkflowProcessId { get; set; }

    /// <summary>How routing is determined.</summary>
    public RoutingType RoutingType { get; set; } = RoutingType.Always;

    /// <summary>Optional label for the link.</summary>
    public string? Name { get; set; }

    /// <summary>Display ordering among links from same source.</summary>
    public int SortOrder { get; set; }

    /// <summary>Visual line shape in the builder ("straight" or "curved"). Null defaults to straight.</summary>
    public string? LineShape { get; set; }

    // Navigation properties
    public Workflow Workflow { get; set; } = null!;
    public WorkflowProcess SourceWorkflowProcess { get; set; } = null!;
    public WorkflowProcess TargetWorkflowProcess { get; set; } = null!;
    public ICollection<WorkflowLinkCondition> Conditions { get; set; } = new List<WorkflowLinkCondition>();
}
