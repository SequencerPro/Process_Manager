using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Kind ────────────────────

public record KindCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsSerialized,
    bool IsBatchable,
    // Extended properties
    KindSourceType SourceType = KindSourceType.Make,
    [StringLength(50)] string? UnitOfMeasure = null,
    decimal? Cost = null,
    decimal? Price = null,
    [StringLength(200)] string? VendorName = null,
    [StringLength(100)] string? VendorPartNumber = null,
    int? LeadTimeDays = null,
    decimal? Weight = null,
    [StringLength(20)] string? WeightUnit = null,
    [StringLength(50)] string? RohsStatus = null,
    [StringLength(100)] string? CountryOfOrigin = null,
    [StringLength(50)] string? Revision = null,
    [StringLength(2000)] string? Notes = null,
    // Reorder thresholds
    decimal? ReorderThreshold = null,
    decimal? ReorderQuantity = null
);

public record KindUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool IsSerialized,
    bool IsBatchable,
    // Extended properties
    KindSourceType SourceType = KindSourceType.Make,
    [StringLength(50)] string? UnitOfMeasure = null,
    decimal? Cost = null,
    decimal? Price = null,
    [StringLength(200)] string? VendorName = null,
    [StringLength(100)] string? VendorPartNumber = null,
    int? LeadTimeDays = null,
    decimal? Weight = null,
    [StringLength(20)] string? WeightUnit = null,
    [StringLength(50)] string? RohsStatus = null,
    [StringLength(100)] string? CountryOfOrigin = null,
    [StringLength(50)] string? Revision = null,
    [StringLength(2000)] string? Notes = null,
    // Reorder thresholds
    decimal? ReorderThreshold = null,
    decimal? ReorderQuantity = null
);

public record KindResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSerialized,
    bool IsBatchable,
    // Extended properties
    string SourceType,
    string? UnitOfMeasure,
    decimal? Cost,
    decimal? Price,
    string? VendorName,
    string? VendorPartNumber,
    int? LeadTimeDays,
    decimal? Weight,
    string? WeightUnit,
    string? RohsStatus,
    string? CountryOfOrigin,
    string? Revision,
    string? Notes,
    // Reorder thresholds
    decimal? ReorderThreshold,
    decimal? ReorderQuantity,
    // 3D Model
    string? ModelFileName,
    string? ModelOriginalFileName,
    string? ModelMimeType,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<GradeResponseDto> Grades,
    List<KindDocumentResponseDto> Documents,
    List<BomLineResponseDto> BomLines
);

public record KindDocumentResponseDto(
    Guid Id,
    Guid KindId,
    string FileName,
    string OriginalFileName,
    string MimeType,
    string? Title,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
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

// ──────────────────── BomLine ────────────────────

public record BomLineCreateDto(
    [Required] Guid ComponentKindId,
    [Range(1, int.MaxValue)] int LineNumber,
    [Required, Range(0.0001, (double)decimal.MaxValue)] decimal Quantity,
    [StringLength(50)] string? UnitOfMeasure = null,
    [StringLength(2000)] string? Notes = null,
    int SortOrder = 0
);

public record BomLineUpdateDto(
    [Range(1, int.MaxValue)] int LineNumber,
    [Required, Range(0.0001, (double)decimal.MaxValue)] decimal Quantity,
    [StringLength(50)] string? UnitOfMeasure = null,
    [StringLength(2000)] string? Notes = null,
    int SortOrder = 0
);

public record BomLineResponseDto(
    Guid Id,
    Guid ParentKindId,
    Guid ComponentKindId,
    string ComponentCode,
    string ComponentName,
    string? ComponentUnitOfMeasure,
    decimal? ComponentCost,
    int LineNumber,
    decimal Quantity,
    string? UnitOfMeasure,
    string? Notes,
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
    [Required, StringLength(100, MinimumLength = 1)] string TermStep,
    [Required, StringLength(100, MinimumLength = 1)] string TermWorkorder
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
    [Required, StringLength(100, MinimumLength = 1)] string TermStep,
    [Required, StringLength(100, MinimumLength = 1)] string TermWorkorder
);

public record DomainVocabularyResponseDto(
    Guid Id,
    string Name,
    bool IsActive,
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
    string TermWorkorder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
