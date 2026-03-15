using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Formal cross-functional review of a nonconforming item whose disposition
/// cannot be determined unilaterally at the floor level.
/// </summary>
public class MrbReview : BaseEntity
{
    /// <summary>The non-conformance that triggered this review.</summary>
    public Guid NonConformanceId { get; set; }

    public MrbStatus Status { get; set; } = MrbStatus.Draft;

    /// <summary>Full description of the nonconforming item.</summary>
    public string ItemDescription { get; set; } = "";

    /// <summary>Quantity or lot size affected (free-form, e.g. "12 pcs", "Lot 4421-B").</summary>
    public string? QuantityAffected { get; set; }

    /// <summary>Technical description of the nonconformance.</summary>
    public string ProblemStatement { get; set; } = "";

    // ── Flags set at creation or during review ──────────────────────────────

    public bool CustomerNotificationRequired { get; set; }
    public bool ScarRequired { get; set; }
    public bool SupplierCaused { get; set; }

    /// <summary>When true, status cannot advance to Closed until a linked RCA exists.</summary>
    public bool RequiresRca { get; set; }

    // ── RCA linkage ─────────────────────────────────────────────────────────

    public MrbLinkedRcaType? LinkedRcaAnalysisType { get; set; }
    public Guid? LinkedRcaId { get; set; }

    // ── Decision ────────────────────────────────────────────────────────────

    public MrbDispositionDecision? DispositionDecision { get; set; }
    public string? DispositionJustification { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public NonConformance NonConformance { get; set; } = null!;
    public ICollection<MrbParticipant> Participants { get; set; } = new List<MrbParticipant>();
}
