using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Shift Definitions ───────────────────────────────────────────────────────

public record ShiftDefinitionResponseDto(
    Guid Id,
    string Code,
    string Name,
    string StartTime,
    string EndTime,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateShiftDefinitionDto
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string StartTime { get; set; } = "06:00";

    [Required]
    public string EndTime { get; set; } = "14:00";
}

public class UpdateShiftDefinitionDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string StartTime { get; set; } = "06:00";

    [Required]
    public string EndTime { get; set; } = "14:00";

    public bool IsActive { get; set; } = true;
}

// ── OEE Calculation Results ─────────────────────────────────────────────────

public record OeeSnapshotDto(
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    DateTime ShiftDate,
    string ShiftCode,
    string ShiftName,
    decimal AvailabilityPct,
    decimal PerformancePct,
    decimal QualityPct,
    decimal OeePct,
    decimal PlannedMinutes,
    decimal DowntimeMinutes,
    decimal RunTimeMinutes,
    int TotalPieces,
    int GoodPieces,
    int DefectPieces);

public record OeeTrendPointDto(
    DateTime Date,
    string ShiftCode,
    decimal AvailabilityPct,
    decimal PerformancePct,
    decimal QualityPct,
    decimal OeePct);

public record OeeLossCategoryDto(
    string Category,
    string Type,
    decimal MinutesLost,
    decimal PercentOfTotal,
    int Occurrences);

public record OeeDashboardDto(
    int EquipmentCount,
    decimal AverageOee,
    decimal AverageAvailability,
    decimal AveragePerformance,
    decimal AverageQuality,
    int EquipmentBelowTarget,
    decimal TargetOee,
    List<OeeSnapshotDto> EquipmentSnapshots,
    List<OeeLossCategoryDto> TopLossCategories);

public record OeeTrendDto(
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    List<OeeTrendPointDto> DataPoints);
