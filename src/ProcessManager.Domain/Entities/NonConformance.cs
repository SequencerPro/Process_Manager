using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Records an out-of-specification condition encountered during operator execution.
/// Created automatically when a hard-limit NumericEntry or PassFail prompt is answered out of spec.
/// </summary>
public class NonConformance : BaseEntity
{
    /// <summary>The step execution during which this NC was raised.</summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>The specific content block (ProcessStepContent) that triggered the NC.</summary>
    public Guid ContentBlockId { get; set; }

    /// <summary>The actual value entered by the operator (as a string to support all prompt types).</summary>
    public string? ActualValue { get; set; }

    /// <summary>Which limit was breached, or FailResult for PassFail prompts.</summary>
    public LimitType LimitType { get; set; }

    /// <summary>Current disposition state of this non-conformance.</summary>
    public DispositionStatus DispositionStatus { get; set; } = DispositionStatus.Pending;

    /// <summary>Name of the person who made the disposition decision.</summary>
    public string? DisposedBy { get; set; }

    /// <summary>When the disposition was recorded.</summary>
    public DateTime? DisposedAt { get; set; }

    /// <summary>Justification text (required for UseAsIs disposition).</summary>
    public string? JustificationText { get; set; }

    // Navigation properties
    public StepExecution StepExecution { get; set; } = null!;
    public ProcessStepContent ContentBlock { get; set; } = null!;
}
