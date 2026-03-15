namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 16 — Training & Competency Management ─────────────

// ── Competency Record DTOs ───────────────────────────────────────────────────

public record CompetencyRecordDto(
    Guid      Id,
    string    UserId,
    string    UserDisplayName,
    Guid      TrainingProcessId,
    string    TrainingProcessCode,
    string    TrainingProcessName,
    string?   CompetencyTitle,         // Process.CompetencyTitle ?? Process.Name
    int       TrainingProcessVersion,
    Guid?     JobId,
    string?   InstructorUserId,
    string?   InstructorDisplayName,
    DateTime  CompletedAt,
    DateTime? ExpiresAt,
    string    Status,                  // CompetencyStatus enum name
    string?   Notes,
    DateTime  CreatedAt,
    string?   CreatedBy
);

public record CompetencyRecordSummaryDto(
    Guid      Id,
    string    UserId,
    string    UserDisplayName,
    Guid      TrainingProcessId,
    string    TrainingProcessCode,
    string    TrainingProcessName,
    string?   CompetencyTitle,
    int       TrainingProcessVersion,
    DateTime  CompletedAt,
    DateTime? ExpiresAt,
    string    Status,
    bool      ExpiringSoon             // true if Current and expires within 30 days
);

public record CreateCompetencyRecordDto(
    string    UserId,
    string    UserDisplayName,
    Guid      TrainingProcessId,
    Guid?     JobId,
    string?   InstructorUserId,
    string?   InstructorDisplayName,
    DateTime  CompletedAt,
    string?   Notes
);

// ── Training Requirement DTOs ────────────────────────────────────────────────

public record ProcessTrainingRequirementDto(
    Guid   Id,
    string SubjectType,              // TrainingRequirementSubjectType enum name
    Guid   SubjectEntityId,
    Guid   RequiredTrainingProcessId,
    string TrainingProcessCode,
    string TrainingProcessName,
    string? CompetencyTitle,
    bool   IsEnforced,
    DateTime CreatedAt,
    string? CreatedBy
);

public record AddTrainingRequirementDto(
    string SubjectType,   // "Process" | "StepTemplate"
    Guid   SubjectEntityId,
    Guid   RequiredTrainingProcessId,
    bool   IsEnforced = true
);

// ── Competency Matrix DTOs ───────────────────────────────────────────────────

public record CompetencyMatrixRowDto(
    string UserId,
    string UserDisplayName,
    List<CompetencyMatrixCellDto> Cells  // one per training process in the column set
);

public record CompetencyMatrixCellDto(
    Guid      TrainingProcessId,
    string?   Status,           // "Current" | "Expired" | null (no record)
    DateTime? CompletedAt,
    DateTime? ExpiresAt,
    bool      ExpiringSoon
);

// ── Training Compliance Summary DTOs (for scorecard & management review) ─────

public record TrainingComplianceSummaryDto(
    int TotalRequired,        // distinct (user × training-process) pairs with an enforced requirement
    int Current,
    int Expired,
    int Missing,              // required pairs with no record at all
    int ExpiringSoon,         // Current and expires within 30 days
    double CompliancePct      // Current / TotalRequired * 100
);
