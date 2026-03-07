namespace ProcessManager.Web.Services;

/// <summary>
/// Scoped service that holds the current user's JWT access token for the duration of
/// a Blazor Server circuit. Populated by middleware at the start of each HTTP request
/// from the ASP.NET Core auth cookie claims.
/// </summary>
public class TokenService
{
    public string? AccessToken { get; set; }
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? Role { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
}
