using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Customer Complaint ─────────────────────────────────────────────────────

public record CustomerComplaintResponseDto(
    Guid Id,
    string Code,
    string CustomerName,
    string? CustomerReference,
    Guid? ProductKindId,
    string? LotNumber,
    DateTime ComplaintDate,
    DateTime ReceivedDate,
    string Category,
    string Severity,
    string Description,
    int QuantityAffected,
    string Status,
    string OwnerUserId,
    string OwnerDisplayName,
    DateTime? ResponseDueDate,
    DateTime? ResponseSentAt,
    bool? CustomerSatisfied,
    Guid? LinkedNonConformanceId,
    Guid? LinkedCapaId,
    Guid? LinkedSupplierId,
    DateTime? ClosedAt,
    int InvestigationCount,
    int ResponseCount,
    int ActionItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CustomerComplaintSummaryDto(
    Guid Id,
    string Code,
    string CustomerName,
    string Category,
    string Severity,
    string Status,
    string Description,
    string OwnerDisplayName,
    DateTime? ResponseDueDate,
    DateTime? ClosedAt,
    int InvestigationCount,
    int ResponseCount,
    DateTime CreatedAt);

public class CreateCustomerComplaintDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CustomerReference { get; set; }

    public Guid? ProductKindId { get; set; }

    [StringLength(100)]
    public string? LotNumber { get; set; }

    public DateTime? ComplaintDate { get; set; }

    [Required, StringLength(30)]
    public string Category { get; set; } = "ProductDefect";

    [Required, StringLength(30)]
    public string Severity { get; set; } = "Minor";

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    public int QuantityAffected { get; set; }

    [Required, StringLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string OwnerDisplayName { get; set; } = string.Empty;

    public DateTime? ResponseDueDate { get; set; }
}

public class UpdateCustomerComplaintDto
{
    [StringLength(200)]
    public string? CustomerName { get; set; }

    [StringLength(200)]
    public string? CustomerReference { get; set; }

    public Guid? ProductKindId { get; set; }

    [StringLength(100)]
    public string? LotNumber { get; set; }

    [StringLength(30)]
    public string? Category { get; set; }

    [StringLength(30)]
    public string? Severity { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public int? QuantityAffected { get; set; }

    [StringLength(450)]
    public string? OwnerUserId { get; set; }

    [StringLength(200)]
    public string? OwnerDisplayName { get; set; }

    public DateTime? ResponseDueDate { get; set; }

    public bool? CustomerSatisfied { get; set; }

    public Guid? LinkedNonConformanceId { get; set; }

    public Guid? LinkedCapaId { get; set; }

    public Guid? LinkedSupplierId { get; set; }
}

public class TransitionComplaintStatusDto
{
    [Required, StringLength(40)]
    public string TargetStatus { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── Investigation ──────────────────────────────────────────────────────────

public record ComplaintInvestigationResponseDto(
    Guid Id,
    Guid CustomerComplaintId,
    string InvestigationType,
    string Findings,
    string InvestigatedByUserId,
    string InvestigatedByDisplayName,
    DateTime InvestigatedAt,
    DateTime CreatedAt);

public class CreateComplaintInvestigationDto
{
    [Required, StringLength(30)]
    public string InvestigationType { get; set; } = "InitialAssessment";

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Findings { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string InvestigatedByUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string InvestigatedByDisplayName { get; set; } = string.Empty;
}

// ── Response ───────────────────────────────────────────────────────────────

public record ComplaintResponseResponseDto(
    Guid Id,
    Guid CustomerComplaintId,
    string ResponseType,
    string Content,
    string SentByUserId,
    string SentByDisplayName,
    DateTime SentAt,
    DateTime CreatedAt);

public class CreateComplaintResponseDto
{
    [Required, StringLength(30)]
    public string ResponseType { get; set; } = "Acknowledgment";

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string SentByUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string SentByDisplayName { get; set; } = string.Empty;
}

// ── Dashboard ──────────────────────────────────────────────────────────────

public record ComplaintDashboardDto(
    int TotalOpen,
    int TotalOverdue,
    int AvgDaysToClose,
    decimal CustomerSatisfactionRate,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByCategory,
    Dictionary<string, int> BySeverity,
    List<CustomerComplaintSummaryDto> RecentComplaints);
