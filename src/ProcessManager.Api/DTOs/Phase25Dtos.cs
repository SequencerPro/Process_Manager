using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Supplier ──────────────────────────────────────────────────────────────────

public record SupplierResponseDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? Notes,
    DateTime? ApprovedDate,
    DateTime? LastEvaluationDate,
    bool IsActive,
    int EvaluationCount,
    int? LatestOverallScore,
    int OpenNcCount,
    int OpenMrbCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SupplierSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    bool IsActive,
    int EvaluationCount,
    int? LatestOverallScore,
    int OpenNcCount);

public class CreateSupplierDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ContactName { get; set; }

    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}

public class UpdateSupplierDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ContactName { get; set; }

    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

// ── Supplier Status Transition ────────────────────────────────────────────────

public class UpdateSupplierStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── Supplier Evaluation ───────────────────────────────────────────────────────

public record SupplierEvaluationResponseDto(
    Guid Id,
    Guid SupplierId,
    DateTime EvaluationDate,
    int QualityScore,
    int DeliveryScore,
    int ResponsivenessScore,
    int OverallScore,
    string? Notes,
    string? EvaluatedByUserId,
    string? EvaluatedByName,
    DateTime CreatedAt);

public class CreateSupplierEvaluationDto
{
    public DateTime EvaluationDate { get; set; }

    [Range(0, 100)]
    public int QualityScore { get; set; }

    [Range(0, 100)]
    public int DeliveryScore { get; set; }

    [Range(0, 100)]
    public int ResponsivenessScore { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── Supplier Quality Dashboard ────────────────────────────────────────────────

public record SupplierQualityDashboardDto(
    int TotalSuppliers,
    int ApprovedSuppliers,
    int ConditionalSuppliers,
    int SuspendedSuppliers,
    int SuppliersWithOpenNcs,
    int SuppliersWithOpenMrbs,
    double AverageOverallScore,
    List<SupplierSummaryDto> TopPerformers,
    List<SupplierSummaryDto> AtRiskSuppliers);
