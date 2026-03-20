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

    /// <summary>The OrgUnit assigned to this workflow process node.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Display name of the assigned OrgUnit.</summary>
    public string? AssigneeName { get; set; }

    /// <summary>Server-side link Id, kept in a dictionary keyed by target node id for quick lookup.</summary>
    public Guid? ServerLinkId { get; set; }

    // ───────── Process detail data (loaded asynchronously after diagram renders) ─────────

    /// <summary>Whether process detail data has been loaded.</summary>
    public bool DetailLoaded { get; set; }

    /// <summary>Number of steps in the process.</summary>
    public int StepCount { get; set; }

    /// <summary>Total input ports across all steps.</summary>
    public int InputPortCount { get; set; }

    /// <summary>Total output ports across all steps.</summary>
    public int OutputPortCount { get; set; }

    /// <summary>Material flow tags summarising what flows in/out of this process.</summary>
    public List<MaterialFlowTag> MaterialFlows { get; set; } = new();
}

/// <summary>
/// Summarises a single material input or output for display on a workflow node.
/// </summary>
public record MaterialFlowTag(string KindName, string GradeName, string Direction);
