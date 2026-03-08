using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

public record ProcessValidationResultDto(
    List<string> Errors,
    List<string> Warnings
);

// ──────────────────── Process ────────────────────

public record ProcessCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description
);

public record ProcessUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool? IsActive = null
);

public record ProcessResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    string Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ProcessStepResponseDto> Steps,
    List<FlowResponseDto> Flows
);

/// <summary>
/// Lightweight process DTO for list endpoints (no Steps/Flows graph).
/// </summary>
public record ProcessSummaryResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    string Status,
    bool IsActive,
    int StepCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── ProcessStep ────────────────────

public record ProcessStepCreateDto(
    Guid StepTemplateId,
    [Range(1, int.MaxValue)] int Sequence,
    [StringLength(200)] string? NameOverride,
    [StringLength(2000)] string? DescriptionOverride,
    StepPattern? PatternOverride = null,
    List<ProcessStepPortOverrideDto>? PortOverrides = null
);

public record ProcessStepUpdateDto(
    [Range(1, int.MaxValue)] int Sequence,
    [StringLength(200)] string? NameOverride,
    [StringLength(2000)] string? DescriptionOverride,
    StepPattern? PatternOverride = null,
    List<ProcessStepPortOverrideDto>? PortOverrides = null
);

public record ProcessStepResponseDto(
    Guid Id,
    Guid ProcessId,
    Guid StepTemplateId,
    string StepTemplateCode,
    string StepTemplateName,
    int Sequence,
    string? NameOverride,
    string? DescriptionOverride,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    MaturitySummaryDto? StepTemplateMaturity = null,
    StepPattern? PatternOverride = null,
    List<ProcessStepPortOverrideResponseDto>? PortOverrides = null
);

// ──────────────────── ProcessStepPortOverride ────────────────────

public record ProcessStepPortOverrideDto(
    Guid PortId,
    [StringLength(200)] string? NameOverride = null,
    PortDirection? DirectionOverride = null,
    Guid? KindIdOverride = null,
    Guid? GradeIdOverride = null,
    QuantityRuleMode? QtyRuleModeOverride = null,
    int? QtyRuleNOverride = null,
    int? SortOrderOverride = null
);

public record ProcessStepPortOverrideResponseDto(
    Guid Id,
    Guid PortId,
    string? NameOverride,
    PortDirection? DirectionOverride,
    Guid? KindIdOverride,
    string? KindOverrideName,
    Guid? GradeIdOverride,
    string? GradeOverrideName,
    QuantityRuleMode? QtyRuleModeOverride,
    int? QtyRuleNOverride,
    int? SortOrderOverride
);

// ──────────────────── ProcessStepContent ────────────────────

public record ProcessStepContentResponseDto(
    Guid Id,
    Guid ProcessStepId,
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
    // Phase 8a fields
    string? ContentCategory = null,
    bool AcknowledgmentRequired = false,
    decimal? NominalValue = null,
    bool IsHardLimit = false
);

public record AddTextBlockDto(
    [Required, StringLength(10000, MinimumLength = 1)] string Body
);

public record UpdateTextBlockDto(
    [Required, StringLength(10000, MinimumLength = 1)] string Body
);

public record AddPromptBlockDto(
    [Required, StringLength(500, MinimumLength = 1)] string Label,
    [Required] string PromptType,
    bool IsRequired = true,
    [StringLength(50)] string? Units = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    [StringLength(4000)] string? Choices = null
);

public record UpdatePromptBlockDto(
    [Required, StringLength(500, MinimumLength = 1)] string Label,
    bool IsRequired = true,
    [StringLength(50)] string? Units = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    [StringLength(4000)] string? Choices = null
);

public record ReorderContentBlocksDto(
    List<Guid> OrderedIds
);

// ──────────────────── PromptResponse ────────────────────

public record PromptResponseDto(
    Guid Id,
    Guid StepExecutionId,
    Guid? ProcessStepContentId,
    Guid? StepTemplateContentId,
    string Label,
    string PromptType,
    string ResponseValue,
    bool IsOutOfRange,
    string? OverrideNote,
    DateTime RespondedAt
);

public record SavePromptResponsesDto(
    [Required] List<PromptResponseItemDto> Responses
);

public record PromptResponseItemDto(
    Guid? ProcessStepContentId,
    Guid? StepTemplateContentId,
    [Required, StringLength(1000)] string ResponseValue,
    [StringLength(2000)] string? OverrideNote = null
);

// ──────────────────── Flow ────────────────────

public record FlowCreateDto(
    Guid SourceProcessStepId,
    Guid SourcePortId,
    Guid TargetProcessStepId,
    Guid TargetPortId
);

public record FlowResponseDto(
    Guid Id,
    Guid ProcessId,
    Guid SourceProcessStepId,
    Guid SourcePortId,
    string SourcePortName,
    Guid TargetProcessStepId,
    Guid TargetPortId,
    string TargetPortName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
