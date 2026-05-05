using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── GageStudy ───────────────────────────────────────────────────────────────

public record GageStudyResponseDto(
    Guid Id,
    string Name,
    string StudyType,
    Guid? EquipmentId,
    string? EquipmentCode,
    string? EquipmentName,
    Guid? ProcessId,
    string? ProcessName,
    string? CharacteristicName,
    decimal? Tolerance,
    decimal? LSL,
    decimal? USL,
    int NumberOfParts,
    int NumberOfOperators,
    int NumberOfTrials,
    string Status,
    decimal? GrrPercent,
    int? Ndc,
    string? AcceptanceDecision,
    int MeasurementCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GageStudySummaryDto(
    Guid Id,
    string Name,
    string StudyType,
    string? EquipmentCode,
    string? CharacteristicName,
    string Status,
    decimal? GrrPercent,
    int? Ndc,
    string? AcceptanceDecision,
    int MeasurementCount);

public class CreateGageStudyDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string StudyType { get; set; } = string.Empty;

    public Guid? EquipmentId { get; set; }
    public Guid? ProcessId { get; set; }

    [StringLength(200)]
    public string? CharacteristicName { get; set; }

    public decimal? Tolerance { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }

    [Required, Range(2, 50)]
    public int NumberOfParts { get; set; }

    [Required, Range(2, 10)]
    public int NumberOfOperators { get; set; }

    [Required, Range(2, 10)]
    public int NumberOfTrials { get; set; }
}

public class UpdateGageStudyDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CharacteristicName { get; set; }

    public decimal? Tolerance { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }
}

// ── GageStudyMeasurement ────────────────────────────────────────────────────

public record GageStudyMeasurementDto(
    Guid Id,
    Guid GageStudyId,
    int PartNumber,
    string OperatorId,
    int TrialNumber,
    decimal MeasuredValue);

public class AddGageStudyMeasurementsDto
{
    [Required]
    public List<MeasurementItemDto> Measurements { get; set; } = new();
}

public class MeasurementItemDto
{
    [Required, Range(1, 50)]
    public int PartNumber { get; set; }

    [Required, StringLength(100)]
    public string OperatorId { get; set; } = string.Empty;

    [Required, Range(1, 10)]
    public int TrialNumber { get; set; }

    [Required]
    public decimal MeasuredValue { get; set; }
}

// ── GRR Calculation Result ──────────────────────────────────────────────────

public record GrrCalculationResultDto(
    decimal RepeatabilityEV,
    decimal ReproducibilityAV,
    decimal GRR,
    decimal PartVariationPV,
    decimal TotalVariationTV,
    decimal PercentEV,
    decimal PercentAV,
    decimal PercentGRR,
    decimal PercentPV,
    int Ndc,
    decimal? PercentTolerance,
    string Assessment);

// ── Dashboard ───────────────────────────────────────────────────────────────

public record GageStudyDashboardDto(
    int Total,
    int Complete,
    int InProgress,
    int Draft,
    int Acceptable,
    int Marginal,
    int Unacceptable,
    List<GageStudySummaryDto> WorstStudies);
