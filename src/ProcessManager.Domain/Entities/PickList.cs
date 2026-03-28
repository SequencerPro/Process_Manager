using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A material pick list auto-generated when a Job is created.
/// Lists the Kinds and quantities needed from warehouse locations.
/// </summary>
public class PickList : BaseEntity
{
    /// <summary>The Job this pick list was generated for.</summary>
    public Guid JobId { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public PickListStatus Status { get; set; } = PickListStatus.Open;

    /// <summary>When the pick list was generated.</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Who triggered the generation (usually the job creator).</summary>
    public string GeneratedByUserId { get; set; } = string.Empty;

    // Navigation properties
    public Job Job { get; set; } = null!;
    public ICollection<PickListLine> Lines { get; set; } = new List<PickListLine>();
}
