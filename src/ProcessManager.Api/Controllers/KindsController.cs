using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

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
        [FromQuery] string? sourceType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Kinds.Include(k => k.Grades).Include(k => k.Documents).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(k => k.Code.Contains(search) || k.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(sourceType) && Enum.TryParse<KindSourceType>(sourceType, true, out var st))
            query = query.Where(k => k.SourceType == st);

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
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kind is null) return NotFound();
        return MapToDto(kind);
    }

    [Authorize(Roles = "Admin,Engineer")]
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
            IsBatchable = dto.IsBatchable,
            SourceType = dto.SourceType,
            UnitOfMeasure = dto.UnitOfMeasure,
            Cost = dto.Cost,
            Price = dto.Price,
            LeadTimeDays = dto.LeadTimeDays,
            Weight = dto.Weight,
            WeightUnit = dto.WeightUnit,
            RohsStatus = dto.RohsStatus,
            CountryOfOrigin = dto.CountryOfOrigin,
            Revision = dto.Revision,
            Notes = dto.Notes
        };

        // Vendor fields only relevant for Buy source type
        if (dto.SourceType == KindSourceType.Buy)
        {
            kind.VendorName = dto.VendorName;
            kind.VendorPartNumber = dto.VendorPartNumber;
        }

        _db.Kinds.Add(kind);
        await _db.SaveChangesAsync();

        var result = await _db.Kinds
            .Include(k => k.Grades)
            .Include(k => k.Documents)
            .FirstAsync(k => k.Id == kind.Id);

        return CreatedAtAction(nameof(GetById), new { id = kind.Id }, MapToDto(result));
    }

    [Authorize(Roles = "Admin,Engineer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KindResponseDto>> Update(Guid id, KindUpdateDto dto)
    {
        var kind = await _db.Kinds
            .Include(k => k.Grades)
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kind is null) return NotFound();

        kind.Name = dto.Name;
        kind.Description = dto.Description;
        kind.IsSerialized = dto.IsSerialized;
        kind.IsBatchable = dto.IsBatchable;
        kind.SourceType = dto.SourceType;
        kind.UnitOfMeasure = dto.UnitOfMeasure;
        kind.Cost = dto.Cost;
        kind.Price = dto.Price;
        kind.LeadTimeDays = dto.LeadTimeDays;
        kind.Weight = dto.Weight;
        kind.WeightUnit = dto.WeightUnit;
        kind.RohsStatus = dto.RohsStatus;
        kind.CountryOfOrigin = dto.CountryOfOrigin;
        kind.Revision = dto.Revision;
        kind.Notes = dto.Notes;

        // Vendor fields only relevant for Buy source type — null out otherwise
        if (dto.SourceType == KindSourceType.Buy)
        {
            kind.VendorName = dto.VendorName;
            kind.VendorPartNumber = dto.VendorPartNumber;
        }
        else
        {
            kind.VendorName = null;
            kind.VendorPartNumber = null;
        }

        await _db.SaveChangesAsync();
        return MapToDto(kind);
    }

    [Authorize(Roles = "Admin,Engineer")]
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

    [Authorize(Roles = "Admin,Engineer")]
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

    [Authorize(Roles = "Admin,Engineer")]
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

    [Authorize(Roles = "Admin,Engineer")]
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

    // ──────────── Document sub-resource ────────────

    [Authorize(Roles = "Admin,Engineer")]
    [HttpPost("{kindId:guid}/documents")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<KindDocumentResponseDto>> UploadDocument(
        Guid kindId,
        [FromForm] ImageUploadRequest request,
        [FromForm] string? title,
        [FromServices] IImageStorageService imageStorage)
    {
        var kind = await _db.Kinds.FindAsync(kindId);
        if (kind is null) return NotFound("Kind not found.");

        var file = request.File;
        if (file is null) return BadRequest("No file was provided.");

        var (fileName, _) = await imageStorage.SaveAsync(file, "kind-documents");

        var sortOrder = await _db.Set<KindDocument>().CountAsync(d => d.KindId == kindId);
        var doc = new KindDocument
        {
            KindId = kindId,
            FileName = fileName,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType,
            Title = title,
            SortOrder = sortOrder
        };

        _db.Set<KindDocument>().Add(doc);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = kindId }, MapDocumentToDto(doc));
    }

    [AllowAnonymous]
    [HttpGet("{kindId:guid}/documents/{docId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(
        Guid kindId, Guid docId)
    {
        var doc = await _db.Set<KindDocument>()
            .FirstOrDefaultAsync(d => d.Id == docId && d.KindId == kindId);
        if (doc is null) return NotFound();

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kind-documents");
        var filePath = Path.Combine(uploadsPath, doc.FileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound("File not found on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, doc.MimeType, doc.OriginalFileName);
    }

    [Authorize(Roles = "Admin,Engineer")]
    [HttpDelete("{kindId:guid}/documents/{docId:guid}")]
    public async Task<IActionResult> DeleteDocument(
        Guid kindId, Guid docId,
        [FromServices] IImageStorageService imageStorage)
    {
        var doc = await _db.Set<KindDocument>()
            .FirstOrDefaultAsync(d => d.Id == docId && d.KindId == kindId);
        if (doc is null) return NotFound();

        await imageStorage.DeleteAsync(Path.Combine("kind-documents", doc.FileName));
        _db.Set<KindDocument>().Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── 3D Model sub-resource ────────────

    private static readonly HashSet<string> AllowedModelExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".obj", ".glb", ".gltf", ".step", ".stp", ".iges", ".igs" };

    [Authorize(Roles = "Admin,Engineer")]
    [HttpPost("{kindId:guid}/model")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<KindResponseDto>> UploadModel(
        Guid kindId,
        [FromForm] ImageUploadRequest request,
        [FromServices] IImageStorageService imageStorage)
    {
        var kind = await _db.Kinds
            .Include(k => k.Grades)
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.Id == kindId);
        if (kind is null) return NotFound("Kind not found.");

        var file = request.File;
        if (file is null) return BadRequest("No file was provided.");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedModelExtensions.Contains(ext))
            return BadRequest($"Unsupported 3D model format. Allowed: {string.Join(", ", AllowedModelExtensions)}");

        // Delete old model if exists
        if (!string.IsNullOrEmpty(kind.ModelFileName))
            await imageStorage.DeleteAsync(Path.Combine("kind-models", kind.ModelFileName));

        var (fileName, _) = await imageStorage.SaveAsync(file, "kind-models");
        kind.ModelFileName = fileName;
        kind.ModelOriginalFileName = file.FileName;
        kind.ModelMimeType = file.ContentType;

        await _db.SaveChangesAsync();
        return MapToDto(kind);
    }

    [AllowAnonymous]
    [HttpGet("{kindId:guid}/model/download")]
    public async Task<IActionResult> DownloadModel(Guid kindId)
    {
        var kind = await _db.Kinds.FindAsync(kindId);
        if (kind is null) return NotFound();
        if (string.IsNullOrEmpty(kind.ModelFileName)) return NotFound("No 3D model attached.");

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kind-models", kind.ModelFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound("Model file not found on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, kind.ModelMimeType ?? "application/octet-stream", kind.ModelOriginalFileName);
    }

    [Authorize(Roles = "Admin,Engineer")]
    [HttpDelete("{kindId:guid}/model")]
    public async Task<IActionResult> DeleteModel(
        Guid kindId,
        [FromServices] IImageStorageService imageStorage)
    {
        var kind = await _db.Kinds.FindAsync(kindId);
        if (kind is null) return NotFound();
        if (string.IsNullOrEmpty(kind.ModelFileName)) return NotFound("No 3D model attached.");

        await imageStorage.DeleteAsync(Path.Combine("kind-models", kind.ModelFileName));
        kind.ModelFileName = null;
        kind.ModelOriginalFileName = null;
        kind.ModelMimeType = null;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Mapping ────────────

    private static KindResponseDto MapToDto(Kind kind) => new(
        kind.Id, kind.Code, kind.Name, kind.Description,
        kind.IsSerialized, kind.IsBatchable,
        kind.SourceType.ToString(),
        kind.UnitOfMeasure, kind.Cost, kind.Price,
        kind.VendorName, kind.VendorPartNumber,
        kind.LeadTimeDays, kind.Weight, kind.WeightUnit,
        kind.RohsStatus, kind.CountryOfOrigin, kind.Revision, kind.Notes,
        kind.ModelFileName, kind.ModelOriginalFileName, kind.ModelMimeType,
        kind.CreatedAt, kind.UpdatedAt,
        kind.Grades.OrderBy(g => g.SortOrder).Select(MapGradeToDto).ToList(),
        kind.Documents.OrderBy(d => d.SortOrder).Select(MapDocumentToDto).ToList()
    );

    private static GradeResponseDto MapGradeToDto(Grade grade) => new(
        grade.Id, grade.KindId, grade.Code, grade.Name,
        grade.Description, grade.IsDefault, grade.SortOrder,
        grade.CreatedAt, grade.UpdatedAt
    );

    private static KindDocumentResponseDto MapDocumentToDto(KindDocument doc) => new(
        doc.Id, doc.KindId, doc.FileName, doc.OriginalFileName,
        doc.MimeType, doc.Title, doc.SortOrder,
        doc.CreatedAt, doc.UpdatedAt
    );
}
