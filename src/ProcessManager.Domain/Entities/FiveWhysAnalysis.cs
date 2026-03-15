using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A branching 5 Whys analysis — iterative depth-first cause tree.
/// The root node is the problem statement; each node can have multiple child nodes (independent causes).
/// Leaf nodes marked IsRootCause represent confirmed root causes.
/// </summary>
public class FiveWhysAnalysis : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>The problem or failure being investigated (root node text).</summary>
    public string ProblemStatement { get; set; } = string.Empty;

    public RcaLinkedEntityType LinkedEntityType { get; set; } = RcaLinkedEntityType.Manual;
    public Guid? LinkedEntityId { get; set; }

    public RcaStatus Status { get; set; } = RcaStatus.Open;
    public DateTime? ClosedAt { get; set; }
    public string? ClosureNotes { get; set; }

    // Navigation
    public ICollection<FiveWhysNode> Nodes { get; set; } = new List<FiveWhysNode>();
}
