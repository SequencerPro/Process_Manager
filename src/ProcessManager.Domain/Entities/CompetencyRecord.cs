using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Records that a specific user has completed a specific training process.
/// Status transitions: Current → Expired (when ExpiresAt < now) or Superseded (when a newer record is created).
/// </summary>
public class CompetencyRecord : BaseEntity
{
    /// <summary>Identity user ID of the person who completed the training.</summary>
    public string UserId { get; set; } = "";

    /// <summary>Display name of the trainee — denormalised for read performance.</summary>
    public string UserDisplayName { get; set; } = "";

    /// <summary>FK to the Training-role Process that was completed.</summary>
    public Guid TrainingProcessId { get; set; }

    /// <summary>Version of the training process at the time of completion.</summary>
    public int TrainingProcessVersion { get; set; }

    /// <summary>FK to the Job that resulted in this competency record (null if manually recorded).</summary>
    public Guid? JobId { get; set; }

    /// <summary>Identity user ID of the instructor (optional, for instructor-led training).</summary>
    public string? InstructorUserId { get; set; }

    /// <summary>Display name of the instructor.</summary>
    public string? InstructorDisplayName { get; set; }

    public DateTime CompletedAt { get; set; }

    /// <summary>When this competency expires. Null means it never expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    public CompetencyStatus Status { get; set; } = CompetencyStatus.Current;

    public string? Notes { get; set; }

    // Navigation
    public Process? TrainingProcess { get; set; }
    public Job? Job { get; set; }
}
