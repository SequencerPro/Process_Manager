using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class SpcChart : BaseEntity
{
    public Guid ProcessId { get; set; }
    public Guid ContentBlockId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SpcChartType ChartType { get; set; } = SpcChartType.XbarR;
    public int SubgroupSize { get; set; } = 5;
    public ControlLimitSource ControlLimitSource { get; set; } = ControlLimitSource.Calculated;
    public decimal? UCL { get; set; }
    public decimal? LCL { get; set; }
    public decimal? CL { get; set; }
    public decimal? RangeUCL { get; set; }
    public decimal? RangeLCL { get; set; }
    public decimal? RangeCL { get; set; }
    public decimal? TargetCpk { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Process Process { get; set; } = null!;
    public ICollection<SpcDataPoint> DataPoints { get; set; } = new List<SpcDataPoint>();
}
