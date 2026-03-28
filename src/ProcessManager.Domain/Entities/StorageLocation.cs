namespace ProcessManager.Domain.Entities;

/// <summary>
/// A physical warehouse location in a zone/aisle/bay/bin hierarchy.
/// Only Items occupy locations — never Kinds.
/// </summary>
public class StorageLocation : BaseEntity
{
    /// <summary>Short unique code, e.g. "A1-B3-S2".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Highest-level grouping (e.g. "Raw Materials", "Finished Goods", "Quarantine").</summary>
    public string? Zone { get; set; }

    /// <summary>Aisle within the zone.</summary>
    public string? Aisle { get; set; }

    /// <summary>Bay within the aisle.</summary>
    public string? Bay { get; set; }

    /// <summary>Lowest-level bin position.</summary>
    public string? Bin { get; set; }

    /// <summary>Optional parent location for hierarchy.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Free-form description.</summary>
    public string? Description { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public StorageLocation? Parent { get; set; }
    public ICollection<StorageLocation> Children { get; set; } = new List<StorageLocation>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
