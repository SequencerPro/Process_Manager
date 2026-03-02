using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Kind ────────────────────

public record KindCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsSerialized,
    bool IsBatchable
);

public record KindUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsSerialized,
    bool IsBatchable
);

public record KindResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSerialized,
    bool IsBatchable,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<GradeResponseDto> Grades
);

// ──────────────────── Grade ────────────────────

public record GradeCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsDefault,
    [Range(0, int.MaxValue)] int SortOrder
);

public record GradeUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsDefault,
    [Range(0, int.MaxValue)] int SortOrder
);

public record GradeResponseDto(
    Guid Id,
    Guid KindId,
    string Code,
    string Name,
    string? Description,
    bool IsDefault,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── DomainVocabulary ────────────────────

public record DomainVocabularyCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(100, MinimumLength = 1)] string TermKind,
    [Required, StringLength(100, MinimumLength = 1)] string TermKindCode,
    [Required, StringLength(100, MinimumLength = 1)] string TermGrade,
    [Required, StringLength(100, MinimumLength = 1)] string TermItem,
    [Required, StringLength(100, MinimumLength = 1)] string TermItemId,
    [Required, StringLength(100, MinimumLength = 1)] string TermBatch,
    [Required, StringLength(100, MinimumLength = 1)] string TermBatchId,
    [Required, StringLength(100, MinimumLength = 1)] string TermJob,
    [Required, StringLength(100, MinimumLength = 1)] string TermWorkflow,
    [Required, StringLength(100, MinimumLength = 1)] string TermProcess,
    [Required, StringLength(100, MinimumLength = 1)] string TermStep
);

public record DomainVocabularyUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(100, MinimumLength = 1)] string TermKind,
    [Required, StringLength(100, MinimumLength = 1)] string TermKindCode,
    [Required, StringLength(100, MinimumLength = 1)] string TermGrade,
    [Required, StringLength(100, MinimumLength = 1)] string TermItem,
    [Required, StringLength(100, MinimumLength = 1)] string TermItemId,
    [Required, StringLength(100, MinimumLength = 1)] string TermBatch,
    [Required, StringLength(100, MinimumLength = 1)] string TermBatchId,
    [Required, StringLength(100, MinimumLength = 1)] string TermJob,
    [Required, StringLength(100, MinimumLength = 1)] string TermWorkflow,
    [Required, StringLength(100, MinimumLength = 1)] string TermProcess,
    [Required, StringLength(100, MinimumLength = 1)] string TermStep
);

public record DomainVocabularyResponseDto(
    Guid Id,
    string Name,
    string TermKind,
    string TermKindCode,
    string TermGrade,
    string TermItem,
    string TermItemId,
    string TermBatch,
    string TermBatchId,
    string TermJob,
    string TermWorkflow,
    string TermProcess,
    string TermStep,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
