using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SupplierStatus Status { get; set; } = SupplierStatus.Pending;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? LastEvaluationDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<SupplierEvaluation> Evaluations { get; set; } = new List<SupplierEvaluation>();
}
