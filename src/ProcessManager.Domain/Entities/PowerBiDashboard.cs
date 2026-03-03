namespace ProcessManager.Domain.Entities;

public class PowerBiDashboard : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
