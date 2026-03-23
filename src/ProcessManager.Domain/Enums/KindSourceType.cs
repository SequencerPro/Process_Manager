namespace ProcessManager.Domain.Enums;

/// <summary>
/// Classifies the sourcing strategy for a Kind.
/// </summary>
public enum KindSourceType
{
    /// <summary>Manufactured in-house.</summary>
    Make,

    /// <summary>Purchased from a vendor.</summary>
    Buy,

    /// <summary>A drawing, spec, or form — not a physical part.</summary>
    ReferenceDocument,

    /// <summary>Virtual assembly or subassembly (BOM placeholder).</summary>
    Phantom,

    /// <summary>Expendable supply (solder, lubricant, fasteners).</summary>
    Consumable
}
