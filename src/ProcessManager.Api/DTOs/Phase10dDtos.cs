namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 10d — Material Review Board ────────────────────

// ── Participant DTOs ─────────────────────────────────────────────────────────

public record MrbParticipantDto(
    Guid Id,
    string UserId,
    string DisplayName,
    string Role,
    bool IsRequired,
    string? Assessment,
    DateTime? AssessedAt
);

public record MrbAddParticipantDto(
    string UserId,
    string DisplayName,
    string Role,          // MrbParticipantRole enum name
    bool IsRequired = false
);

public record MrbUpdateAssessmentDto(
    string Assessment
);

// ── Review DTOs ──────────────────────────────────────────────────────────────

public record MrbReviewSummaryDto(
    Guid Id,
    Guid NonConformanceId,
    string? NcJobCode,
    string? NcStepName,
    string? NcActualValue,
    string Status,
    string ItemDescription,
    string? QuantityAffected,
    bool CustomerNotificationRequired,
    bool ScarRequired,
    bool SupplierCaused,
    bool RequiresRca,
    string? DispositionDecision,
    string? DecidedBy,
    DateTime? DecidedAt,
    int ParticipantCount,
    DateTime CreatedAt,
    string? CreatedBy
);

public record MrbReviewResponseDto(
    Guid Id,
    Guid NonConformanceId,
    string? NcJobCode,
    string? NcStepName,
    string? NcContentBlockLabel,
    string? NcActualValue,
    string? NcLimitType,
    string? NcDispositionStatus,
    string Status,
    string ItemDescription,
    string? QuantityAffected,
    string ProblemStatement,
    bool CustomerNotificationRequired,
    bool ScarRequired,
    bool SupplierCaused,
    bool RequiresRca,
    string? LinkedRcaAnalysisType,
    Guid? LinkedRcaId,
    string? DispositionDecision,
    string? DispositionJustification,
    string? DecidedBy,
    DateTime? DecidedAt,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime UpdatedAt,
    List<MrbParticipantDto> Participants
);

public record MrbReviewCreateDto(
    Guid NonConformanceId,
    string ItemDescription,
    string ProblemStatement,
    string? QuantityAffected = null,
    bool CustomerNotificationRequired = false,
    bool ScarRequired = false,
    bool SupplierCaused = false,
    bool RequiresRca = false
);

public record MrbReviewUpdateDto(
    string ItemDescription,
    string ProblemStatement,
    string? QuantityAffected,
    bool CustomerNotificationRequired,
    bool ScarRequired,
    bool SupplierCaused,
    bool RequiresRca
);

public record MrbDecisionDto(
    string DispositionDecision,   // MrbDispositionDecision enum name
    string? DispositionJustification,
    string? DecidedBy
);

public record MrbLinkRcaDto(
    string LinkedRcaAnalysisType,  // MrbLinkedRcaType enum name (Ishikawa | FiveWhys)
    Guid LinkedRcaId
);
