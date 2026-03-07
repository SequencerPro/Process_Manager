namespace ProcessManager.Domain.Entities;

/// <summary>
/// Permanent audit record of a submission and approval/rejection decision for a Process or StepTemplate.
/// One record is created per submission; if rejected and resubmitted, a new record is created.
/// </summary>
public class ApprovalRecord : BaseEntity
{
    /// <summary>Discriminator: "Process" or "StepTemplate".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>The Process or StepTemplate that was submitted.</summary>
    public Guid EntityId { get; set; }

    /// <summary>The version number of the entity at the time of submission.</summary>
    public int EntityVersion { get; set; }

    /// <summary>Username of the person who submitted for approval.</summary>
    public string SubmittedBy { get; set; } = string.Empty;

    /// <summary>When the submission was made.</summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>Username of the approver who reviewed (null if still pending).</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>When the review decision was made.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Review outcome: "Pending", "Approved", or "Rejected".</summary>
    public string Decision { get; set; } = "Pending";

    /// <summary>Approver notes or rejection reason.</summary>
    public string? Notes { get; set; }

    // Navigation — set based on EntityType
    public Guid? ProcessId { get; set; }
    public Process? Process { get; set; }

    public Guid? StepTemplateId { get; set; }
    public StepTemplate? StepTemplate { get; set; }
}
