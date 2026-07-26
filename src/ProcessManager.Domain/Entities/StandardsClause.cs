using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single addressable clause from ISO 9001:2015 or AS9100 Rev D.
/// Pre-seeded reference data — not user-editable.
/// </summary>
public class StandardsClause : BaseEntity
{
    public ConformanceStandard Standard { get; set; }

    /// <summary>e.g. "8.5.2"</summary>
    public string ClauseNumber { get; set; } = "";

    /// <summary>e.g. "Identification and Traceability"</summary>
    public string Title { get; set; } = "";

    /// <summary>One-paragraph plain-language summary of what the clause requires.</summary>
    public string RequirementSummary { get; set; } = "";

    /// <summary>True for clauses that are AS9100-only and not present in base ISO 9001.</summary>
    public bool IsAs9100Addition { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public ICollection<ClauseEvidenceLink> EvidenceLinks { get; set; } = new List<ClauseEvidenceLink>();
    public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
}
