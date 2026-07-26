namespace ProcessManager.Domain.Entities;

public class Workstation : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid FixedLocationId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public StorageLocation FixedLocation { get; set; } = null!;
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
