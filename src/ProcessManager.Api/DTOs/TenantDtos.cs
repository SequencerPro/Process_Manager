using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

public record TenantResponseDto(
    Guid Id,
    string Subdomain,
    string Name,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateTenantDto(string Subdomain, string Name);

public record UpdateTenantStatusDto(TenantStatus Status);
