using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An Ishikawa (fishbone) diagram — structured cause enumeration organised by 7M category.
/// One diagram per investigation; causes are grouped into category "bones" with one level of sub-causes.
/// </summary>
public class IshikawaDiagram : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>The problem or effect being investigated (the "head" of the fish).</summary>
    public string ProblemStatement { get; set; } = string.Empty;

    /// <summary>What triggered this investigation.</summary>
    public RcaLinkedEntityType LinkedEntityType { get; set; } = RcaLinkedEntityType.Manual;

    /// <summary>Id of the linked NonConformance or PfmeaFailureMode, or null for Manual.</summary>
    public Guid? LinkedEntityId { get; set; }

    public RcaStatus Status { get; set; } = RcaStatus.Open;
    public DateTime? ClosedAt { get; set; }
    public string? ClosureNotes { get; set; }

    // Navigation
    public ICollection<IshikawaCause> Causes { get; set; } = new List<IshikawaCause>();
}
