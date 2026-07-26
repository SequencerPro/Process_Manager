using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ComplaintInvestigation : BaseEntity
{
    public Guid CustomerComplaintId { get; set; }

    public InvestigationType InvestigationType { get; set; } = InvestigationType.InitialAssessment;

    public string Findings { get; set; } = string.Empty;

    public string InvestigatedByUserId { get; set; } = string.Empty;

    public string InvestigatedByDisplayName { get; set; } = string.Empty;

    public DateTime InvestigatedAt { get; set; }
}
