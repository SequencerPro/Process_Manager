using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────────── Phase 36.4 — Execution Hardening ────────────────

/// <summary>Request to record entering an execution phase (T4.2).</summary>
public record RecordPhaseDto(
    [Required] ExecutionPhase Phase
);

/// <summary>A single phase visit in a step execution's navigation history.</summary>
public record StepExecutionPhaseEventDto(
    Guid Id,
    Guid StepExecutionId,
    ExecutionPhase Phase,
    DateTime EnteredAt,
    DateTime? ExitedAt,
    double? DurationSeconds,
    string? OperatorUserId
);

/// <summary>
/// Consolidated rehydration payload for resuming a step execution on another
/// device (T4.4). Everything the wizard needs to land the operator on the
/// right phase with their saved data.
/// </summary>
public record StepExecutionResumeDto(
    Guid StepExecutionId,
    Guid JobId,
    string Status,
    ExecutionPhase CurrentPhase,
    int SavedPromptResponseCount,
    int PortTransactionCount,
    bool IsResumable,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

/// <summary>Per-phase timing aggregate (T4.5).</summary>
public record PhaseTimingStatDto(
    ExecutionPhase Phase,
    int SampleCount,
    double TotalSeconds,
    double MeanSeconds,
    double MedianSeconds,
    double StdDevSeconds
);

public record PhaseTimingOutlierDto(
    Guid PhaseEventId,
    Guid StepExecutionId,
    ExecutionPhase Phase,
    double DurationSeconds,
    double ThresholdSeconds
);

public record PhaseTimingReportDto(
    List<PhaseTimingStatDto> PerPhase,
    List<PhaseTimingOutlierDto> Outliers
);
