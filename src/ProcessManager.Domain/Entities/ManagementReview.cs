using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A formal periodic quality management review (ISO 9001 clause 9.3).
/// Captures structured inputs, decisions, and generates action items.
/// </summary>
public class ManagementReview : BaseEntity
{
    public string Title { get; set; } = "";

    public ManagementReviewType ReviewType { get; set; } = ManagementReviewType.Quarterly;

    public DateTime ScheduledDate { get; set; }

    public ManagementReviewStatus Status { get; set; } = ManagementReviewStatus.Scheduled;

    public string? ConductedBy { get; set; }

    // ── Auto-populated inputs (set by system at review time) ─────────────────

    /// <summary>Text summary of NC counts by period — auto-populated at review start.</summary>
    public string? NcSummary { get; set; }

    /// <summary>Action item close rate % as text — auto-populated at review start.</summary>
    public string? ActionCloseRateSummary { get; set; }

    /// <summary>Open MRB count and average age — auto-populated at review start.</summary>
    public string? MrbSummary { get; set; }

    /// <summary>Training compliance % and expired/expiring counts — auto-populated at review start.</summary>
    public string? TrainingComplianceSummary { get; set; }

    // ── Manual supplementary inputs ──────────────────────────────────────────

    public string? CustomerComplaintsNotes { get; set; }
    public string? SupplierQualityNotes { get; set; }
    public string? InternalAuditStatus { get; set; }
    public string? PriorActionsSummary { get; set; }

    // ── Outputs ──────────────────────────────────────────────────────────────

    /// <summary>Strategic decisions and direction from the review meeting.</summary>
    public string? Decisions { get; set; }

    /// <summary>Performance targets set for the next review cycle.</summary>
    public string? NextCycleTargets { get; set; }
}
