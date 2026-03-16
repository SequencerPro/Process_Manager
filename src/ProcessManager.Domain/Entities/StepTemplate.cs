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

    /// <summary>Formal lifecycle state — mirrors Process lifecycle for step templates.</summary>
    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;

    /// <summary>
    /// When true this template appears in the shared library and can be reused across processes.
    /// When false it is a private step owned by a single process (created inline from the Builder).
    /// </summary>
    public bool IsShared { get; set; } = true;

    /// <summary>Expected duration under normal conditions. Used for schedule estimation and variance flagging.</summary>
    public int? ExpectedDurationMinutes { get; set; }

    /// <summary>Declares that this step must be performed on a machine of this category (Phase 11b).</summary>
    public Guid? RequiredEquipmentCategoryId { get; set; }

    // Navigation properties
    public EquipmentCategory? RequiredEquipmentCategory { get; set; }
    public ICollection<Port> Ports { get; set; } = new List<Port>();
    public ICollection<StepTemplateImage> Images { get; set; } = new List<StepTemplateImage>();
    public ICollection<RunChartWidget> RunChartWidgets { get; set; } = new List<RunChartWidget>();
    public ICollection<StepTemplateContent> Contents { get; set; } = new List<StepTemplateContent>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
}
