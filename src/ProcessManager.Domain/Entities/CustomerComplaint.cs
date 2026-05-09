using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class CustomerComplaint : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerReference { get; set; }

    public Guid? ProductKindId { get; set; }

    public string? LotNumber { get; set; }

    public DateTime ComplaintDate { get; set; }

    public DateTime ReceivedDate { get; set; }

    public ComplaintCategory Category { get; set; } = ComplaintCategory.ProductDefect;

    public ComplaintSeverity Severity { get; set; } = ComplaintSeverity.Minor;

    public string Description { get; set; } = string.Empty;

    public int QuantityAffected { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;

    public string OwnerUserId { get; set; } = string.Empty;

    public string OwnerDisplayName { get; set; } = string.Empty;

    public DateTime? ResponseDueDate { get; set; }

    public DateTime? ResponseSentAt { get; set; }

    public bool? CustomerSatisfied { get; set; }

    public Guid? LinkedNonConformanceId { get; set; }

    public Guid? LinkedCapaId { get; set; }

    public Guid? LinkedSupplierId { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<ComplaintInvestigation> Investigations { get; set; } = new List<ComplaintInvestigation>();

    public ICollection<ComplaintResponse> Responses { get; set; } = new List<ComplaintResponse>();
}
