using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

public record TenantBrandingResponseDto(
    Guid Id,
    string? LogoFileName,
    string? PrimaryColorHex,
    string CompanyName,
    string? FooterText,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpdateTenantBrandingDto(
    [StringLength(200, MinimumLength = 1)] string CompanyName,
    [StringLength(7)] string? PrimaryColorHex,
    [StringLength(500)] string? FooterText);

/// <summary>
/// Public-safe subset of branding for unauthenticated surfaces (e.g. login page).
/// Intentionally omits database identifiers and audit timestamps.
///
/// <see cref="LogoDataUrl"/> embeds the logo bytes inline as a base64 data URL,
/// which lets the login page render the logo without making a second cross-origin
/// fetch — sidestepping CORS, service-worker interception, and any reverse-proxy
/// quirks that might affect a separate image request.
/// </summary>
public record PublicTenantBrandingDto(
    string? LogoFileName,
    string? LogoDataUrl,
    string? PrimaryColorHex,
    string CompanyName);
