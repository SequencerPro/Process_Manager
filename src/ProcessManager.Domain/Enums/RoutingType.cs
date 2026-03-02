namespace ProcessManager.Domain.Enums;

/// <summary>
/// How items are routed through a WorkflowLink.
/// </summary>
public enum RoutingType
{
    /// <summary>Unconditional — items always follow this link.</summary>
    Always,

    /// <summary>Follow when item's grade matches a condition.</summary>
    GradeBased,

    /// <summary>Human operator selects the path.</summary>
    Manual
}
