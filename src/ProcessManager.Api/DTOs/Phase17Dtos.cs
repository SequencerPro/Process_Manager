namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 17 — Standards Conformance Management ────────────

// ── Standards Clause DTOs ────────────────────────────────────────────────────

public record StandardsClauseDto(
    Guid     Id,
    string   Standard,
    string   ClauseNumber,
    string   Title,
    string   RequirementSummary,
    bool     IsAs9100Addition,
    int      EvidenceLinkCount,
    int      OpenFindingCount,
    string   CoverageStatus
);

public record StandardsClauseSummaryDto(
    Guid     Id,
    string   Standard,
    string   ClauseNumber,
    string   Title,
    bool     IsAs9100Addition,
    int      EvidenceLinkCount,
    string   CoverageStatus
);

// ── Clause Evidence Link DTOs ────────────────────────────────────────────────

public record ClauseEvidenceLinkDto(
    Guid     Id,
    Guid     ClauseId,
    string   ClauseNumber,
    string   ClauseTitle,
    string   EntityType,
    Guid     EntityId,
    string?  EntityName,
    string?  EvidenceNote,
    bool     IsAutoLinked,
    DateTime CreatedAt
);

public record CreateClauseEvidenceLinkDto(
    Guid     ClauseId,
    string   EntityType,
    Guid     EntityId,
    string?  EvidenceNote
);

// ── Audit Program DTOs ───────────────────────────────────────────────────────

public record AuditProgramDto(
    Guid     Id,
    string   Name,
    string   Standard,
    int      Year,
    string   LeadAuditor,
    string   Status,
    int      AuditCount,
    int      OpenFindingCount,
    DateTime CreatedAt
);

public record AuditProgramSummaryDto(
    Guid     Id,
    string   Name,
    string   Standard,
    int      Year,
    string   LeadAuditor,
    string   Status,
    int      AuditCount
);

public record CreateAuditProgramDto(
    string   Name,
    string   Standard,
    int      Year,
    string   LeadAuditor
);

public record UpdateAuditProgramDto(
    string   Name,
    string   Standard,
    int      Year,
    string   LeadAuditor
);

// ── Audit DTOs ───────────────────────────────────────────────────────────────

public record AuditDto(
    Guid     Id,
    Guid     ProgramId,
    string   ProgramName,
    string   AuditType,
    string   Scope,
    DateTime PlannedDate,
    DateTime? ActualDate,
    string   LeadAuditor,
    string   Status,
    int      FindingCount,
    int      MajorCount,
    int      MinorCount,
    int      ObservationCount,
    int      OfiCount,
    DateTime CreatedAt
);

public record AuditSummaryDto(
    Guid     Id,
    string   AuditType,
    string   Scope,
    DateTime PlannedDate,
    DateTime? ActualDate,
    string   LeadAuditor,
    string   Status,
    int      FindingCount
);

public record CreateAuditDto(
    Guid     ProgramId,
    string   AuditType,
    string   Scope,
    DateTime PlannedDate,
    string   LeadAuditor
);

public record UpdateAuditDto(
    string   AuditType,
    string   Scope,
    DateTime PlannedDate,
    DateTime? ActualDate,
    string   LeadAuditor
);

// ── Audit Finding DTOs ───────────────────────────────────────────────────────

public record AuditFindingDto(
    Guid     Id,
    Guid     AuditId,
    Guid     ClauseId,
    string   ClauseNumber,
    string   ClauseTitle,
    string   FindingType,
    string   Description,
    string   ObjectiveEvidence,
    string   Status,
    Guid?    ActionItemId,
    string?  ActionItemTitle,
    string?  ActionItemStatus,
    DateTime? ClosedAt,
    string?  ClosureNotes,
    DateTime CreatedAt
);

public record CreateAuditFindingDto(
    Guid     ClauseId,
    string   FindingType,
    string   Description,
    string   ObjectiveEvidence
);

public record CloseAuditFindingDto(
    string   ClosureNotes
);

// ── Conformance Dashboard DTOs ───────────────────────────────────────────────

public record ConformanceDashboardDto(
    int      TotalClauses,
    int      CoveredCount,
    int      PartialCount,
    int      GapCount,
    int      OpenMajorFindingCount,
    int      OpenMinorFindingCount,
    DateTime? NextAuditDate,
    List<StandardsClauseSummaryDto> Clauses
);
