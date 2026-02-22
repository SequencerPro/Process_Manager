using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ItemsController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ItemResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] Guid? jobId = null,
        [FromQuery] Guid? kindId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.SerialNumber != null && i.SerialNumber.Contains(search));

        if (jobId.HasValue)
            query = query.Where(i => i.JobId == jobId.Value);

        if (kindId.HasValue)
            query = query.Where(i => i.KindId == kindId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ItemStatus>(status, true, out var s))
            query = query.Where(i => i.Status == s);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<ItemResponseDto>(
            items.Select(JobsController.MapItemToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemResponseDto>> GetById(Guid id)
    {
        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null) return NotFound();
        return JobsController.MapItemToDto(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemResponseDto>> Create(CreateItemDto dto)
    {
        // Validate job
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == dto.JobId);
        if (job is null) return BadRequest($"Job '{dto.JobId}' not found.");

        // Validate kind
        var kind = await _db.Kinds.FirstOrDefaultAsync(k => k.Id == dto.KindId);
        if (kind is null) return BadRequest($"Kind '{dto.KindId}' not found.");

        // Validate grade belongs to kind
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId);
        if (grade is null) return BadRequest($"Grade '{dto.GradeId}' does not belong to Kind '{kind.Name}'.");

        // Serial number required for serialized kinds
        if (kind.IsSerialized && string.IsNullOrWhiteSpace(dto.SerialNumber))
            return BadRequest($"Kind '{kind.Name}' is serialized — serial number is required.");

        // Serial number must be unique within kind
        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            if (await _db.Items.AnyAsync(i => i.KindId == dto.KindId && i.SerialNumber == dto.SerialNumber))
                return Conflict($"An item with serial number '{dto.SerialNumber}' already exists for Kind '{kind.Name}'.");
        }

        // Validate batch if provided
        if (dto.BatchId.HasValue)
        {
            if (!kind.IsBatchable)
                return BadRequest($"Kind '{kind.Name}' is not batchable.");

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == dto.BatchId.Value);
            if (batch is null) return BadRequest($"Batch '{dto.BatchId}' not found.");
            if (batch.KindId != dto.KindId)
                return BadRequest("Item Kind must match Batch Kind.");
            if (batch.JobId != dto.JobId)
                return BadRequest("Item Job must match Batch Job.");
        }

        var item = new Item
        {
            SerialNumber = dto.SerialNumber,
            KindId = dto.KindId,
            GradeId = dto.GradeId,
            JobId = dto.JobId,
            BatchId = dto.BatchId,
            Status = ItemStatus.Available
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var result = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .FirstAsync(i => i.Id == item.Id);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, JobsController.MapItemToDto(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemResponseDto>> Update(Guid id, UpdateItemDto dto)
    {
        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Include(i => i.Job)
            .Include(i => i.Batch)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null) return NotFound();

        if (dto.SerialNumber != null)
        {
            if (await _db.Items.AnyAsync(i => i.KindId == item.KindId && i.SerialNumber == dto.SerialNumber && i.Id != id))
                return Conflict($"An item with serial number '{dto.SerialNumber}' already exists for this Kind.");
            item.SerialNumber = dto.SerialNumber;
        }

        if (dto.BatchId.HasValue)
        {
            if (!item.Kind.IsBatchable)
                return BadRequest($"Kind '{item.Kind.Name}' is not batchable.");

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == dto.BatchId.Value);
            if (batch is null) return BadRequest($"Batch '{dto.BatchId}' not found.");
            if (batch.KindId != item.KindId)
                return BadRequest("Item Kind must match Batch Kind.");
            item.BatchId = dto.BatchId;
        }

        await _db.SaveChangesAsync();
        return JobsController.MapItemToDto(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();

        // Check for port transactions referencing this item
        if (await _db.PortTransactions.AnyAsync(pt => pt.ItemId == id))
            return Conflict("Cannot delete item — it has port transaction records.");

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Item Execution Data ─────

    [HttpGet("{id:guid}/data")]
    public async Task<ActionResult<List<ExecutionDataResponseDto>>> GetData(Guid id)
    {
        if (!await _db.Items.AnyAsync(i => i.Id == id)) return NotFound();

        var data = await _db.ExecutionData
            .Where(ed => ed.ItemId == id)
            .OrderBy(ed => ed.Key)
            .ToListAsync();

        return data.Select(StepExecutionsController.MapExecutionDataToDto).ToList();
    }

    [HttpPost("{id:guid}/data")]
    public async Task<ActionResult<ExecutionDataResponseDto>> AddData(Guid id, CreateExecutionDataDto dto)
    {
        if (!await _db.Items.AnyAsync(i => i.Id == id)) return NotFound();

        var ed = new ExecutionData
        {
            Key = dto.Key,
            Value = dto.Value,
            DataType = dto.DataType,
            UnitOfMeasure = dto.UnitOfMeasure,
            ItemId = id
        };

        _db.ExecutionData.Add(ed);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetData), new { id }, StepExecutionsController.MapExecutionDataToDto(ed));
    }
}
