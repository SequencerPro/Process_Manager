namespace ProcessManager.Domain.Entities;

/// <summary>
/// A directed graph of Processes connected by routing links.
/// Workflows enable branching, rework loops, and multi-process flows.
/// </summary>
public class Workflow : BaseEntity
{
    /// <summary>Short identifier (e.g., "WF-WDG-01").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Purpose / scope of this workflow.</summary>
    public string? Description { get; set; }

    /// <summary>Version number for change tracking.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether this workflow is available for use.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<WorkflowProcess> WorkflowProcesses { get; set; } = new List<WorkflowProcess>();
    public ICollection<WorkflowLink> WorkflowLinks { get; set; } = new List<WorkflowLink>();
}
