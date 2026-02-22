using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A record of a Step being performed (or pending) within a Job.
/// Auto-created when a Job is created.
/// </summary>
public class StepExecution : BaseEntity
{
    /// <summary>The Job this execution belongs to.</summary>
    public Guid JobId { get; set; }

    /// <summary>Which step in the process.</summary>
    public Guid ProcessStepId { get; set; }

    /// <summary>Mirrors ProcessStep.Sequence for ordering.</summary>
    public int Sequence { get; set; }

    /// <summary>Current execution state.</summary>
    public StepExecutionStatus Status { get; set; } = StepExecutionStatus.Pending;

    /// <summary>When execution began.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When execution finished.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Operator notes / observations.</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public ProcessStep ProcessStep { get; set; } = null!;
    public ICollection<PortTransaction> PortTransactions { get; set; } = new List<PortTransaction>();
    public ICollection<ExecutionData> ExecutionData { get; set; } = new List<ExecutionData>();
}
