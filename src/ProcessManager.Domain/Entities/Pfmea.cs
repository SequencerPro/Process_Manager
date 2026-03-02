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

    // Navigation
    public Process Process { get; set; } = null!;
    public ICollection<PfmeaFailureMode> FailureModes { get; set; } = new List<PfmeaFailureMode>();
}
