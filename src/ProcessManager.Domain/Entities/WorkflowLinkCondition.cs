namespace ProcessManager.Domain.Entities;

/// <summary>
/// A grade-based routing condition on a WorkflowLink.
/// Items follow the link only when their grade matches one of its conditions.
/// </summary>
public class WorkflowLinkCondition : BaseEntity
{
    /// <summary>The link this condition belongs to.</summary>
    public Guid WorkflowLinkId { get; set; }

    /// <summary>Route when item has this grade.</summary>
    public Guid GradeId { get; set; }

    // Navigation properties
    public WorkflowLink WorkflowLink { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
}
