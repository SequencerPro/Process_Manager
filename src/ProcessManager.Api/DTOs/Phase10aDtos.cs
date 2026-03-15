using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Root Cause Library (Phase 10a) ────────────────────

public record RootCauseEntryResponseDto(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string? Tags,
    string? CorrectiveActionTemplate,
    int UsageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RootCauseEntryCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    [Required, StringLength(20, MinimumLength = 1)] string Category,
    [StringLength(500)] string? Tags,
    [StringLength(2000)] string? CorrectiveActionTemplate
);

public record RootCauseEntryUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    [Required, StringLength(20, MinimumLength = 1)] string Category,
    [StringLength(500)] string? Tags,
    [StringLength(2000)] string? CorrectiveActionTemplate
);

// ──────────────────── Unified RCA Analysis Index (Phase 10a enhancement) ────────────────────

/// <summary>
/// A single row in the unified analysis index — covers both 5 Whys and Ishikawa analyses.
/// </summary>
public record RcaAnalysisIndexItemDto(
    Guid Id,
    string AnalysisType,       // "FiveWhys" | "Ishikawa"
    string Title,
    string ProblemStatement,
    string LinkedEntityType,
    Guid? LinkedEntityId,
    string Status,
    int TotalNodes,
    int ConfirmedRootCauses,
    DateTime CreatedAt
);
