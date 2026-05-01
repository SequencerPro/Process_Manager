using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A single audit event within an <see cref="AuditProgram"/>.
/// </summary>
public class Audit : BaseEntity
{
    public Guid ProgramId { get; set; }

    public AuditType AuditType { get; set; }

    /// <summary>Free-text description of processes and areas covered.</summary>
    public string Scope { get; set; } = "";

    public DateTime PlannedDate { get; set; }

    /// <summary>Null until the audit has been conducted.</summary>
    public DateTime? ActualDate { get; set; }

    public string LeadAuditor { get; set; } = "";

    public AuditStatus Status { get; set; } = AuditStatus.Planned;

    // ── Navigation ──────────────────────────────────────────────────────────

    public AuditProgram Program { get; set; } = null!;
    public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
}
