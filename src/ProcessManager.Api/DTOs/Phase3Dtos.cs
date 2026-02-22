using System.ComponentModel.DataAnnotations;

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
    [StringLength(2000)] string? DescriptionOverride
);

public record ProcessStepUpdateDto(
    [Range(1, int.MaxValue)] int Sequence,
    [StringLength(200)] string? NameOverride,
    [StringLength(2000)] string? DescriptionOverride
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
    DateTime UpdatedAt
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
