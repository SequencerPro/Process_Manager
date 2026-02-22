namespace ProcessManager.Domain.Enums;

/// <summary>
/// Defines how many items a port expects or produces.
/// </summary>
public enum QuantityRuleMode
{
    /// <summary>Must be exactly N items.</summary>
    Exactly,

    /// <summary>Either 0 or N items (conditional flow).</summary>
    ZeroOrN,

    /// <summary>Between min and max items (inclusive).</summary>
    Range,

    /// <summary>At least min items, no upper limit.</summary>
    Unbounded
}
