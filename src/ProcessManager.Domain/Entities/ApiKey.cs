namespace ProcessManager.Domain.Entities;

public class ApiKey : BaseEntity
{
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WorkstationId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public Workstation Workstation { get; set; } = null!;
}
