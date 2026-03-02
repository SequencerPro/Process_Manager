using Microsoft.AspNetCore.Identity;

namespace ProcessManager.Api.Data;

/// <summary>
/// Application user extending ASP.NET Core Identity's IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Optional display name shown in the UI.</summary>
    public string? DisplayName { get; set; }
}
