using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── CAPA Record ─────────────────────────────────────────────────────────────

public record CapaRecordResponseDto(
    Guid Id,
    string Code,
    string Type,
    string SourceType,
    Guid? SourceEntityId,
    string ProblemStatement,
    string? ContainmentAction,
    Guid? RootCauseAnalysisId,
    string? RootCauseAnalysisType,
    string? PermanentCorrectiveAction,
    string? PreventiveAction,
    string? VerificationMethod,
    DateTime? VerificationDueDate,
    string? VerifiedByUserId,
    DateTime? VerifiedAt,
    DateTime? EffectivenessReviewDate,
    string? EffectivenessVerifiedByUserId,
    DateTime? EffectivenessVerifiedAt,
    string Status,
    string OwnerUserId,
    string OwnerDisplayName,
    string? TeamMemberIds,
    DateTime? ClosedAt,
    int StepCount,
    int ActionItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CapaRecordSummaryDto(
    Guid Id,
    string Code,
    string Type,
    string SourceType,
    string Status,
    string ProblemStatement,
    string OwnerDisplayName,
    DateTime? VerificationDueDate,
    DateTime? ClosedAt,
    int StepCount,
    int ActionItemCount,
    DateTime CreatedAt);

public class CreateCapaRecordDto
{
    [Required, StringLength(30)]
    public string Type { get; set; } = "Corrective";

    [StringLength(30)]
    public string SourceType { get; set; } = "Manual";

    public Guid? SourceEntityId { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string ProblemStatement { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? ContainmentAction { get; set; }

    [Required, StringLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string OwnerDisplayName { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? TeamMemberIds { get; set; }
}

public class UpdateCapaRecordDto
{
    [StringLength(4000)]
    public string? ProblemStatement { get; set; }

    [StringLength(4000)]
    public string? ContainmentAction { get; set; }

    [StringLength(4000)]
    public string? PermanentCorrectiveAction { get; set; }

    [StringLength(4000)]
    public string? PreventiveAction { get; set; }

    [StringLength(4000)]
    public string? VerificationMethod { get; set; }

    public DateTime? VerificationDueDate { get; set; }

    public DateTime? EffectivenessReviewDate { get; set; }

    [StringLength(4000)]
    public string? TeamMemberIds { get; set; }
}

// ── CAPA Step ────────────────────────────────────────────────────────────────

public record CapaStepResponseDto(
    Guid Id,
    Guid CapaRecordId,
    string StepType,
    string? CompletedByUserId,
    string? CompletedByDisplayName,
    DateTime? CompletedAt,
    string? Notes,
    string? AttachmentFileName,
    DateTime CreatedAt);

public class CreateCapaStepDto
{
    [Required, StringLength(50)]
    public string StepType { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? AttachmentFileName { get; set; }
}

// ── CAPA Lifecycle DTOs ──────────────────────────────────────────────────────

public class TransitionCapaDto
{
    [StringLength(4000)]
    public string? Notes { get; set; }
}

public class LinkRcaDto
{
    public Guid RootCauseAnalysisId { get; set; }

    [Required, StringLength(30)]
    public string RootCauseAnalysisType { get; set; } = string.Empty;
}

public class VerifyCapaDto
{
    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── CAPA Dashboard ───────────────────────────────────────────────────────────

public record CapaDashboardDto(
    int TotalOpen,
    int TotalOverdue,
    int TotalClosed,
    double AvgDaysToClose,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> BySourceType,
    double EffectivenessRate,
    List<CapaRecordSummaryDto> OverdueCapas);
