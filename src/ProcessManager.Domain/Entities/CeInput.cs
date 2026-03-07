using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A row in a C&amp;E matrix — an input to (or source of variation at) the process step.
/// May be auto-linked from an existing step Port, or added manually.
/// </summary>
public class CeInput : BaseEntity
{
    public Guid CeMatrixId { get; set; }

    /// <summary>Display name for this input (e.g. "Raw aluminium bar", "Spindle speed", "Ambient humidity").</summary>
    public string Name { get; set; } = string.Empty;

    public CeInputCategory Category { get; set; }

    /// <summary>If this input was derived from a Port, stores the Port id for traceability. Null for free-form factors.</summary>
    public Guid? PortId { get; set; }

    /// <summary>Sort position within the matrix rows.</summary>
    public int SortOrder { get; set; }

    // Navigation
    public CeMatrix CeMatrix { get; set; } = null!;
    public Port? Port { get; set; }
    public ICollection<CeCorrelation> Correlations { get; set; } = new List<CeCorrelation>();
}
