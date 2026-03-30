using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<TokenHandler> _logger;

    public TokenHandler(
        TokenService tokenService,
        AuthenticationStateProvider authState,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenHandler> logger)
    {
        _tokenService        = tokenService;
        _authState           = authState;
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            ClaimsPrincipal? user = null;
            string source = "none";

            try
            {
                // Interactive circuit: AuthenticationStateProvider works fine.
                var state = await _authState.GetAuthenticationStateAsync();
                user = state.User;
                if (user?.Identity?.IsAuthenticated == true)
                    source = "AuthenticationStateProvider";
            }
            catch (Exception ex)
            {
                // SSR prerender or handler running outside the circuit scope.
                _logger.LogDebug("AuthenticationStateProvider unavailable: {Message}", ex.Message);
            }

            // If AuthenticationStateProvider didn't yield an authenticated user
            // (either it threw, or it returned an anonymous principal because
            // this handler was resolved in a separate IHttpClientFactory scope),
            // fall back to IHttpContextAccessor which carries the cookie claims
            // during SSR / HTTP requests.
            if (user?.Identity?.IsAuthenticated != true)
            {
                user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated == true)
                    source = "IHttpContextAccessor";
            }

            if (user?.Identity?.IsAuthenticated == true)
            {
                _tokenService.AccessToken  = user.FindFirst("access_token")?.Value;
                _tokenService.UserName     = user.Identity!.Name;
                _tokenService.DisplayName  = user.FindFirst("display_name")?.Value;
                _tokenService.Role         = user.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(_tokenService.AccessToken))
                {
                    _logger.LogWarning(
                        "[TokenHandler] User '{User}' is authenticated via {Source} but has NO access_token claim. " +
                        "Available claims: {Claims}",
                        _tokenService.UserName, source,
                        string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value[..Math.Min(c.Value.Length, 20)]}...")));
                }
                else
                {
                    _logger.LogDebug(
                        "[TokenHandler] Token acquired from {Source} for user '{User}' (token length: {Len})",
                        source, _tokenService.UserName, _tokenService.AccessToken.Length);
                }
            }
            else
            {
                _logger.LogWarning(
                    "[TokenHandler] No authenticated user found. " +
                    "HttpContext available: {HasContext}, HttpContext.User authenticated: {CtxAuth}",
                    _httpContextAccessor.HttpContext is not null,
                    _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated);
            }
        }

        if (!string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);
        }
        else
        {
            _logger.LogWarning(
                "[TokenHandler] Sending request WITHOUT Authorization header to {Url}",
                request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(
                "[TokenHandler] 401 Unauthorized from {Url}. Had token: {HadToken}, Token length: {Len}",
                request.RequestUri,
                !string.IsNullOrEmpty(_tokenService.AccessToken),
                _tokenService.AccessToken?.Length ?? 0);
        }

        return response;
    }
}
