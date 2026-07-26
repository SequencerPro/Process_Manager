using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class CapaRecord : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public CapaType Type { get; set; } = CapaType.Corrective;

    public CapaSourceType SourceType { get; set; } = CapaSourceType.Manual;

    public Guid? SourceEntityId { get; set; }

    public string ProblemStatement { get; set; } = string.Empty;

    public string? ContainmentAction { get; set; }

    public Guid? RootCauseAnalysisId { get; set; }

    public string? RootCauseAnalysisType { get; set; }

    public string? PermanentCorrectiveAction { get; set; }

    public string? PreventiveAction { get; set; }

    public string? VerificationMethod { get; set; }

    public DateTime? VerificationDueDate { get; set; }

    public string? VerifiedByUserId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? EffectivenessReviewDate { get; set; }

    public string? EffectivenessVerifiedByUserId { get; set; }

    public DateTime? EffectivenessVerifiedAt { get; set; }

    public CapaStatus Status { get; set; } = CapaStatus.Open;

    public string OwnerUserId { get; set; } = string.Empty;

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string? TeamMemberIds { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<CapaStep> Steps { get; set; } = new List<CapaStep>();
}
