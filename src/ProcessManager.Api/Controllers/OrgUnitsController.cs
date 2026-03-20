using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrgUnitsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public OrgUnitsController(ProcessManagerDbContext db) => _db = db;

    // ───── CRUD ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<OrgUnitResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] bool? topLevelOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.OrgUnits.Include(o => o.Parent).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Code.Contains(search) || o.Name.Contains(search));

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<OrgUnitType>(type, true, out var t))
            query = query.Where(o => o.Type == t);

        if (activeOnly == true)
            query = query.Where(o => o.IsActive);

        if (parentId.HasValue)
            query = query.Where(o => o.ParentId == parentId.Value);

        if (topLevelOnly == true)
            query = query.Where(o => o.ParentId == null);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(o => o.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load child counts
        var ids = items.Select(o => o.Id).ToList();
        var childCounts = await _db.OrgUnits
            .Where(o => o.ParentId.HasValue && ids.Contains(o.ParentId.Value))
            .GroupBy(o => o.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

        // Load member counts
        var memberCounts = await _db.OrgUnitMembers
            .Where(m => ids.Contains(m.OrgUnitId))
            .GroupBy(m => m.OrgUnitId)
            .Select(g => new { OrgUnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgUnitId, x => x.Count);

        return new PaginatedResponse<OrgUnitResponseDto>(
            items.Select(o => MapToDto(o, childCounts.GetValueOrDefault(o.Id), memberCounts.GetValueOrDefault(o.Id))).ToList(),
            totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrgUnitResponseDto>> GetById(Guid id)
    {
        var orgUnit = await _db.OrgUnits
            .Include(o => o.Parent)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orgUnit is null) return NotFound();

        var childCount = await _db.OrgUnits.CountAsync(o => o.ParentId == id);
        var memberCount = await _db.OrgUnitMembers.CountAsync(m => m.OrgUnitId == id);
        return MapToDto(orgUnit, childCount, memberCount);
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<List<OrgUnitResponseDto>>> GetChildren(Guid id)
    {
        if (!await _db.OrgUnits.AnyAsync(o => o.Id == id))
            return NotFound();

        var children = await _db.OrgUnits
            .Where(o => o.ParentId == id)
            .OrderBy(o => o.Code)
            .ToListAsync();

        var childIds = children.Select(c => c.Id).ToList();
        var childCounts = await _db.OrgUnits
            .Where(o => o.ParentId.HasValue && childIds.Contains(o.ParentId.Value))
            .GroupBy(o => o.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

        var memberCounts = await _db.OrgUnitMembers
            .Where(m => childIds.Contains(m.OrgUnitId))
            .GroupBy(m => m.OrgUnitId)
            .Select(g => new { OrgUnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgUnitId, x => x.Count);

        return children.Select(o => MapToDto(o, childCounts.GetValueOrDefault(o.Id), memberCounts.GetValueOrDefault(o.Id))).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<OrgUnitResponseDto>> Create(OrgUnitCreateDto dto)
    {
        if (await _db.OrgUnits.AnyAsync(o => o.Code == dto.Code))
            return Conflict($"An OrgUnit with code '{dto.Code}' already exists.");

        if (dto.ParentId.HasValue && !await _db.OrgUnits.AnyAsync(o => o.Id == dto.ParentId.Value))
            return BadRequest($"Parent OrgUnit '{dto.ParentId}' not found.");

        var orgUnit = new OrgUnit
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            ParentId = dto.ParentId
        };

        _db.OrgUnits.Add(orgUnit);
        await _db.SaveChangesAsync();

        var result = await _db.OrgUnits
            .Include(o => o.Parent)
            .FirstAsync(o => o.Id == orgUnit.Id);

        return CreatedAtAction(nameof(GetById), new { id = orgUnit.Id }, MapToDto(result, 0, 0));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<OrgUnitResponseDto>> Update(Guid id, OrgUnitUpdateDto dto)
    {
        var orgUnit = await _db.OrgUnits
            .Include(o => o.Parent)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orgUnit is null) return NotFound();

        // Prevent circular parent reference
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
                return BadRequest("An OrgUnit cannot be its own parent.");

            if (!await _db.OrgUnits.AnyAsync(o => o.Id == dto.ParentId.Value))
                return BadRequest($"Parent OrgUnit '{dto.ParentId}' not found.");

            // Check for circular reference: walk up from proposed parent
            if (await WouldCreateCycle(id, dto.ParentId.Value))
                return BadRequest("Setting this parent would create a circular hierarchy.");
        }

        orgUnit.Name = dto.Name;
        orgUnit.Type = dto.Type;
        orgUnit.ParentId = dto.ParentId;
        orgUnit.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var result = await _db.OrgUnits
            .Include(o => o.Parent)
            .FirstAsync(o => o.Id == id);

        var childCount = await _db.OrgUnits.CountAsync(o => o.ParentId == id);
        var memberCount2 = await _db.OrgUnitMembers.CountAsync(m => m.OrgUnitId == id);
        return MapToDto(result, childCount, memberCount2);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgUnit = await _db.OrgUnits.FirstOrDefaultAsync(o => o.Id == id);
        if (orgUnit is null) return NotFound();

        // Check if any children exist — suggest deactivation instead
        if (await _db.OrgUnits.AnyAsync(o => o.ParentId == id))
            return Conflict("Cannot delete an OrgUnit with children. Reassign or remove children first, or deactivate instead.");

        _db.OrgUnits.Remove(orgUnit);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Membership ─────

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<OrgUnitMemberResponseDto>>> GetMembers(Guid id)
    {
        var orgUnit = await _db.OrgUnits.FirstOrDefaultAsync(o => o.Id == id);
        if (orgUnit is null) return NotFound();

        var members = await _db.OrgUnitMembers
            .Where(m => m.OrgUnitId == id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Load user details from Identity
        var userIds = members.Select(m => m.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        return members.Select(m =>
        {
            users.TryGetValue(m.UserId, out var user);
            return new OrgUnitMemberResponseDto(
                m.Id,
                m.UserId,
                user?.UserName,
                user?.DisplayName,
                user?.Email,
                m.OrgUnitId,
                orgUnit.Name,
                m.CreatedAt);
        }).ToList();
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<OrgUnitMemberResponseDto>> AddMember(Guid id, OrgUnitMemberAddDto dto)
    {
        var orgUnit = await _db.OrgUnits.FirstOrDefaultAsync(o => o.Id == id);
        if (orgUnit is null) return NotFound();

        var user = await _db.Users.FindAsync(dto.UserId);
        if (user is null) return BadRequest($"User '{dto.UserId}' not found.");

        if (await _db.OrgUnitMembers.AnyAsync(m => m.OrgUnitId == id && m.UserId == dto.UserId))
            return Conflict("User is already a member of this OrgUnit.");

        var member = new OrgUnitMember
        {
            UserId = dto.UserId,
            OrgUnitId = id
        };

        _db.OrgUnitMembers.Add(member);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMembers), new { id },
            new OrgUnitMemberResponseDto(
                member.Id, member.UserId, user.UserName, user.DisplayName,
                user.Email, id, orgUnit.Name, member.CreatedAt));
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var member = await _db.OrgUnitMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrgUnitId == id);

        if (member is null) return NotFound();

        _db.OrgUnitMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── User's OrgUnits ─────

    [HttpGet("/api/users/{userId}/orgunits")]
    public async Task<ActionResult<List<UserOrgUnitResponseDto>>> GetUserOrgUnits(string userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var memberships = await _db.OrgUnitMembers
            .Include(m => m.OrgUnit)
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.OrgUnit.Code)
            .ToListAsync();

        return memberships.Select(m => new UserOrgUnitResponseDto(
            m.OrgUnitId,
            m.OrgUnit.Code,
            m.OrgUnit.Name,
            m.OrgUnit.Type.ToString(),
            m.Id,
            m.CreatedAt)).ToList();
    }

    // ───── Helpers ─────

    private async Task<bool> WouldCreateCycle(Guid orgUnitId, Guid proposedParentId)
    {
        var currentId = proposedParentId;
        var visited = new HashSet<Guid> { orgUnitId };

        while (currentId != Guid.Empty)
        {
            if (visited.Contains(currentId))
                return true;

            visited.Add(currentId);

            var parent = await _db.OrgUnits
                .Where(o => o.Id == currentId)
                .Select(o => o.ParentId)
                .FirstOrDefaultAsync();

            if (!parent.HasValue) break;
            currentId = parent.Value;
        }

        return false;
    }

    private static OrgUnitResponseDto MapToDto(OrgUnit o, int childCount = 0, int memberCount = 0)
    {
        return new OrgUnitResponseDto(
            o.Id,
            o.Code,
            o.Name,
            o.Type.ToString(),
            o.ParentId,
            o.Parent?.Name,
            o.IsActive,
            childCount,
            memberCount,
            o.CreatedAt,
            o.UpdatedAt);
    }
}
