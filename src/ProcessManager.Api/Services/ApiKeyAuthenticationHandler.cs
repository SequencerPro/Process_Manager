using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcessManager.Api.Data;

namespace ProcessManager.Api.Services;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private const string HeaderName = "X-Api-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
            return AuthenticateResult.NoResult();

        var rawKey = headerValue.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.Fail("Empty API key");

        var keyHash = HashKey(rawKey);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();

        var apiKey = await db.ApiKeys
            .Include(k => k.Workstation)
                .ThenInclude(w => w.FixedLocation)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (apiKey == null)
            return AuthenticateResult.Fail("Invalid API key");

        if (!apiKey.IsActive)
            return AuthenticateResult.Fail("API key is inactive");

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key has expired");

        apiKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim("workstation_id", apiKey.WorkstationId.ToString()),
            new Claim("workstation_code", apiKey.Workstation.Code),
            new Claim("fixed_location_id", apiKey.Workstation.FixedLocationId.ToString()),
            new Claim("api_key_id", apiKey.Id.ToString()),
            new Claim("api_key_prefix", apiKey.KeyPrefix),
            new Claim("tenant_id", apiKey.TenantId.ToString()),
            new Claim(ClaimTypes.Name, $"apikey:{apiKey.KeyPrefix}"),
            new Claim(ClaimTypes.Role, "ApiKey"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    public static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"pk_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
