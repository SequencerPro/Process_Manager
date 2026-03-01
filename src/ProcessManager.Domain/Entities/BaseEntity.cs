namespace ProcessManager.Domain.Entities;

/// <summary>
/// Base class for all entities, providing common audit fields.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Username of the user who created this record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Username of the user who last updated this record.</summary>
    public string? UpdatedBy { get; set; }
}
