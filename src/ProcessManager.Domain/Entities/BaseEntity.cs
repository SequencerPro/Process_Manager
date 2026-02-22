namespace ProcessManager.Domain.Entities;

/// <summary>
/// Base class for all entities, providing common audit fields.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
