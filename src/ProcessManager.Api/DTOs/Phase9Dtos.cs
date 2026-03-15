using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Approval Records ────────────────────

public record ApprovalRecordResponseDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    int EntityVersion,
    string SubmittedBy,
    DateTime SubmittedAt,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string Decision,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── Lifecycle Actions ────────────────────

/// <summary>Submit a Draft Process or StepTemplate for approval.</summary>
public record SubmitForApprovalDto(
    [Required, StringLength(200)] string SubmittedBy,
    [StringLength(2000)] string? SubmissionNotes = null
);

/// <summary>Approve a PendingApproval submission.</summary>
public record ApproveDto(
    [Required, StringLength(200)] string ApprovedBy,
    [StringLength(2000)] string? ApprovalNotes = null
);

/// <summary>Reject a PendingApproval submission, returning it to Draft.</summary>
public record RejectDto(
    [Required, StringLength(200)] string RejectedBy,
    [Required, StringLength(2000)] string RejectionReason
);

/// <summary>Create a new Draft revision from a Released Process or StepTemplate.</summary>
public record NewRevisionDto(
    [Required, StringLength(200)] string RequestedBy
);

// ──────────────────── PFMEA Staleness ────────────────────

public record ClearPfmeaStalenessDto(
    [Required, StringLength(200)] string ClearedBy,
    [StringLength(2000)] string? ClearanceNotes = null
);
