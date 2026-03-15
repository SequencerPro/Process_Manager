using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Document Approval Request ────────────────────

public record DocumentApprovalRequestDto(
    Guid Id,
    Guid ProcessId,
    string ProcessCode,
    string ProcessName,
    int ProcessVersion,
    Guid ApprovalJobId,
    string JobCode,
    string Status,
    string SubmittedBy,
    DateTime SubmittedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── Submit for Approval ────────────────────

public record DocumentSubmitForApprovalDto(
    /// <summary>The document Process being submitted.</summary>
    Guid ProcessId,

    /// <summary>Summary of what changed in this revision. Stored on Process.ChangeDescription.</summary>
    [Required, StringLength(2000, MinimumLength = 1)] string ChangeDescription,

    /// <summary>Optional human-readable revision label (e.g. "Rev B", "1.0"). Stored on Process.RevisionCode.</summary>
    [StringLength(20)] string? RevisionCode,

    /// <summary>When the released revision becomes effective. Null defaults to approval timestamp.</summary>
    DateTime? EffectiveDate,

    /// <summary>Per-step user assignments. Key = ProcessStepId, Value = Identity user Id.</summary>
    Dictionary<Guid, string> StepAssignments
);

// ──────────────────── Admin Release ────────────────────

public record AdminReleaseDocumentDto(
    [StringLength(2000)] string? ChangeDescription,
    [StringLength(20)] string? RevisionCode,
    DateTime? EffectiveDate
);
