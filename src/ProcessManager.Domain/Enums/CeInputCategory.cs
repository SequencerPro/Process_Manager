namespace ProcessManager.Domain.Enums;

/// <summary>
/// Classifies a row in the C&amp;E matrix: is the input something the process can control,
/// a source of uncontrolled variation, or an item flowing in from a port?
/// </summary>
public enum CeInputCategory
{
    /// <summary>Item flowing into the step via a material port — auto-linked.</summary>
    PortInput,

    /// <summary>A factor the process can deliberately set and hold (e.g. spindle speed, temperature set-point).</summary>
    ControlFactor,

    /// <summary>A source of variation the process cannot control (e.g. ambient humidity, incoming raw material variation).</summary>
    NoiseFactor,
}
