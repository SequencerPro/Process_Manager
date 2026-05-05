using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class CalibrationRecord : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public CalibrationType CalibrationType { get; set; }
    public DateTime CalibrationDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? CertificateFileName { get; set; }
    public CalibrationResult Result { get; set; }
    public string? PerformedBy { get; set; }
    public string? StandardsUsed { get; set; }
    public string? TemperatureHumidity { get; set; }
    public string? AsFoundReading { get; set; }
    public string? AsLeftReading { get; set; }
    public decimal? Uncertainty { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
