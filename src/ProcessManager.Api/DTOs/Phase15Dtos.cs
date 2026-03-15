namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 15 — Tiered Accountability & Action Tracking ────

// ── Action Item DTOs ─────────────────────────────────────────────────────────

public record ActionItemDto(
    Guid     Id,
    string   Title,
    string?  Description,
    string   AssignedToUserId,
    string   AssignedToDisplayName,
    string   AssignedByUserId,
    string   AssignedByDisplayName,
    DateTime DueDate,
    string   Priority,
    string   Status,
    string   SourceType,
    Guid?    SourceEntityId,
    string?  CompletedBy,
    DateTime? CompletedAt,
    string?  CompletionNotes,
    string?  VerifiedBy,
    DateTime? VerifiedAt,
    DateTime CreatedAt,
    string?  CreatedBy,
    DateTime? UpdatedAt,
    bool     IsOverdue
);

public record ActionItemSummaryDto(
    Guid     Id,
    string   Title,
    string   AssignedToDisplayName,
    string   AssignedByDisplayName,
    DateTime DueDate,
    string   Priority,
    string   Status,
    string   SourceType,
    Guid?    SourceEntityId,
    DateTime CreatedAt,
    bool     IsOverdue
);

public record CreateActionItemDto(
    string   Title,
    string?  Description,
    string   AssignedToUserId,
    string   AssignedToDisplayName,
    DateTime DueDate,
    string   Priority = "Medium",   // ActionItemPriority enum name
    string   SourceType = "Manual", // ActionItemSourceType enum name
    Guid?    SourceEntityId = null
);

public record UpdateActionItemDto(
    string   Title,
    string?  Description,
    string   AssignedToUserId,
    string   AssignedToDisplayName,
    DateTime DueDate,
    string   Priority
);

public record CompleteActionItemDto(
    string CompletionNotes
);

public record VerifyActionItemDto(
    string? VerificationNotes
);

// ── Management Review DTOs ───────────────────────────────────────────────────

public record ManagementReviewSummaryDto(
    Guid     Id,
    string   Title,
    string   ReviewType,
    DateTime ScheduledDate,
    string   Status,
    string?  ConductedBy,
    int      ActionItemCount,
    DateTime CreatedAt,
    string?  CreatedBy
);

public record ManagementReviewDto(
    Guid     Id,
    string   Title,
    string   ReviewType,
    DateTime ScheduledDate,
    string   Status,
    string?  ConductedBy,
    // Auto-populated inputs
    string?  NcSummary,
    string?  ActionCloseRateSummary,
    string?  MrbSummary,
    // Manual inputs
    string?  CustomerComplaintsNotes,
    string?  SupplierQualityNotes,
    string?  InternalAuditStatus,
    string?  PriorActionsSummary,
    // Outputs
    string?  Decisions,
    string?  NextCycleTargets,
    DateTime CreatedAt,
    string?  CreatedBy,
    DateTime? UpdatedAt,
    int      ActionItemCount
);

public record CreateManagementReviewDto(
    string   Title,
    string   ReviewType,  // ManagementReviewType enum name
    DateTime ScheduledDate
);

public record UpdateManagementReviewDto(
    string   Title,
    string   ReviewType,
    DateTime ScheduledDate,
    string?  ConductedBy,
    string?  CustomerComplaintsNotes,
    string?  SupplierQualityNotes,
    string?  InternalAuditStatus,
    string?  PriorActionsSummary,
    string?  Decisions,
    string?  NextCycleTargets
);

// ── Quality Scorecard DTOs ───────────────────────────────────────────────────

public record QualityScorecardDto(
    int     TotalOpenActionItems,
    int     OverdueActionItems,
    int     ActionItemsCompleteAwaitingVerification,
    double  ActionCloseRatePercent,      // % closed in last 30 days out of all created
    double  AverageDaysToClose,
    int     OpenNonConformances,
    int     OpenMrbReviews,
    int     MrbReviewsOpenOver30Days,
    List<ActionItemAgeGroupDto> ActionItemsByPriority,
    List<ActionItemSourceBreakdownDto> ActionItemsBySource,
    List<ActionItemSummaryDto> TopOverdueItems
);

public record ActionItemAgeGroupDto(string Priority, int Open, int Overdue);

public record ActionItemSourceBreakdownDto(string SourceType, int Open, int Total);
