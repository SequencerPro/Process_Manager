using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

public class ScanEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkstationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public string ScannedBarcode { get; set; } = string.Empty;
    public Guid? ItemId { get; set; }
    public Guid? TransactionId { get; set; }
    public ScanResult Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScannedAt { get; set; }

    // Navigation properties
    public Workstation Workstation { get; set; } = null!;
    public ApiKey ApiKey { get; set; } = null!;
    public Item? Item { get; set; }
    public InventoryTransaction? Transaction { get; set; }
}
