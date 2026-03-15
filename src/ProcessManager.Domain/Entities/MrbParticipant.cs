using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A named participant in an MRB review with a defined functional role.
/// </summary>
public class MrbParticipant : BaseEntity
{
    public Guid MrbReviewId { get; set; }

    /// <summary>Identity user ID of the participant.</summary>
    public string UserId { get; set; } = "";

    /// <summary>Display name captured at time of addition (denormalised for readability).</summary>
    public string DisplayName { get; set; } = "";

    public MrbParticipantRole Role { get; set; }

    /// <summary>When true, this participant must provide an assessment before the MRB can be decided.</summary>
    public bool IsRequired { get; set; }

    public string? Assessment { get; set; }
    public DateTime? AssessedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────

    public MrbReview MrbReview { get; set; } = null!;
}
