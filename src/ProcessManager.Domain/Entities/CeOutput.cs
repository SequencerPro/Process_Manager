using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A column in a C&amp;E matrix — an output of the process step or a quality characteristic to be controlled.
/// May be auto-linked from an existing step Port, or defined manually.
/// </summary>
public class CeOutput : BaseEntity
{
    public Guid CeMatrixId { get; set; }

    /// <summary>Display name for this output (e.g. "Machined part", "Flatness", "Surface finish Ra").</summary>
    public string Name { get; set; } = string.Empty;

    public CeOutputCategory Category { get; set; }

    /// <summary>If this output was derived from a Port, stores the Port id for traceability. Null for quality characteristics.</summary>
    public Guid? PortId { get; set; }

    /// <summary>
    /// Importance weight for this output (1–10).
    /// Reflects how critical this output is to the customer or downstream step.
    /// Used in priority score calculation: Σ(CorrelationScore × Importance).
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>Sort position within the matrix columns.</summary>
    public int SortOrder { get; set; }

    // Navigation
    public CeMatrix CeMatrix { get; set; } = null!;
    public Port? Port { get; set; }
    public ICollection<CeCorrelation> Correlations { get; set; } = new List<CeCorrelation>();
}
