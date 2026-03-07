using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace ProcessManager.Web.Components.Pages.Workflows;

/// <summary>
/// Custom diagram node model representing a Process within a Workflow.
/// </summary>
public class WorkflowProcessNodeModel : NodeModel
{
    public WorkflowProcessNodeModel(Point position) : base(position)
    {
    }

    /// <summary>Server-side WorkflowProcess Id.</summary>
    public Guid WorkflowProcessId { get; set; }

    /// <summary>The Process this node represents (null for terminal nodes).</summary>
    public Guid? ProcessId { get; set; }

    /// <summary>True if this is a terminal "End" node.</summary>
    public bool IsTerminalNode { get; set; }

    public string ProcessName { get; set; } = "";
    public string ProcessCode { get; set; } = "";
    public bool IsEntryPoint { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Hex color for the tile border/header (e.g. "#0d6efd").</summary>
    public string? Color { get; set; }

    /// <summary>Server-side link Id, kept in a dictionary keyed by target node id for quick lookup.</summary>
    public Guid? ServerLinkId { get; set; }
}
