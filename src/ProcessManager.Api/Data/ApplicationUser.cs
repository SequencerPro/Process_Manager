using Microsoft.AspNetCore.Identity;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Data;

/// <summary>
/// Application user extending ASP.NET Core Identity's IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Optional display name shown in the UI.</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The tenant this user belongs to. Null for platform admins who operate outside any
    /// single tenant. All regular users are scoped to exactly one tenant; queries they
    /// issue are filtered to that tenant via the EF global query filter.
    /// </summary>
    public Guid? TenantId { get; set; } = Tenant.DefaultTenantId;

    /// <summary>
    /// When true, this user can access <c>/api/platform/*</c> endpoints and bypass the
    /// tenant query filter. Reserved for system operators (Sequencer staff), not customers.
    /// </summary>
    public bool IsPlatformAdmin { get; set; }
}
