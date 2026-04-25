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
