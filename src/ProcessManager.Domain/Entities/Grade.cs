namespace ProcessManager.Domain.Entities;

/// <summary>
/// The condition or qualification an item carries.
/// Grades are defined per Kind.
/// </summary>
public class Grade : BaseEntity
{
    /// <summary>The Kind this Grade belongs to.</summary>
    public Guid KindId { get; set; }

    /// <summary>Short identifier (e.g., "PASS").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Passed").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this is the default Grade for the Kind.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Display ordering.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public Kind Kind { get; set; } = null!;
}
