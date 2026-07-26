using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class CalibrationSchedule : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public int IntervalDays { get; set; }
    public IntervalAdjustmentMethod IntervalAdjustmentMethod { get; set; } = IntervalAdjustmentMethod.Fixed;
    public int ConsecutivePassCount { get; set; }
    public int MaxIntervalDays { get; set; }
    public int MinIntervalDays { get; set; }
    public int ExtensionPercent { get; set; } = 25;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
