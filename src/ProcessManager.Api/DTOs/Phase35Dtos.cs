using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Quality Cost ──────────────────────────────────────────────────────────

public record QualityCostResponseDto(
    Guid Id,
    string SourceType,
    Guid? SourceEntityId,
    string? SourceEntityCode,
    decimal Amount,
    string Currency,
    string CostCategory,
    Guid? KindId,
    string? KindName,
    Guid? JobId,
    string? Description,
    string RecordedByUserId,
    string RecordedByDisplayName,
    DateTime RecordedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record QualityCostSummaryDto(
    Guid Id,
    string SourceType,
    string? SourceEntityCode,
    decimal Amount,
    string Currency,
    string CostCategory,
    string? KindName,
    string? Description,
    string RecordedByDisplayName,
    DateTime RecordedAt);

public class CreateQualityCostDto
{
    [Required, StringLength(30)]
    public string SourceType { get; set; } = "Manual";

    public Guid? SourceEntityId { get; set; }

    [StringLength(100)]
    public string? SourceEntityCode { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "USD";

    [Required, StringLength(30)]
    public string CostCategory { get; set; } = "InternalFailure";

    public Guid? KindId { get; set; }

    [StringLength(200)]
    public string? KindName { get; set; }

    public Guid? JobId { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, StringLength(450)]
    public string RecordedByUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string RecordedByDisplayName { get; set; } = string.Empty;
}

public class UpdateQualityCostDto
{
    [StringLength(30)]
    public string? SourceType { get; set; }

    public Guid? SourceEntityId { get; set; }

    [StringLength(100)]
    public string? SourceEntityCode { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }

    [StringLength(10)]
    public string? Currency { get; set; }

    [StringLength(30)]
    public string? CostCategory { get; set; }

    public Guid? KindId { get; set; }

    [StringLength(200)]
    public string? KindName { get; set; }

    public Guid? JobId { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}

// ── Quality Cost Rule ─────────────────────────────────────────────────────

public record QualityCostRuleResponseDto(
    Guid Id,
    string TriggerEvent,
    string DefaultCategory,
    string DefaultSourceType,
    decimal DefaultAmount,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateQualityCostRuleDto
{
    [Required, StringLength(40)]
    public string TriggerEvent { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string DefaultCategory { get; set; } = "InternalFailure";

    [Required, StringLength(30)]
    public string DefaultSourceType { get; set; } = "Manual";

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal DefaultAmount { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateQualityCostRuleDto
{
    [StringLength(40)]
    public string? TriggerEvent { get; set; }

    [StringLength(30)]
    public string? DefaultCategory { get; set; }

    [StringLength(30)]
    public string? DefaultSourceType { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? DefaultAmount { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

// ── Dashboard ─────────────────────────────────────────────────────────────

public record CoqDashboardDto(
    decimal TotalCostThisMonth,
    decimal TotalCostThisQuarter,
    decimal TotalCostThisYear,
    Dictionary<string, decimal> ByCategory,
    Dictionary<string, decimal> BySourceType,
    List<CoqTrendPointDto> MonthlyTrend,
    List<CoqTopDriverDto> TopDriversByKind,
    int TotalEntries);

public record CoqTrendPointDto(string Month, decimal Prevention, decimal Appraisal, decimal InternalFailure, decimal ExternalFailure, decimal Total);

public record CoqTopDriverDto(string KindName, decimal TotalCost, int EntryCount);
