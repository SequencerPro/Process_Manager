using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single cause on an Ishikawa diagram.
/// Top-level causes hang off a category "spine"; sub-causes have a ParentCauseId.
/// </summary>
public class IshikawaCause : BaseEntity
{
    public Guid DiagramId { get; set; }

    /// <summary>7M category this cause belongs to.</summary>
    public RootCauseCategory Category { get; set; }

    public string CauseText { get; set; } = string.Empty;

    /// <summary>Null = top-level bone cause; set = sub-cause of another cause.</summary>
    public Guid? ParentCauseId { get; set; }

    /// <summary>Linked library entry (set when engineer selects from typeahead).</summary>
    public Guid? RootCauseLibraryEntryId { get; set; }

    /// <summary>True when the team marks this as an actual confirmed root cause.</summary>
    public bool IsSelectedRootCause { get; set; }

    // Navigation
    public IshikawaDiagram Diagram { get; set; } = null!;
    public IshikawaCause? ParentCause { get; set; }
    public RootCauseEntry? RootCauseLibraryEntry { get; set; }
    public ICollection<IshikawaCause> SubCauses { get; set; } = new List<IshikawaCause>();
}
