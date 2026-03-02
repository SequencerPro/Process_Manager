using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A reusable definition of a unit of work with typed, quantified ports.
/// </summary>
public class StepTemplate : BaseEntity
{
    /// <summary>Short identifier (e.g., "INSP-DIM-01").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Dimensional Inspection").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed work instructions / description.</summary>
    public string? Description { get; set; }

    /// <summary>Classification by port configuration.</summary>
    public StepPattern Pattern { get; set; }

    /// <summary>Version number for change tracking.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether this template is available for use.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Port> Ports { get; set; } = new List<Port>();
    public ICollection<StepTemplateImage> Images { get; set; } = new List<StepTemplateImage>();
    public ICollection<RunChartWidget> RunChartWidgets { get; set; } = new List<RunChartWidget>();
    public ICollection<StepTemplateContent> Contents { get; set; } = new List<StepTemplateContent>();
}
