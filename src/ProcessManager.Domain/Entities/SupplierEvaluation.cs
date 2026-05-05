namespace ProcessManager.Domain.Entities;

public class SupplierEvaluation : BaseEntity
{
    public Guid SupplierId { get; set; }
    public DateTime EvaluationDate { get; set; }
    public int QualityScore { get; set; }
    public int DeliveryScore { get; set; }
    public int ResponsivenessScore { get; set; }
    public int OverallScore { get; set; }
    public string? Notes { get; set; }
    public string? EvaluatedByUserId { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
}
