using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──── Phase 10b: Ishikawa Diagrams ─────────────────────────────────────────

public record IshikawaCauseSummaryDto(
    Guid Id,
    string Category,
    string CauseText,
    Guid? ParentCauseId,
    Guid? RootCauseLibraryEntryId,
    string? LibraryEntryTitle,
    bool IsSelectedRootCause,
    List<IshikawaCauseSummaryDto> SubCauses
);

public record IshikawaDiagramResponseDto(
    Guid Id,
    string Title,
    string ProblemStatement,
    string LinkedEntityType,
    Guid? LinkedEntityId,
    string? CreatedBy,
    string Status,
    DateTime? ClosedAt,
    string? ClosureNotes,
    List<IshikawaCauseSummaryDto> Causes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record IshikawaDiagramSummaryDto(
    Guid Id,
    string Title,
    string ProblemStatement,
    string LinkedEntityType,
    Guid? LinkedEntityId,
    string Status,
    int TotalCauses,
    int SelectedRootCauses,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record IshikawaDiagramCreateDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required, StringLength(2000, MinimumLength = 1)] string ProblemStatement,
    [Required, StringLength(20, MinimumLength = 1)] string LinkedEntityType,
    Guid? LinkedEntityId
);

public record IshikawaDiagramUpdateDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required, StringLength(2000, MinimumLength = 1)] string ProblemStatement
);

public record IshikawaDiagramCloseDto(
    [StringLength(2000)] string? ClosureNotes
);

public record IshikawaCauseCreateDto(
    [Required, StringLength(20, MinimumLength = 1)] string Category,
    [Required, StringLength(500, MinimumLength = 1)] string CauseText,
    Guid? ParentCauseId,
    Guid? RootCauseLibraryEntryId
);

public record IshikawaCauseUpdateDto(
    [Required, StringLength(500, MinimumLength = 1)] string CauseText,
    Guid? RootCauseLibraryEntryId,
    bool IsSelectedRootCause
);

// ──── Phase 10c: Branching 5 Whys ──────────────────────────────────────────

public record FiveWhysNodeDto(
    Guid Id,
    Guid? ParentNodeId,
    string WhyStatement,
    bool IsRootCause,
    Guid? RootCauseLibraryEntryId,
    string? LibraryEntryTitle,
    string? CorrectiveAction,
    List<FiveWhysNodeDto> ChildNodes
);

public record FiveWhysAnalysisResponseDto(
    Guid Id,
    string Title,
    string ProblemStatement,
    string LinkedEntityType,
    Guid? LinkedEntityId,
    string? CreatedBy,
    string Status,
    DateTime? ClosedAt,
    string? ClosureNotes,
    List<FiveWhysNodeDto> Nodes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record FiveWhysAnalysisSummaryDto(
    Guid Id,
    string Title,
    string ProblemStatement,
    string LinkedEntityType,
    Guid? LinkedEntityId,
    string Status,
    int TotalNodes,
    int RootCauseNodes,
    bool HasIncompleteLeaves,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record FiveWhysAnalysisCreateDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required, StringLength(2000, MinimumLength = 1)] string ProblemStatement,
    [Required, StringLength(20, MinimumLength = 1)] string LinkedEntityType,
    Guid? LinkedEntityId
);

public record FiveWhysAnalysisUpdateDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required, StringLength(2000, MinimumLength = 1)] string ProblemStatement
);

public record FiveWhysAnalysisCloseDto(
    [StringLength(2000)] string? ClosureNotes
);

public record FiveWhysNodeCreateDto(
    Guid? ParentNodeId,
    [Required, StringLength(1000, MinimumLength = 1)] string WhyStatement,
    Guid? RootCauseLibraryEntryId
);

public record FiveWhysNodeUpdateDto(
    [Required, StringLength(1000, MinimumLength = 1)] string WhyStatement,
    bool IsRootCause,
    Guid? RootCauseLibraryEntryId,
    [StringLength(2000)] string? CorrectiveAction
);
