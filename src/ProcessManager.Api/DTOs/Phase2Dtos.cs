using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────────── StepTemplate ────────────────────

public record StepTemplateCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    StepPattern Pattern,
    List<PortCreateDto>? Ports = null,
    bool IsShared = true
);

public record StepTemplateUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    StepPattern Pattern,
    bool? IsActive = null
);

public record StepTemplateResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    StepPattern Pattern,
    int Version,
    string Status,
    bool IsActive,
    bool IsShared,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PortResponseDto> Ports,
    List<StepTemplateImageResponseDto> Images,
    MaturitySummaryDto? Maturity = null,
    int? ExpectedDurationMinutes = null,
    Guid? RequiredEquipmentCategoryId = null,
    string? RequiredEquipmentCategoryName = null
);

public record StepTemplateImageResponseDto(
    Guid Id,
    Guid StepTemplateId,
    string FileName,
    string OriginalFileName,
    string MimeType,
    int SortOrder,
    string Url,
    DateTime CreatedAt
);

public record StepTemplateContentResponseDto(
    Guid Id,
    Guid StepTemplateId,
    string ContentType,
    int SortOrder,
    string? Body,
    string? FileName,
    string? OriginalFileName,
    string? MimeType,
    string? ImageUrl,
    DateTime CreatedAt,
    // Prompt fields — null for Text/Image blocks
    string? PromptType,
    string? Label,
    bool IsRequired,
    string? Units,
    decimal? MinValue,
    decimal? MaxValue,
    string? Choices,
    // Phase 8a — categorisation + spec enrichment
    string? ContentCategory,
    bool AcknowledgmentRequired,
    decimal? NominalValue,
    bool IsHardLimit
);

public record AddStepTemplateTextBlockDto(
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.StringLength(10000, MinimumLength = 1)] string Body,
    string? ContentCategory = null
);

public record UpdateStepTemplateTextBlockDto(
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.StringLength(10000, MinimumLength = 1)] string Body,
    string? ContentCategory = null
);

public record AddStepTemplatePromptBlockDto(
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 1)] string Label,
    [System.ComponentModel.DataAnnotations.Required] string PromptType,
    bool IsRequired = true,
    [System.ComponentModel.DataAnnotations.StringLength(50)] string? Units = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    [System.ComponentModel.DataAnnotations.StringLength(4000)] string? Choices = null,
    string? ContentCategory = null,
    decimal? NominalValue = null,
    bool IsHardLimit = false
);

public record UpdateStepTemplatePromptBlockDto(
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 1)] string Label,
    bool IsRequired = true,
    [System.ComponentModel.DataAnnotations.StringLength(50)] string? Units = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    [System.ComponentModel.DataAnnotations.StringLength(4000)] string? Choices = null,
    string? ContentCategory = null,
    decimal? NominalValue = null,
    bool IsHardLimit = false
);

public record ReorderStepTemplateContentBlocksDto(
    [System.ComponentModel.DataAnnotations.Required] List<Guid> OrderedIds
);

/// <summary>PATCH payload for updating the ContentCategory of any block type (including images).</summary>
public record PatchContentCategoryDto(
    string? ContentCategory,
    bool? AcknowledgmentRequired = null
);

// ──────────────────── Port ────────────────────

public record PortCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    PortDirection Direction,
    PortType PortType,
    // Material-only
    Guid? KindId,
    Guid? GradeId,
    QuantityRuleMode? QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    // Parameter / Characteristic
    DataValueType? DataType,
    [StringLength(50)] string? Units,
    [StringLength(200)] string? NominalValue,
    [StringLength(100)] string? LowerTolerance,
    [StringLength(100)] string? UpperTolerance,
    [Range(0, int.MaxValue)] int SortOrder
);

public record PortUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    PortType PortType,
    // Material-only
    Guid? KindId,
    Guid? GradeId,
    QuantityRuleMode? QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    // Parameter / Characteristic
    DataValueType? DataType,
    [StringLength(50)] string? Units,
    [StringLength(200)] string? NominalValue,
    [StringLength(100)] string? LowerTolerance,
    [StringLength(100)] string? UpperTolerance,
    [Range(0, int.MaxValue)] int SortOrder
);

public record PortResponseDto(
    Guid Id,
    Guid StepTemplateId,
    string Name,
    PortDirection Direction,
    PortType PortType,
    // Material-only (null for non-Material ports)
    Guid? KindId,
    string? KindCode,
    string? KindName,
    Guid? GradeId,
    string? GradeCode,
    string? GradeName,
    QuantityRuleMode? QtyRuleMode,
    int? QtyRuleN,
    int? QtyRuleMin,
    int? QtyRuleMax,
    // Parameter / Characteristic (null for Material and Condition ports)
    DataValueType? DataType,
    string? Units,
    string? NominalValue,
    string? LowerTolerance,
    string? UpperTolerance,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
