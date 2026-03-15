using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A named, reusable root cause that builds the organisation's institutional knowledge over time.
/// Entries are shared across all analysis tools (Ishikawa, 5 Whys) via typeahead search.
/// </summary>
public class RootCauseEntry : BaseEntity
{
    /// <summary>Short cause name, e.g. "Fixture wear", "Operator training gap".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detail on how this cause manifests and how to detect it.</summary>
    public string? Description { get; set; }

    /// <summary>7M category for filtering and fishbone grouping.</summary>
    public RootCauseCategory Category { get; set; }

    /// <summary>Free-form comma-separated tags for cross-cutting retrieval.</summary>
    public string? Tags { get; set; }

    /// <summary>Suggested corrective action text — pre-populated into analyses that reference this entry.</summary>
    public string? CorrectiveActionTemplate { get; set; }

    /// <summary>Number of analyses that have referenced this entry. Incremented by Phases 10b/c.</summary>
    public int UsageCount { get; set; }
}
