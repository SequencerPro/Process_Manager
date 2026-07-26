using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An append-only reading of an ObjectiveMeasure — manual entries plus periodic
/// auto-captures. Powers trend charts and historical RAG evaluation.
/// </summary>
public class MeasureSnapshot : BaseEntity
{
    public Guid MeasureId { get; set; }
    public ObjectiveMeasure Measure { get; set; } = null!;

    public decimal Value { get; set; }

    public DateTime CapturedAt { get; set; }

    public MeasureCaptureSource CaptureSource { get; set; } = MeasureCaptureSource.Manual;

    public string? Note { get; set; }
}
