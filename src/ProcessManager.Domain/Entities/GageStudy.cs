using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class GageStudy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public GageStudyType StudyType { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? ProcessId { get; set; }
    public string? CharacteristicName { get; set; }
    public decimal? Tolerance { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }
    public int NumberOfParts { get; set; }
    public int NumberOfOperators { get; set; }
    public int NumberOfTrials { get; set; }
    public GageStudyStatus Status { get; set; } = GageStudyStatus.Draft;
    public decimal? GrrPercent { get; set; }
    public int? Ndc { get; set; }
    public string? AcceptanceDecision { get; set; }

    // Navigation
    public Equipment? Equipment { get; set; }
    public Process? Process { get; set; }
    public ICollection<GageStudyMeasurement> Measurements { get; set; } = new List<GageStudyMeasurement>();
}
