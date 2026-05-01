namespace ProcessManager.Domain.Entities;

public class SpcDataPoint : BaseEntity
{
    public Guid SpcChartId { get; set; }
    public Guid StepExecutionId { get; set; }
    public decimal Value { get; set; }
    public int SubgroupIndex { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public SpcChart SpcChart { get; set; } = null!;
    public StepExecution StepExecution { get; set; } = null!;
}
