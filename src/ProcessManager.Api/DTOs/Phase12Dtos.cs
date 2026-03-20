using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ───── OrgUnit ─────

public record OrgUnitCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    OrgUnitType Type = OrgUnitType.Department,
    Guid? ParentId = null);

public record OrgUnitUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    OrgUnitType Type = OrgUnitType.Department,
    Guid? ParentId = null,
    bool IsActive = true);

public record OrgUnitResponseDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    Guid? ParentId,
    string? ParentName,
    bool IsActive,
    int ChildCount,
    int MemberCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── OrgUnitMember ─────

public record OrgUnitMemberAddDto(
    [Required] string UserId);

public record OrgUnitMemberResponseDto(
    Guid Id,
    string UserId,
    string? UserName,
    string? DisplayName,
    string? Email,
    Guid OrgUnitId,
    string OrgUnitName,
    DateTime CreatedAt);

public record UserOrgUnitResponseDto(
    Guid OrgUnitId,
    string OrgUnitCode,
    string OrgUnitName,
    string OrgUnitType,
    Guid MembershipId,
    DateTime JoinedAt);

