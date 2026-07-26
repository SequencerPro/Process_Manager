namespace ProcessManager.Domain.Entities;

public class GageStudyMeasurement : BaseEntity
{
    public Guid GageStudyId { get; set; }
    public int PartNumber { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public int TrialNumber { get; set; }
    public decimal MeasuredValue { get; set; }

    // Navigation
    public GageStudy GageStudy { get; set; } = null!;
}
