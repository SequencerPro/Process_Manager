namespace ProcessManager.Domain.Enums;

/// <summary>
/// The guided phases an operator moves through while executing a single step
/// in the Execution Wizard. Numeric values match the wizard's 1-based step
/// indicator so client and server agree on ordering.
/// </summary>
public enum ExecutionPhase
{
    /// <summary>Prepare the workstation; review setup instructions.</summary>
    Setup = 1,

    /// <summary>Acknowledge safety hazards before work begins.</summary>
    Safety = 2,

    /// <summary>Review reference material (drawings, 3D models, notes).</summary>
    Reference = 3,

    /// <summary>Capture data: prompts, ports, photos, scans.</summary>
    Execution = 4,

    /// <summary>Final completion and sign-off. Terminal — irreversible.</summary>
    SignOff = 5
}
