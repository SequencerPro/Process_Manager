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
public class BatchesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public BatchesController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BatchResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? jobId = null,
        [FromQuery] Guid? kindId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Include(b => b.Job)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Code.Contains(search));

        if (jobId.HasValue)
            query = query.Where(b => b.JobId == jobId.Value);

        if (kindId.HasValue)
            query = query.Where(b => b.KindId == kindId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BatchStatus>(status, true, out var s))
            query = query.Where(b => b.Status == s);

        var totalCount = await query.CountAsync();

        var batches = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<BatchResponseDto>(
            batches.Select(JobsController.MapBatchToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BatchResponseDto>> GetById(Guid id)
    {
        var batch = await _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch is null) return NotFound();
        return JobsController.MapBatchToDto(batch);
    }

    [HttpPost]
    public async Task<ActionResult<BatchResponseDto>> Create(CreateBatchDto dto)
    {
        // Validate job
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == dto.JobId);
        if (job is null) return BadRequest($"Job '{dto.JobId}' not found.");

        // Validate kind is batchable
        var kind = await _db.Kinds.FirstOrDefaultAsync(k => k.Id == dto.KindId);
        if (kind is null) return BadRequest($"Kind '{dto.KindId}' not found.");
        if (!kind.IsBatchable)
            return BadRequest($"Kind '{kind.Name}' is not batchable.");

        // Validate grade belongs to kind
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId);
        if (grade is null) return BadRequest($"Grade '{dto.GradeId}' does not belong to Kind '{kind.Name}'.");

        // Unique batch code
        if (await _db.Batches.AnyAsync(b => b.Code == dto.Code))
            return Conflict($"A Batch with code '{dto.Code}' already exists.");

        var batch = new Batch
        {
            Code = dto.Code,
            KindId = dto.KindId,
            GradeId = dto.GradeId,
            JobId = dto.JobId,
            Quantity = dto.Quantity,
            Status = BatchStatus.Open
        };

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync();

        var result = await _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Include(b => b.Job)
            .FirstAsync(b => b.Id == batch.Id);

        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, JobsController.MapBatchToDto(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BatchResponseDto>> Update(Guid id, UpdateBatchDto dto)
    {
        var batch = await _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch is null) return NotFound();

        if (batch.Status != BatchStatus.Open)
            return BadRequest($"Cannot update a batch with status '{batch.Status}'. Must be Open.");

        if (dto.Quantity.HasValue)
            batch.Quantity = dto.Quantity.Value;

        await _db.SaveChangesAsync();
        return JobsController.MapBatchToDto(batch);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return NotFound();

        if (await _db.PortTransactions.AnyAsync(pt => pt.BatchId == id))
            return Conflict("Cannot delete batch — it has port transaction records.");

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Batch Item Management ─────

    [HttpGet("{id:guid}/items")]
    public async Task<ActionResult<List<ItemResponseDto>>> GetItems(Guid id)
    {
        if (!await _db.Batches.AnyAsync(b => b.Id == id)) return NotFound();

        var items = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .Where(i => i.BatchId == id)
            .OrderBy(i => i.SerialNumber)
            .ToListAsync();

        return items.Select(JobsController.MapItemToDto).ToList();
    }

    [HttpPost("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ItemResponseDto>> AddItem(Guid id, Guid itemId)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return NotFound();

        if (batch.Status != BatchStatus.Open)
            return BadRequest($"Cannot add items to a batch with status '{batch.Status}'. Must be Open.");

        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item is null) return BadRequest($"Item '{itemId}' not found.");

        if (item.KindId != batch.KindId)
            return BadRequest("Item Kind must match Batch Kind.");

        if (item.JobId != batch.JobId)
            return BadRequest("Item must belong to the same Job as the Batch.");

        if (!item.Kind.IsBatchable)
            return BadRequest($"Kind '{item.Kind.Name}' is not batchable.");

        item.BatchId = id;
        item.GradeId = batch.GradeId; // Inherit batch grade

        await _db.SaveChangesAsync();
        return JobsController.MapItemToDto(item);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.BatchId == id);
        if (item is null) return NotFound();

        item.BatchId = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Batch Lifecycle ─────

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<BatchResponseDto>> Close(Guid id)
    {
        var batch = await _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch is null) return NotFound();

        if (batch.Status != BatchStatus.Open)
            return BadRequest($"Cannot close a batch with status '{batch.Status}'. Must be Open.");

        batch.Status = BatchStatus.Closed;
        await _db.SaveChangesAsync();
        return JobsController.MapBatchToDto(batch);
    }

    // ───── Batch Execution Data ─────

    [HttpGet("{id:guid}/data")]
    public async Task<ActionResult<List<ExecutionDataResponseDto>>> GetData(Guid id)
    {
        if (!await _db.Batches.AnyAsync(b => b.Id == id)) return NotFound();

        var data = await _db.ExecutionData
            .Where(ed => ed.BatchId == id)
            .OrderBy(ed => ed.Key)
            .ToListAsync();

        return data.Select(StepExecutionsController.MapExecutionDataToDto).ToList();
    }

    [HttpPost("{id:guid}/data")]
    public async Task<ActionResult<ExecutionDataResponseDto>> AddData(Guid id, CreateExecutionDataDto dto)
    {
        if (!await _db.Batches.AnyAsync(b => b.Id == id)) return NotFound();

        var ed = new ExecutionData
        {
            Key = dto.Key,
            Value = dto.Value,
            DataType = dto.DataType,
            UnitOfMeasure = dto.UnitOfMeasure,
            BatchId = id
        };

        _db.ExecutionData.Add(ed);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetData), new { id }, StepExecutionsController.MapExecutionDataToDto(ed));
    }
}
