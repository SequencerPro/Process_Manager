using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/api-keys")]
public class ApiKeysController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ApiKeysController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ApiKeyResponseDto>>> GetAll(
        [FromQuery] Guid? workstationId = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.ApiKeys
            .Include(k => k.Workstation)
            .AsQueryable();

        if (workstationId.HasValue)
            query = query.Where(k => k.WorkstationId == workstationId.Value);

        if (active.HasValue)
            query = query.Where(k => k.IsActive == active.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<ApiKeyResponseDto>(
            items.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiKeyResponseDto>> GetById(Guid id)
    {
        var key = await _db.ApiKeys
            .Include(k => k.Workstation)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (key is null) return NotFound();
        return MapToDto(key);
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create(CreateApiKeyDto dto)
    {
        var ws = await _db.Workstations.FindAsync(dto.WorkstationId);
        if (ws is null || !ws.IsActive)
            return BadRequest("Workstation not found or inactive.");

        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value <= DateTime.UtcNow)
            return BadRequest("Expiry date must be in the future.");

        var rawKey = ApiKeyAuthenticationHandler.GenerateRawKey();
        var keyHash = ApiKeyAuthenticationHandler.HashKey(rawKey);
        var keyPrefix = rawKey[..Math.Min(11, rawKey.Length)];

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

        var apiKey = new ApiKey
        {
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = dto.Name.Trim(),
            WorkstationId = dto.WorkstationId,
            CreatedByUserId = userId,
            ExpiresAt = dto.ExpiresAt
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = apiKey.Id },
            new ApiKeyCreatedDto(apiKey.Id, rawKey, keyPrefix, apiKey.Name, ws.Id, ws.Code));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiKeyResponseDto>> Update(Guid id, UpdateApiKeyDto dto)
    {
        var key = await _db.ApiKeys
            .Include(k => k.Workstation)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (key is null) return NotFound();

        key.Name = dto.Name.Trim();
        key.IsActive = dto.IsActive;
        key.ExpiresAt = dto.ExpiresAt;

        await _db.SaveChangesAsync();
        return MapToDto(key);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var key = await _db.ApiKeys.FindAsync(id);
        if (key is null) return NotFound();

        _db.ApiKeys.Remove(key);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ApiKeyResponseDto MapToDto(ApiKey k) => new(
        k.Id, k.KeyPrefix, k.Name, k.WorkstationId,
        k.Workstation?.Code ?? "", k.IsActive,
        k.LastUsedAt, k.ExpiresAt, k.CreatedAt);
}
