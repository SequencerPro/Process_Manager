using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DomainVocabulariesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public DomainVocabulariesController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<DomainVocabularyResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.DomainVocabularies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Name.Contains(search));

        var totalCount = await query.CountAsync();

        var vocabs = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<DomainVocabularyResponseDto>(
            vocabs.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DomainVocabularyResponseDto>> GetById(Guid id)
    {
        var vocab = await _db.DomainVocabularies.FindAsync(id);
        if (vocab is null) return NotFound();
        return MapToDto(vocab);
    }

    [HttpPost]
    public async Task<ActionResult<DomainVocabularyResponseDto>> Create(DomainVocabularyCreateDto dto)
    {
        if (await _db.DomainVocabularies.AnyAsync(v => v.Name == dto.Name))
            return Conflict($"A vocabulary named '{dto.Name}' already exists.");

        var vocab = new DomainVocabulary
        {
            Name = dto.Name,
            TermKind = dto.TermKind,
            TermKindCode = dto.TermKindCode,
            TermGrade = dto.TermGrade,
            TermItem = dto.TermItem,
            TermItemId = dto.TermItemId,
            TermBatch = dto.TermBatch,
            TermBatchId = dto.TermBatchId,
            TermJob = dto.TermJob,
            TermWorkflow = dto.TermWorkflow,
            TermProcess = dto.TermProcess,
            TermStep = dto.TermStep
        };

        _db.DomainVocabularies.Add(vocab);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vocab.Id }, MapToDto(vocab));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DomainVocabularyResponseDto>> Update(Guid id, DomainVocabularyUpdateDto dto)
    {
        var vocab = await _db.DomainVocabularies.FindAsync(id);
        if (vocab is null) return NotFound();

        vocab.Name = dto.Name;
        vocab.TermKind = dto.TermKind;
        vocab.TermKindCode = dto.TermKindCode;
        vocab.TermGrade = dto.TermGrade;
        vocab.TermItem = dto.TermItem;
        vocab.TermItemId = dto.TermItemId;
        vocab.TermBatch = dto.TermBatch;
        vocab.TermBatchId = dto.TermBatchId;
        vocab.TermJob = dto.TermJob;
        vocab.TermWorkflow = dto.TermWorkflow;
        vocab.TermProcess = dto.TermProcess;
        vocab.TermStep = dto.TermStep;

        await _db.SaveChangesAsync();
        return MapToDto(vocab);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var vocab = await _db.DomainVocabularies.FindAsync(id);
        if (vocab is null) return NotFound();

        _db.DomainVocabularies.Remove(vocab);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static DomainVocabularyResponseDto MapToDto(DomainVocabulary v) => new(
        v.Id, v.Name,
        v.TermKind, v.TermKindCode, v.TermGrade,
        v.TermItem, v.TermItemId,
        v.TermBatch, v.TermBatchId,
        v.TermJob, v.TermWorkflow, v.TermProcess, v.TermStep,
        v.CreatedAt, v.UpdatedAt
    );
}
