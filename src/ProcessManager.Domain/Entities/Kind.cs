namespace ProcessManager.Domain.Entities;

/// <summary>
/// Defines what something physically or logically is.
/// A Kind has a set of valid Grades and tracking flags.
/// </summary>
public class Kind : BaseEntity
{
    /// <summary>Short identifier (e.g., "WDG-100").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Widget").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether individual items get unique IDs.</summary>
    public bool IsSerialized { get; set; }

    /// <summary>Whether items can be grouped into batches.</summary>
    public bool IsBatchable { get; set; }

    // Navigation properties
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
