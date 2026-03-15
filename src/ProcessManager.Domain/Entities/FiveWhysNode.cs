namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single node in a branching 5 Whys tree.
/// ParentNodeId = null means this is a direct child of the problem statement (depth 1).
/// IsRootCause = true marks where the causal chain terminates at an actionable level.
/// </summary>
public class FiveWhysNode : BaseEntity
{
    public Guid AnalysisId { get; set; }

    /// <summary>Null = depth-1 node (direct answer to the problem statement).</summary>
    public Guid? ParentNodeId { get; set; }

    public string WhyStatement { get; set; } = string.Empty;

    /// <summary>True when engineer judges this is an actionable root cause (no further drilling needed/possible).</summary>
    public bool IsRootCause { get; set; }

    /// <summary>Linked library entry — set when engineer selects from typeahead or marks IsRootCause.</summary>
    public Guid? RootCauseLibraryEntryId { get; set; }

    /// <summary>Corrective action text — appears when IsRootCause is checked.</summary>
    public string? CorrectiveAction { get; set; }

    // Navigation
    public FiveWhysAnalysis Analysis { get; set; } = null!;
    public FiveWhysNode? ParentNode { get; set; }
    public RootCauseEntry? RootCauseLibraryEntry { get; set; }
    public ICollection<FiveWhysNode> ChildNodes { get; set; } = new List<FiveWhysNode>();
}
