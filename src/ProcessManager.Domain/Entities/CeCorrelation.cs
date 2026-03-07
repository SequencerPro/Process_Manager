namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single cell in the C&amp;E matrix: the correlation score between one input and one output.
/// Score is the standard QFD scale: 0 = no relationship, 1 = weak, 3 = moderate, 9 = strong.
/// Priority score for an input = Σ (Score × Output.Importance) across all outputs.
/// </summary>
public class CeCorrelation : BaseEntity
{
    public Guid CeInputId { get; set; }
    public Guid CeOutputId { get; set; }

    /// <summary>Relationship strength. Valid values: 0, 1, 3, 9.</summary>
    public int Score { get; set; } = 0;

    // Navigation
    public CeInput Input { get; set; } = null!;
    public CeOutput Output { get; set; } = null!;
}
