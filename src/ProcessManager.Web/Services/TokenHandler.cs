using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace ProcessManager.Web.Services;

/// <summary>
/// DelegatingHandler that ensures the TokenService is populated from the
/// Blazor circuit's AuthenticationState before every outbound API request.
///
/// During SSR prerender there is no Blazor circuit, so AuthenticationStateProvider
/// throws. In that case we fall back to IHttpContextAccessor which is always
/// available during SSR and carries the same claims from the auth cookie.
/// </summary>
public class TokenHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;
    private readonly AuthenticationStateProvider _authState;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenHandler(
        TokenService tokenService,
        AuthenticationStateProvider authState,
        IHttpContextAccessor httpContextAccessor)
    {
        _tokenService        = tokenService;
        _authState           = authState;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            ClaimsPrincipal? user = null;

            try
            {
                // Interactive circuit: AuthenticationStateProvider works fine.
                var state = await _authState.GetAuthenticationStateAsync();
                user = state.User;
            }
            catch
            {
                // SSR prerender: no circuit scope — fall back to the HTTP context.
                user = _httpContextAccessor.HttpContext?.User;
            }

            if (user?.Identity?.IsAuthenticated == true)
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
