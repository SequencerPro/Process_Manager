using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Services;

/// <summary>
/// Issues JWT tokens for authenticated users. Extracted from AuthController so that
/// the public signup flow (M2) can issue a login token immediately after provisioning
/// a new tenant + admin user without duplicating the signing logic.
/// </summary>
public sealed class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public TokenResponseDto Generate(ApplicationUser user, string role)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(jwtConfig["ExpiryMinutes"] ?? "480");
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.TenantId is Guid tenantId)
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
        if (user.IsPlatformAdmin)
            claims.Add(new Claim("platform_admin", "true"));
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("display_name", user.DisplayName));

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new TokenResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.UserName!,
            user.Email!,
            role,
            user.DisplayName,
            expiry);
    }
}
