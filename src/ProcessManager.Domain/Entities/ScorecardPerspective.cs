namespace ProcessManager.Domain.Entities;

/// <summary>
/// One lens on the business within a Scorecard. The four Kaplan-Norton perspectives
/// (Financial, Customer, Internal Business Process, Learning &amp; Growth) are seeded on
/// scorecard creation; users may rename, reorder, add, or remove.
/// </summary>
public class ScorecardPerspective : BaseEntity
{
    public Guid ScorecardId { get; set; }
    public Scorecard Scorecard { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Display order — top-to-bottom rows on the strategy map.</summary>
    public int SortOrder { get; set; }

    public ICollection<StrategicObjective> Objectives { get; set; } = new List<StrategicObjective>();
}
