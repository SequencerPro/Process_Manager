using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A period during which a piece of equipment was unavailable.
/// </summary>
public class DowntimeRecord : BaseEntity
{
    public Guid EquipmentId { get; set; }

    public DowntimeType Type { get; set; }

    public DateTime StartedAt { get; set; }

    /// <summary>Null if equipment is currently down.</summary>
    public DateTime? EndedAt { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? ResolvedBy { get; set; }

    /// <summary>FK to the maintenance task that caused or resolved this downtime event.</summary>
    public Guid? LinkedMaintenanceTaskId { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
