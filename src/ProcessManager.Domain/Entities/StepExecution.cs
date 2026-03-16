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

    /// <summary>
    /// Parallel execution group. Executions sharing the same non-zero group value start simultaneously.
    /// 0 = sequential (default). Approval steps all share group 1.
    /// </summary>
    public int ParallelGroup { get; set; } = 0;

    /// <summary>Identity user Id of the person assigned to execute this step. Set at job creation; overridable by the submitting author.</summary>
    public string? AssignedToUserId { get; set; }

    /// <summary>The specific machine this execution ran on (Phase 11b).</summary>
    public Guid? EquipmentId { get; set; }

    // Navigation properties
    public Equipment? Equipment { get; set; }
    public Job Job { get; set; } = null!;
    public ProcessStep ProcessStep { get; set; } = null!;
    public ICollection<PortTransaction> PortTransactions { get; set; } = new List<PortTransaction>();
    public ICollection<ExecutionData> ExecutionData { get; set; } = new List<ExecutionData>();
    public ICollection<PromptResponse> PromptResponses { get; set; } = new List<PromptResponse>();
    public ICollection<NonConformance> NonConformances { get; set; } = new List<NonConformance>();
}
