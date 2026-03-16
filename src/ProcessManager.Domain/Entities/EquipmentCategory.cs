namespace ProcessManager.Domain.Entities;

/// <summary>
/// A user-defined category of equipment (e.g. CNC Lathe, CMM, Assembly Station, Oven, Press).
/// </summary>
public class EquipmentCategory : BaseEntity
{
    /// <summary>Short identifier (e.g. "CMM", "CNC-LATHE").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
