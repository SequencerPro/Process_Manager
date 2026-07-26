using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A finding raised during an <see cref="Audit"/> against a specific
/// <see cref="StandardsClause"/>. Major/Minor findings create an
/// <see cref="ActionItem"/> for corrective action tracking.
/// </summary>
public class AuditFinding : BaseEntity
{
    public Guid AuditId { get; set; }

    /// <summary>The clause this finding is raised against.</summary>
    public Guid ClauseId { get; set; }

    public FindingType FindingType { get; set; }

    /// <summary>Finding statement.</summary>
    public string Description { get; set; } = "";

    /// <summary>Evidence seen that supports the finding.</summary>
    public string ObjectiveEvidence { get; set; } = "";

    public FindingStatus Status { get; set; } = FindingStatus.Open;

    /// <summary>FK to ActionItem — null for Observations/OFIs that don't require a CA.</summary>
    public Guid? ActionItemId { get; set; }

    public DateTime? ClosedAt { get; set; }
    public string? ClosureNotes { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public Audit Audit { get; set; } = null!;
    public StandardsClause Clause { get; set; } = null!;
    public ActionItem? ActionItem { get; set; }
}
