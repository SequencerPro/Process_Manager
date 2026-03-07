namespace ProcessManager.Domain.Enums;

/// <summary>
/// Classifies the purpose of a content block within a step's work instructions.
/// Drives the guided operator wizard phase assignment, acknowledgment gating,
/// and the maturity scoring ruleset.
/// </summary>
public enum ContentCategory
{
    /// <summary>Preparation, tooling, or equipment checks before work begins.</summary>
    Setup,

    /// <summary>Hazards, PPE requirements, or stop conditions. Always requires acknowledgment.</summary>
    Safety,

    /// <summary>Visual or measurement checks during or after work. Typically paired with data prompts.</summary>
    Inspection,

    /// <summary>Background information, diagrams, or drawings. Informational only.</summary>
    Reference,

    /// <summary>Engineering notes, caveats, or clarifications.</summary>
    Note
}
