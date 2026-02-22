namespace ProcessManager.Domain.Entities;

/// <summary>
/// A linear sequence of steps. Has exactly one entry and one exit point.
/// </summary>
public class Process : BaseEntity
{
    /// <summary>Short identifier (e.g., "WDG-MACH-01").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Widget Machining").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Purpose and scope of this process.</summary>
    public string? Description { get; set; }

    /// <summary>Version number for change tracking.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether this process is available for use.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();
    public ICollection<Flow> Flows { get; set; } = new List<Flow>();
}
