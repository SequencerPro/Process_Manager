namespace ProcessManager.Domain.Entities;

public class UsageMetric : BaseEntity
{
    public UsageMetricType MetricType { get; set; }
    public int Count { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public enum UsageMetricType
{
    JobExecutions,
    PdfExports,
    ApiCalls,
    ActiveUsers
}
