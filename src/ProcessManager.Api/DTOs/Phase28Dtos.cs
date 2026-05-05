using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── CalibrationRecord ────────────────────────────────────────────────────────

public record CalibrationRecordResponseDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    string CalibrationType,
    DateTime CalibrationDate,
    DateTime NextDueDate,
    string? CertificateNumber,
    string? CertificateFileName,
    string Result,
    string? PerformedBy,
    string? StandardsUsed,
    string? TemperatureHumidity,
    string? AsFoundReading,
    string? AsLeftReading,
    decimal? Uncertainty,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CalibrationRecordSummaryDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    string CalibrationType,
    DateTime CalibrationDate,
    DateTime NextDueDate,
    string Result,
    string? CertificateNumber,
    string? PerformedBy);

public class CreateCalibrationRecordDto
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required, StringLength(30)]
    public string CalibrationType { get; set; } = string.Empty;

    [Required]
    public DateTime CalibrationDate { get; set; }

    [Required]
    public DateTime NextDueDate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(500)]
    public string? CertificateFileName { get; set; }

    [Required, StringLength(20)]
    public string Result { get; set; } = string.Empty;

    [StringLength(200)]
    public string? PerformedBy { get; set; }

    [StringLength(500)]
    public string? StandardsUsed { get; set; }

    [StringLength(200)]
    public string? TemperatureHumidity { get; set; }

    [StringLength(500)]
    public string? AsFoundReading { get; set; }

    [StringLength(500)]
    public string? AsLeftReading { get; set; }

    public decimal? Uncertainty { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}

public class UpdateCalibrationRecordDto
{
    [Required, StringLength(30)]
    public string CalibrationType { get; set; } = string.Empty;

    [Required]
    public DateTime CalibrationDate { get; set; }

    [Required]
    public DateTime NextDueDate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(500)]
    public string? CertificateFileName { get; set; }

    [Required, StringLength(20)]
    public string Result { get; set; } = string.Empty;

    [StringLength(200)]
    public string? PerformedBy { get; set; }

    [StringLength(500)]
    public string? StandardsUsed { get; set; }

    [StringLength(200)]
    public string? TemperatureHumidity { get; set; }

    [StringLength(500)]
    public string? AsFoundReading { get; set; }

    [StringLength(500)]
    public string? AsLeftReading { get; set; }

    public decimal? Uncertainty { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── CalibrationSchedule ──────────────────────────────────────────────────────

public record CalibrationScheduleResponseDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    int IntervalDays,
    string IntervalAdjustmentMethod,
    int ConsecutivePassCount,
    int MaxIntervalDays,
    int MinIntervalDays,
    int ExtensionPercent,
    bool IsActive,
    DateTime? LastCalibrationDate,
    DateTime? NextDueDate,
    string? LastResult,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateCalibrationScheduleDto
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required, Range(1, 3650)]
    public int IntervalDays { get; set; }

    [Required, StringLength(30)]
    public string IntervalAdjustmentMethod { get; set; } = "Fixed";

    [Range(1, 3650)]
    public int MaxIntervalDays { get; set; } = 365;

    [Range(1, 3650)]
    public int MinIntervalDays { get; set; } = 30;

    [Range(1, 100)]
    public int ExtensionPercent { get; set; } = 25;
}

public class UpdateCalibrationScheduleDto
{
    [Required, Range(1, 3650)]
    public int IntervalDays { get; set; }

    [Required, StringLength(30)]
    public string IntervalAdjustmentMethod { get; set; } = "Fixed";

    [Range(1, 3650)]
    public int MaxIntervalDays { get; set; } = 365;

    [Range(1, 3650)]
    public int MinIntervalDays { get; set; } = 30;

    [Range(1, 100)]
    public int ExtensionPercent { get; set; } = 25;

    public bool IsActive { get; set; } = true;
}

// ── Calibration Dashboard ────────────────────────────────────────────────────

public record CalibrationDashboardDto(
    int TotalSchedules,
    int ActiveSchedules,
    int DueCount,
    int OverdueCount,
    int TotalRecords,
    int PassCount,
    int FailCount,
    int LimitedCount,
    List<CalibrationRecallDto> DueRecalls,
    List<CalibrationRecallDto> OverdueRecalls);

public record CalibrationRecallDto(
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    DateTime NextDueDate,
    int DaysUntilDue,
    string? LastResult,
    DateTime? LastCalibrationDate);
