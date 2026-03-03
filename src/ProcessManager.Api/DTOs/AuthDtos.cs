using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record LoginRequestDto(
    [Required] string UserName,
    [Required] string Password
);

public record RegisterRequestDto(
    [Required][StringLength(50)] string UserName,
    [Required][EmailAddress] string Email,
    [Required][StringLength(100, MinimumLength = 8)] string Password,
    [Required] string Role,         // "Admin" or "Engineer"
    [StringLength(100)] string? DisplayName = null
);

public record AdminUpdateUserDto(
    [StringLength(100)] string? DisplayName,
    string? Role
);

public record ChangePasswordRequestDto(
    [Required] string CurrentPassword,
    [Required][StringLength(100, MinimumLength = 8)] string NewPassword
);

public record UpdateProfileRequestDto(
    [StringLength(100)] string? DisplayName
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record TokenResponseDto(
    string Token,
    string UserName,
    string Email,
    string Role,
    string? DisplayName,
    DateTime ExpiresAt
);

public record UserResponseDto(
    string Id,
    string UserName,
    string Email,
    string Role,
    string? DisplayName
);
