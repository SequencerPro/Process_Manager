using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Tracks a formal document approval cycle for a specific Process revision.
/// Created by the Submit for Approval flow; drives transitions in the Process lifecycle.
/// </summary>
public class DocumentApprovalRequest : BaseEntity
{
    /// <summary>The document (Process) being submitted for approval.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>The version number of the Process being approved.</summary>
    public int ProcessVersion { get; set; }

    /// <summary>The Job that contains the parallel approval step executions.</summary>
    public Guid ApprovalJobId { get; set; }

    /// <summary>Current state of this approval cycle.</summary>
    public DocumentApprovalStatus Status { get; set; } = DocumentApprovalStatus.Pending;

    /// <summary>Display name of the author who submitted the document for approval.</summary>
    public string SubmittedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp of submission.</summary>
    public DateTime SubmittedAt { get; set; }

    // Navigation properties
    public Process Process { get; set; } = null!;
    public Job ApprovalJob { get; set; } = null!;
}
