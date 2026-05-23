using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An audit record of an operator entering a phase of the Execution Wizard for
/// a given <see cref="StepExecution"/>. One row per visit — revisiting an
/// earlier phase (allowed before sign-off) creates a new row, so the full
/// navigation history is preserved.
///
/// Phase 36.4 (T4.2). Feeds the time-on-phase telemetry in T4.5.
/// </summary>
public class StepExecutionPhaseEvent : BaseEntity
{
    /// <summary>The step execution this phase visit belongs to.</summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>Which phase was entered.</summary>
    public ExecutionPhase Phase { get; set; }

    /// <summary>When the operator entered this phase.</summary>
    public DateTime EnteredAt { get; set; }

    /// <summary>
    /// When the operator left this phase. Null while the phase is the operator's
    /// current location. Set automatically when a new phase event is recorded.
    /// </summary>
    public DateTime? ExitedAt { get; set; }

    /// <summary>Identity user id of the operator who performed this visit.</summary>
    public string? OperatorUserId { get; set; }

    // Navigation
    public StepExecution StepExecution { get; set; } = null!;

    /// <summary>Duration of this phase visit, if it has been exited.</summary>
    public TimeSpan? Duration => ExitedAt.HasValue ? ExitedAt.Value - EnteredAt : null;
}
