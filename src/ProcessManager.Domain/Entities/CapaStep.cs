namespace ProcessManager.Domain.Entities;

public class CapaStep : BaseEntity
{
    public Guid CapaRecordId { get; set; }

    public string StepType { get; set; } = string.Empty;

    public string? CompletedByUserId { get; set; }

    public string? CompletedByDisplayName { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    public string? AttachmentFileName { get; set; }

    public CapaRecord CapaRecord { get; set; } = null!;
}
