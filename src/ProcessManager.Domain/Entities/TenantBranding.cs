namespace ProcessManager.Domain.Entities;

public class TenantBranding : BaseEntity
{
    public string? LogoFileName { get; set; }
    public string? PrimaryColorHex { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? FooterText { get; set; }
}
