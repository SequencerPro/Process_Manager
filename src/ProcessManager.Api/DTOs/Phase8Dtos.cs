namespace ProcessManager.Api.DTOs;

// ──────────────────── Phase 8 — Maturity Scoring ────────────────────

public enum MaturityLevel
{
    Draft,       // 0–49 or any Error rule failure
    Developing,  // 50–79
    Defined,     // 80–99
    Optimised    // 100
}

public enum MaturityRuleOutcome
{
    Pass,
    Warn,
    Fail
}

public record MaturityRuleResultDto(
    string RuleId,
    string Description,
    MaturityRuleOutcome Outcome,
    string? RemediationHint
);

public record MaturityReportDto(
    Guid StepTemplateId,
    string StepTemplateCode,
    string StepTemplateName,
    int Score,
    MaturityLevel Level,
    bool HasBlockingErrors,
    List<MaturityRuleResultDto> Rules
);

/// <summary>Lightweight summary for embedding in list views.</summary>
public record MaturitySummaryDto(
    int Score,
    MaturityLevel Level,
    bool HasBlockingErrors
);

// ──────────────────── Phase 8c — Non-Conformance Disposition ────────────────────

public record NonConformanceResponseDto(
    Guid Id,
    Guid StepExecutionId,
    Guid ContentBlockId,
    string? ContentBlockLabel,
    string? StepName,
    string? JobCode,
    string? ActualValue,
    string LimitType,
    string DispositionStatus,
    string? DisposedBy,
    DateTime? DisposedAt,
    string? JustificationText,
    bool MrbRequired,
    Guid? MrbReviewId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateNonConformanceDto(
    Guid StepExecutionId,
    Guid ContentBlockId,
    string? ActualValue,
    string LimitType
);

public record DispositionNonConformanceDto(
    string DispositionStatus,     // Rework | Scrap | Quarantine | UseAsIs
    string? DisposedBy = null,
    string? JustificationText = null
);
