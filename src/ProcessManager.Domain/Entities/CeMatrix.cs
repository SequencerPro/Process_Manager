namespace ProcessManager.Domain.Entities;

/// <summary>
/// A Cause and Effect (C&amp;E) Matrix attached to a specific ProcessStep.
/// Rows are inputs (port inputs + free-form control/noise factors).
/// Columns are outputs (port outputs + free-form quality characteristics).
/// Each cell scores how strongly the input affects the output (0/1/3/9 scale).
/// The resulting priority score per input guides improvement prioritisation.
/// </summary>
public class CeMatrix : BaseEntity
{
    public Guid ProcessStepId { get; set; }

    /// <summary>Human-readable name, e.g. "Milling Step C&amp;E — Rev 1".</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ProcessStep ProcessStep { get; set; } = null!;
    public ICollection<CeInput> Inputs { get; set; } = new List<CeInput>();
    public ICollection<CeOutput> Outputs { get; set; } = new List<CeOutput>();
    public ICollection<CeCorrelation> Correlations { get; set; } = new List<CeCorrelation>();
}
