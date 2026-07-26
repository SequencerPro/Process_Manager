using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Many-to-many link between a <see cref="StandardsClause"/> and a system
/// entity that serves as objective evidence of conformance.
/// </summary>
public class ClauseEvidenceLink : BaseEntity
{
    public Guid ClauseId { get; set; }

    public ClauseEvidenceEntityType EntityType { get; set; }

    /// <summary>FK into the respective table (Process, ControlPlan, etc.).</summary>
    public Guid EntityId { get; set; }

    /// <summary>Optional context note on how this record evidences the clause.</summary>
    public string? EvidenceNote { get; set; }

    /// <summary>True when created by the seeder or auto-link logic.</summary>
    public bool IsAutoLinked { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public StandardsClause Clause { get; set; } = null!;
}
