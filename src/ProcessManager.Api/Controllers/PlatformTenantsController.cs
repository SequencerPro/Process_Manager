using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Platform-admin endpoints for provisioning and managing tenants. Bypasses the
/// per-request tenant query filter by running as <c>IsPlatformAdmin</c>. Requires
/// the <c>platform_admin</c> JWT claim (set on <see cref="ApplicationUser.IsPlatformAdmin"/>).
/// </summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = PlatformAdminPolicy.Name)]
public class PlatformTenantsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly ITenantContext _tenantContext;

    public PlatformTenantsController(ProcessManagerDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetAll()
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Subdomain)
            .Select(t => new TenantResponseDto(t.Id, t.Subdomain, t.Name, t.Status, t.CreatedAt, t.UpdatedAt))
            .ToListAsync();
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponseDto>> GetById(Guid id)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        return Ok(new TenantResponseDto(t.Id, t.Subdomain, t.Name, t.Status, t.CreatedAt, t.UpdatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponseDto>> Create([FromBody] CreateTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subdomain) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Subdomain and Name are required.");

        var normalizedSubdomain = dto.Subdomain.Trim().ToLowerInvariant();
        if (await _db.Tenants.AnyAsync(t => t.Subdomain == normalizedSubdomain))
            return Conflict($"Subdomain '{normalizedSubdomain}' is already in use.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Subdomain = normalizedSubdomain,
            Name = dto.Name.Trim(),
            Status = TenantStatus.Trial,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id },
            new TenantResponseDto(tenant.Id, tenant.Subdomain, tenant.Name, tenant.Status, tenant.CreatedAt, tenant.UpdatedAt));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TenantResponseDto>> UpdateStatus(Guid id, [FromBody] UpdateTenantStatusDto dto)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound();

        tenant.Status = dto.Status;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new TenantResponseDto(tenant.Id, tenant.Subdomain, tenant.Name, tenant.Status, tenant.CreatedAt, tenant.UpdatedAt));
    }
}

/// <summary>Authorization policy name for platform-admin-only endpoints.</summary>
public static class PlatformAdminPolicy
{
    public const string Name = "PlatformAdmin";
}
