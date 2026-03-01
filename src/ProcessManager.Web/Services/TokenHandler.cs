using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProcessManager.Web.Services;

/// <summary>
/// DelegatingHandler that ensures the TokenService is populated from the
/// Blazor circuit's AuthenticationState before every outbound API request.
///
/// This solves the Blazor Server scoping problem where MainLayout's
/// OnInitializedAsync is async and may not have finished before child
/// component OnInitializedAsync calls fire.
/// </summary>
public class TokenHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;
    private readonly AuthenticationStateProvider _authState;

    public TokenHandler(TokenService tokenService, AuthenticationStateProvider authState)
    {
        _tokenService = tokenService;
        _authState    = authState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Lazily populate TokenService from the circuit's ClaimsPrincipal
        // if it hasn't been set yet (e.g. interactive circuit first call).
        if (string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            var state = await _authState.GetAuthenticationStateAsync();
            var user  = state.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                _tokenService.AccessToken  = user.FindFirst("access_token")?.Value;
                _tokenService.UserName     = user.Identity.Name;
                _tokenService.DisplayName  = user.FindFirst("display_name")?.Value;
                _tokenService.Role         = user.FindFirst(ClaimTypes.Role)?.Value;
            }
        }

        if (!string.IsNullOrEmpty(_tokenService.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
