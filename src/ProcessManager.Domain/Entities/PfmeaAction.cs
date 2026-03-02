using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A corrective or preventive action taken to reduce the risk of a PFMEA failure mode.
/// Captures simple tracking (person, date, status) plus before/after risk comparison.
/// </summary>
public class PfmeaAction : BaseEntity
{
    public Guid FailureModeId { get; set; }

    /// <summary>Description of the action to be taken.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Person responsible for completing the action (free text).</summary>
    public string? ResponsiblePerson { get; set; }

    /// <summary>Target completion date.</summary>
    public DateTime? TargetDate { get; set; }

    public PfmeaActionStatus Status { get; set; } = PfmeaActionStatus.Open;

    /// <summary>Date the action was actually completed.</summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>Evidence of effectiveness or notes on what was done.</summary>
    public string? CompletionNotes { get; set; }

    // ── Revised risk ratings (post-action) ───────────────────────────────

    /// <summary>
    /// Revised Occurrence after the action is completed.
    /// Null until the action is completed and re-evaluated.
    /// </summary>
    public int? RevisedOccurrence { get; set; }

    /// <summary>
    /// Revised Detection after the action is completed.
    /// Null until the action is completed and re-evaluated.
    /// </summary>
    public int? RevisedDetection { get; set; }

    /// <summary>
    /// Revised RPN = Severity (from parent failure mode) × RevisedOccurrence × RevisedDetection.
    /// Returns null until both revised ratings are present.
    /// </summary>
    public int? RevisedRpn =>
        RevisedOccurrence.HasValue && RevisedDetection.HasValue
            ? FailureMode?.Severity * RevisedOccurrence * RevisedDetection
            : null;

    // Navigation
    public PfmeaFailureMode FailureMode { get; set; } = null!;
}
