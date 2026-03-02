namespace ProcessManager.Domain.Enums;

/// <summary>
/// Classifies a column in the C&amp;E matrix: is the output an item flowing out via a port,
/// or a named quality characteristic the team wants to control?
/// </summary>
public enum CeOutputCategory
{
    /// <summary>Item flowing out of the step via a material port — auto-linked.</summary>
    PortOutput,

    /// <summary>A named quality characteristic that may not correspond to a specific port
    /// (e.g. flatness, tensile strength, surface finish).</summary>
    QualityCharacteristic,
}
