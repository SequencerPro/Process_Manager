using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Pfmea ────────────────────

public record PfmeaCreateDto(
    Guid ProcessId,
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description
);

public record PfmeaUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    bool? IsActive = null
);

public record PfmeaSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    Guid ProcessId,
    string ProcessName,
    string ProcessCode,
    int FailureModeCount,
    int OpenActionCount,
    int HighestRpn,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PfmeaResponseDto(
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
    List<PfmeaFailureModeResponseDto> FailureModes
);

// ──────────────────── PfmeaFailureMode ────────────────────

public record PfmeaFailureModeCreateDto(
    Guid ProcessStepId,
    [Required, StringLength(500, MinimumLength = 1)] string StepFunction,
    [Required, StringLength(500, MinimumLength = 1)] string FailureMode,
    [Required, StringLength(500, MinimumLength = 1)] string FailureEffect,
    [StringLength(500)] string? FailureCause,
    [StringLength(1000)] string? PreventionControls,
    [StringLength(1000)] string? DetectionControls,
    [Range(1, 10)] int Severity = 1,
    [Range(1, 10)] int Occurrence = 1,
    [Range(1, 10)] int Detection = 1
);

public record PfmeaFailureModeUpdateDto(
    [Required, StringLength(500, MinimumLength = 1)] string StepFunction,
    [Required, StringLength(500, MinimumLength = 1)] string FailureMode,
    [Required, StringLength(500, MinimumLength = 1)] string FailureEffect,
    [StringLength(500)] string? FailureCause,
    [StringLength(1000)] string? PreventionControls,
    [StringLength(1000)] string? DetectionControls,
    [Range(1, 10)] int Severity,
    [Range(1, 10)] int Occurrence,
    [Range(1, 10)] int Detection
);

public record PfmeaFailureModeResponseDto(
    Guid Id,
    Guid PfmeaId,
    Guid ProcessStepId,
    string ProcessStepName,
    int ProcessStepSequence,
    string StepFunction,
    string FailureMode,
    string FailureEffect,
    string? FailureCause,
    string? PreventionControls,
    string? DetectionControls,
    int Severity,
    int Occurrence,
    int Detection,
    int Rpn,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PfmeaActionResponseDto> Actions
);

// ──────────────────── PfmeaAction ────────────────────

public record PfmeaActionCreateDto(
    [Required, StringLength(1000, MinimumLength = 1)] string Description,
    [StringLength(200)] string? ResponsiblePerson,
    DateTime? TargetDate
);

public record PfmeaActionUpdateDto(
    [Required, StringLength(1000, MinimumLength = 1)] string Description,
    [StringLength(200)] string? ResponsiblePerson,
    DateTime? TargetDate,
    PfmeaActionStatus Status,
    DateTime? CompletedDate,
    [StringLength(2000)] string? CompletionNotes,
    [Range(1, 10)] int? RevisedOccurrence,
    [Range(1, 10)] int? RevisedDetection
);

public record PfmeaActionResponseDto(
    Guid Id,
    Guid FailureModeId,
    string Description,
    string? ResponsiblePerson,
    DateTime? TargetDate,
    string Status,
    DateTime? CompletedDate,
    string? CompletionNotes,
    int? RevisedOccurrence,
    int? RevisedDetection,
    int? RevisedRpn,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── CeMatrix ────────────────────

public record CeMatrixCreateDto(
    Guid ProcessStepId,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description
);

public record CeMatrixUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description
);

public record CeMatrixSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid ProcessStepId,
    string ProcessStepName,
    int ProcessStepSequence,
    int InputCount,
    int OutputCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CeMatrixResponseDto(
    Guid Id,
    string Name,
    string? Description,
    Guid ProcessStepId,
    string ProcessStepName,
    int ProcessStepSequence,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CeInputResponseDto> Inputs,
    List<CeOutputResponseDto> Outputs,
    List<CeCorrelationResponseDto> Correlations
);

// ──────────────────── CeInput ────────────────────

public record CeInputCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    CeInputCategory Category,
    Guid? PortId = null,
    int SortOrder = 0
);

public record CeInputUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    CeInputCategory Category,
    int SortOrder
);

public record CeInputResponseDto(
    Guid Id,
    Guid CeMatrixId,
    string Name,
    string Category,
    Guid? PortId,
    string? PortName,
    int SortOrder,
    int PriorityScore,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── CeOutput ────────────────────

public record CeOutputCreateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    CeOutputCategory Category,
    [Range(1, 10)] int Importance = 5,
    Guid? PortId = null,
    int SortOrder = 0
);

public record CeOutputUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    CeOutputCategory Category,
    [Range(1, 10)] int Importance,
    int SortOrder
);

public record CeOutputResponseDto(
    Guid Id,
    Guid CeMatrixId,
    string Name,
    string Category,
    Guid? PortId,
    string? PortName,
    int Importance,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ──────────────────── CeCorrelation ────────────────────

/// <summary>
/// Upsert DTO: creates or updates the correlation score for an input/output pair.
/// Valid scores: 0, 1, 3, 9.
/// </summary>
public record CeCorrelationUpsertDto(
    Guid CeInputId,
    Guid CeOutputId,
    [Range(0, 9)] int Score
);

public record CeCorrelationResponseDto(
    Guid Id,
    Guid CeInputId,
    Guid CeOutputId,
    int Score
);
