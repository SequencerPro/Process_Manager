using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// The overarching work order that drives items through a process.
/// A Job ties together the entire execution lifecycle.
/// </summary>
public class Job : BaseEntity
{
    /// <summary>Short identifier (e.g., "JOB-2026-001").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Purpose/scope of this job.</summary>
    public string? Description { get; set; }

    /// <summary>The Process being executed.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>The Process version this Job was started against (pinned at creation).</summary>
    public int ProcessVersion { get; set; } = 1;

    /// <summary>Current lifecycle state.</summary>
    public JobStatus Status { get; set; } = JobStatus.Created;

    /// <summary>Relative priority (higher = more urgent).</summary>
    public int Priority { get; set; }

    /// <summary>When work actually began.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When job finished (completed or cancelled).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Populated only for approval routing jobs created by the Submit for Approval flow.</summary>
    public Guid? DocumentApprovalRequestId { get; set; }

    /// <summary>The Workorder this job belongs to (null for standalone jobs).</summary>
    public Guid? WorkorderId { get; set; }

    /// <summary>Committed delivery date for this job (Phase 11a).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>When the job is expected to begin (Phase 11a).</summary>
    public DateTime? PlannedStartDate { get; set; }

    // Navigation properties
    public Process Process { get; set; } = null!;
    public Workorder? Workorder { get; set; }
    public ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
