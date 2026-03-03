namespace ProcessManager.Domain.Entities;

/// <summary>
/// A Process Failure Mode and Effects Analysis (PFMEA) attached to a specific Process.
/// The system pre-populates one entry (PfmeaStep) per ProcessStep; engineers add failure modes
/// on top of that structure.
/// </summary>
public class Pfmea : BaseEntity
{
    /// <summary>The Process this PFMEA analyses.</summary>
    public Guid ProcessId { get; set; }

    /// <summary>Short identifier, e.g. "PFMEA-WDG-MACH-V1".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Scope, revision history, team members, etc.</summary>
    public string? Description { get; set; }

    /// <summary>Version number — incremented when the underlying process changes and a new PFMEA is branched.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether this is the current active PFMEA for the process.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>The Process version this PFMEA was authored against.</summary>
    public int ProcessVersion { get; set; } = 1;

    /// <summary>True when the linked Process has been released with a higher version since this PFMEA was last reviewed.</summary>
    public bool IsStale { get; set; } = false;

    /// <summary>Who cleared the staleness flag (reviewed and accepted no changes required).</summary>
    public string? StalenessClearedBy { get; set; }

    /// <summary>When the staleness flag was cleared.</summary>
    public DateTime? StalenessClearedAt { get; set; }

    /// <summary>Reason why no PFMEA update was required despite the process change.</summary>
    public string? StalenessClearanceNotes { get; set; }

    // Navigation
    public Process Process { get; set; } = null!;
    public ICollection<PfmeaFailureMode> FailureModes { get; set; } = new List<PfmeaFailureMode>();
}
