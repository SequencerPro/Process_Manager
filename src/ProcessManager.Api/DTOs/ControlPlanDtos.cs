using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────────── ControlPlan ────────────────────

public record ControlPlanCreateDto(
    Guid ProcessId,
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description
);

public record ControlPlanUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool? IsActive = null
);

public record ClearControlPlanStalenessDto(
    [Required, StringLength(200, MinimumLength = 1)] string ClearedBy,
    [StringLength(2000)] string? ClearanceNotes
);

public record ControlPlanSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    Guid ProcessId,
    string ProcessName,
    string ProcessCode,
    int EntryCount,
    bool IsStale,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ControlPlanResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    Guid ProcessId,
    string ProcessName,
    string ProcessCode,
    int ProcessVersion,
    bool IsStale,
    string? StalenessClearedBy,
    DateTime? StalenessClearedAt,
    string? StalenessClearanceNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ControlPlanEntryResponseDto> Entries
);

// ──────────────────── ControlPlanEntry ────────────────────

public record ControlPlanEntryCreateDto(
    Guid ProcessStepId,
    [Required, StringLength(300, MinimumLength = 1)] string CharacteristicName,
    CharacteristicType CharacteristicType,
    [StringLength(500)] string? SpecificationOrTolerance,
    [StringLength(300)] string? MeasurementTechnique,
    [StringLength(200)] string? SampleSize,
    [StringLength(200)] string? SampleFrequency,
    [StringLength(500)] string? ControlMethod,
    [StringLength(1000)] string? ReactionPlan,
    Guid? LinkedPfmeaFailureModeId = null,
    Guid? LinkedPortId = null,
    int SortOrder = 0
);

public record ControlPlanEntryUpdateDto(
    [Required, StringLength(300, MinimumLength = 1)] string CharacteristicName,
    CharacteristicType CharacteristicType,
    [StringLength(500)] string? SpecificationOrTolerance,
    [StringLength(300)] string? MeasurementTechnique,
    [StringLength(200)] string? SampleSize,
    [StringLength(200)] string? SampleFrequency,
    [StringLength(500)] string? ControlMethod,
    [StringLength(1000)] string? ReactionPlan,
    Guid? LinkedPfmeaFailureModeId,
    Guid? LinkedPortId,
    int SortOrder
);

public record ControlPlanEntryResponseDto(
    Guid Id,
    Guid ControlPlanId,
    Guid ProcessStepId,
    string ProcessStepName,
    int ProcessStepSequence,
    string CharacteristicName,
    string CharacteristicType,
    string? SpecificationOrTolerance,
    string? MeasurementTechnique,
    string? SampleSize,
    string? SampleFrequency,
    string? ControlMethod,
    string? ReactionPlan,
    Guid? LinkedPfmeaFailureModeId,
    string? LinkedPfmeaFailureModeDescription,
    Guid? LinkedPortId,
    string? LinkedPortName,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
