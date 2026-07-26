using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A named audit programme covering a calendar year and one or both standards.
/// </summary>
public class AuditProgram : BaseEntity
{
    /// <summary>e.g. "2026 Internal Audit Programme"</summary>
    public string Name { get; set; } = "";

    public ConformanceStandard Standard { get; set; }

    /// <summary>Calendar year this programme covers.</summary>
    public int Year { get; set; }

    public string LeadAuditor { get; set; } = "";

    public AuditProgramStatus Status { get; set; } = AuditProgramStatus.Planning;

    // ── Navigation ──────────────────────────────────────────────────────────

    public ICollection<Audit> Audits { get; set; } = new List<Audit>();
}
