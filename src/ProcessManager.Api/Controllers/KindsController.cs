using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KindsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public KindsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<KindResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Kinds.Include(k => k.Grades).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(k => k.Code.Contains(search) || k.Name.Contains(search));

        var totalCount = await query.CountAsync();

        var kinds = await query
            .OrderBy(k => k.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<KindResponseDto>(
            kinds.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KindResponseDto>> GetById(Guid id)
    {
        var kind = await _db.Kinds
            .Include(k => k.Grades)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kind is null) return NotFound();
        return MapToDto(kind);
    }

    [HttpPost]
    public async Task<ActionResult<KindResponseDto>> Create(KindCreateDto dto)
    {
        if (await _db.Kinds.AnyAsync(k => k.Code == dto.Code))
            return Conflict($"A Kind with code '{dto.Code}' already exists.");

        var kind = new Kind
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IsSerialized = dto.IsSerialized,
            IsBatchable = dto.IsBatchable
        };

        _db.Kinds.Add(kind);
        await _db.SaveChangesAsync();

        var result = await _db.Kinds
            .Include(k => k.Grades)
            .FirstAsync(k => k.Id == kind.Id);

        return CreatedAtAction(nameof(GetById), new { id = kind.Id }, MapToDto(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KindResponseDto>> Update(Guid id, KindUpdateDto dto)
    {
        var kind = await _db.Kinds
            .Include(k => k.Grades)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kind is null) return NotFound();

        kind.Name = dto.Name;
        kind.Description = dto.Description;
        kind.IsSerialized = dto.IsSerialized;
        kind.IsBatchable = dto.IsBatchable;

        await _db.SaveChangesAsync();
        return MapToDto(kind);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var kind = await _db.Kinds.FindAsync(id);
        if (kind is null) return NotFound();

        // Check if any ports reference this kind
        if (await _db.Ports.AnyAsync(p => p.KindId == id))
            return Conflict("Cannot delete a Kind that is referenced by one or more Ports.");

        _db.Kinds.Remove(kind);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Grade sub-resource ────────────

    [HttpPost("{kindId:guid}/grades")]
    public async Task<ActionResult<GradeResponseDto>> CreateGrade(Guid kindId, GradeCreateDto dto)
    {
        var kind = await _db.Kinds.FindAsync(kindId);
        if (kind is null) return NotFound("Kind not found.");

        if (await _db.Grades.AnyAsync(g => g.KindId == kindId && g.Code == dto.Code))
            return Conflict($"A Grade with code '{dto.Code}' already exists for this Kind.");

        // If this grade is default, clear any existing default
        if (dto.IsDefault)
        {
            var existingDefault = await _db.Grades
                .Where(g => g.KindId == kindId && g.IsDefault)
                .ToListAsync();
            foreach (var g in existingDefault) g.IsDefault = false;
        }

        var grade = new Grade
        {
            KindId = kindId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IsDefault = dto.IsDefault,
            SortOrder = dto.SortOrder
        };

        _db.Grades.Add(grade);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = kindId }, MapGradeToDto(grade));
    }

    [HttpPut("{kindId:guid}/grades/{gradeId:guid}")]
    public async Task<ActionResult<GradeResponseDto>> UpdateGrade(Guid kindId, Guid gradeId, GradeUpdateDto dto)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == gradeId && g.KindId == kindId);
        if (grade is null) return NotFound();

        if (dto.IsDefault)
        {
            var existingDefault = await _db.Grades
                .Where(g => g.KindId == kindId && g.IsDefault && g.Id != gradeId)
                .ToListAsync();
            foreach (var g in existingDefault) g.IsDefault = false;
        }

        grade.Name = dto.Name;
        grade.Description = dto.Description;
        grade.IsDefault = dto.IsDefault;
        grade.SortOrder = dto.SortOrder;

        await _db.SaveChangesAsync();
        return MapGradeToDto(grade);
    }

    [HttpDelete("{kindId:guid}/grades/{gradeId:guid}")]
    public async Task<IActionResult> DeleteGrade(Guid kindId, Guid gradeId)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == gradeId && g.KindId == kindId);
        if (grade is null) return NotFound();

        // Check if any ports reference this grade
        if (await _db.Ports.AnyAsync(p => p.GradeId == gradeId))
            return Conflict("Cannot delete a Grade that is referenced by one or more Ports.");

        _db.Grades.Remove(grade);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Mapping ────────────

    private static KindResponseDto MapToDto(Kind kind) => new(
        kind.Id, kind.Code, kind.Name, kind.Description,
        kind.IsSerialized, kind.IsBatchable,
        kind.CreatedAt, kind.UpdatedAt,
        kind.Grades.OrderBy(g => g.SortOrder).Select(MapGradeToDto).ToList()
    );

    private static GradeResponseDto MapGradeToDto(Grade grade) => new(
        grade.Id, grade.KindId, grade.Code, grade.Name,
        grade.Description, grade.IsDefault, grade.SortOrder,
        grade.CreatedAt, grade.UpdatedAt
    );
}
