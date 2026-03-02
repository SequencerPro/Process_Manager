namespace ProcessManager.Domain.Entities;

/// <summary>
/// A failure mode associated with a specific ProcessStep within a PFMEA.
/// One step may have multiple failure modes.
/// </summary>
public class PfmeaFailureMode : BaseEntity
{
    public Guid PfmeaId { get; set; }

    /// <summary>The ProcessStep this failure mode is associated with.</summary>
    public Guid ProcessStepId { get; set; }

    /// <summary>The intended purpose of the step (what it should do).</summary>
    public string StepFunction { get; set; } = string.Empty;

    /// <summary>How the step could fail to perform its intended function.</summary>
    public string FailureMode { get; set; } = string.Empty;

    /// <summary>The consequence of the failure on the customer or next step.</summary>
    public string FailureEffect { get; set; } = string.Empty;

    /// <summary>The root cause or mechanism that leads to the failure.</summary>
    public string? FailureCause { get; set; }

    /// <summary>Existing controls that prevent the cause from occurring.</summary>
    public string? PreventionControls { get; set; }

    /// <summary>Existing controls that detect the failure before it reaches the customer.</summary>
    public string? DetectionControls { get; set; }

    // ── Risk ratings (1–10 each) ─────────────────────────────────────────

    /// <summary>Severity of the failure effect (1 = no effect, 10 = catastrophic).</summary>
    public int Severity { get; set; } = 1;

    /// <summary>Likelihood of the cause occurring (1 = very unlikely, 10 = almost certain).</summary>
    public int Occurrence { get; set; } = 1;

    /// <summary>Ability of current controls to detect the failure (1 = near certain detection, 10 = no detection).</summary>
    public int Detection { get; set; } = 1;

    /// <summary>Risk Priority Number = Severity × Occurrence × Detection. Computed, not stored.</summary>
    public int Rpn => Severity * Occurrence * Detection;

    // Navigation
    public Pfmea Pfmea { get; set; } = null!;
    public ProcessStep ProcessStep { get; set; } = null!;
    public ICollection<PfmeaAction> Actions { get; set; } = new List<PfmeaAction>();
}
