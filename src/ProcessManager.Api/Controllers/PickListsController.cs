using System.Security.Claims;
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
[Route("api/picklists")]
public class PickListsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public PickListsController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PickListSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.PickLists
            .Include(pl => pl.Job)
            .Include(pl => pl.Lines)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PickListStatus>(status, true, out var s))
            query = query.Where(pl => pl.Status == s);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(pl => pl.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<PickListSummaryDto>(
            items.Select(pl => new PickListSummaryDto(
                pl.Id, pl.JobId, pl.Job?.Code ?? "",
                pl.Status.ToString(), pl.Lines.Count,
                pl.Lines.Count(l => l.Status == PickListLineStatus.ShortShipped),
                pl.GeneratedAt)).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Detail ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PickListResponseDto>> GetById(Guid id)
    {
        var pl = await _db.PickLists
            .Include(p => p.Job)
            .Include(p => p.Lines).ThenInclude(l => l.Kind)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.SourceLocation)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pl is null) return NotFound();

        return new PickListResponseDto(
            pl.Id, pl.JobId, pl.Job?.Code ?? "",
            pl.Status.ToString(), pl.GeneratedAt, pl.GeneratedByUserId,
            pl.Lines.Count,
            pl.Lines.Select(l => new PickListLineResponseDto(
                l.Id, l.KindId, l.Kind?.Code ?? "", l.Kind?.Name ?? "",
                l.Kind?.UnitOfMeasure,
                l.ItemId, l.Item?.SerialNumber,
                l.SourceLocationId, l.SourceLocation?.Code,
                l.RequiredQuantity, l.PickedQuantity, l.ConsumedQuantity,
                l.Status.ToString())).ToList());
    }

    // ───── Pick a line ─────

    [HttpPost("{id:guid}/lines/{lineId:guid}/pick")]
    public async Task<ActionResult<PickListLineResponseDto>> PickLine(Guid id, Guid lineId, PickLineDto dto)
    {
        var pl = await _db.PickLists
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pl is null) return NotFound();

        if (pl.Status != PickListStatus.Open && pl.Status != PickListStatus.PartiallyPicked)
            return BadRequest("Pick list is not in a pickable state.");

        var line = pl.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return NotFound("Pick list line not found.");

        if (line.Status != PickListLineStatus.Pending)
            return BadRequest("Line has already been picked or short-shipped.");

        var item = await _db.Items
            .Include(i => i.Kind)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId);

        if (item is null) return BadRequest("Item not found.");
        if (item.KindId != line.KindId) return BadRequest("Item Kind does not match the required Kind for this line.");
        if (item.Status != ItemStatus.Available) return BadRequest("Item is not available.");
        if (item.StorageLocationId != dto.SourceLocationId)
            return BadRequest("Item is not in the specified source location.");

        var loc = await _db.StorageLocations.FindAsync(dto.SourceLocationId);
        if (loc is null) return BadRequest("Source location not found.");

        // Update line
        line.ItemId = item.Id;
        line.SourceLocationId = loc.Id;
        line.PickedQuantity = dto.PickedQuantity;
        line.Status = PickListLineStatus.Picked;

        // Create Issue transaction
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            TransactionType = InventoryTransactionType.Issue,
            ItemId = item.Id,
            FromLocationId = loc.Id,
            Quantity = dto.PickedQuantity,
            ReferenceType = InventoryReferenceType.PickList,
            ReferenceId = pl.Id,
            TransactedAt = DateTime.UtcNow,
            TransactedByUserId = userId
        });

        // Item leaves the warehouse
        item.StorageLocationId = null;

        // Recalculate pick list status
        RecalculatePickListStatus(pl);

        await _db.SaveChangesAsync();

        // Reload for response
        await _db.Entry(line).Reference(l => l.Kind).LoadAsync();
        await _db.Entry(line).Reference(l => l.Item).LoadAsync();
        await _db.Entry(line).Reference(l => l.SourceLocation).LoadAsync();

        return new PickListLineResponseDto(
            line.Id, line.KindId, line.Kind?.Code ?? "", line.Kind?.Name ?? "",
            line.Kind?.UnitOfMeasure,
            line.ItemId, line.Item?.SerialNumber,
            line.SourceLocationId, line.SourceLocation?.Code,
            line.RequiredQuantity, line.PickedQuantity, line.ConsumedQuantity,
            line.Status.ToString());
    }

    // ───── Consume a line ─────

    [HttpPost("{id:guid}/lines/{lineId:guid}/consume")]
    public async Task<ActionResult<PickListLineResponseDto>> ConsumeLine(Guid id, Guid lineId, ConsumeLineDto dto)
    {
        var pl = await _db.PickLists
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pl is null) return NotFound();

        var line = pl.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return NotFound("Pick list line not found.");

        if (line.Status != PickListLineStatus.Picked)
            return BadRequest("Line must be picked before it can be consumed.");

        if (line.ItemId is null) return BadRequest("Line has no assigned item.");

        var item = await _db.Items.FindAsync(line.ItemId.Value);
        if (item is null) return BadRequest("Assigned item not found.");

        // Update line
        line.ConsumedQuantity = dto.ConsumedQuantity;
        line.Status = PickListLineStatus.Consumed;

        // Create PicklistConsumption transaction
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            TransactionType = InventoryTransactionType.PicklistConsumption,
            ItemId = item.Id,
            Quantity = dto.ConsumedQuantity,
            ReferenceType = InventoryReferenceType.PickList,
            ReferenceId = pl.Id,
            TransactedAt = DateTime.UtcNow,
            TransactedByUserId = userId
        });

        // Mark item as consumed
        item.Status = ItemStatus.Consumed;

        // Recalculate pick list status
        RecalculatePickListStatus(pl);

        await _db.SaveChangesAsync();

        // Reload for response
        await _db.Entry(line).Reference(l => l.Kind).LoadAsync();
        await _db.Entry(line).Reference(l => l.Item).LoadAsync();
        await _db.Entry(line).Reference(l => l.SourceLocation).LoadAsync();

        return new PickListLineResponseDto(
            line.Id, line.KindId, line.Kind?.Code ?? "", line.Kind?.Name ?? "",
            line.Kind?.UnitOfMeasure,
            line.ItemId, line.Item?.SerialNumber,
            line.SourceLocationId, line.SourceLocation?.Code,
            line.RequiredQuantity, line.PickedQuantity, line.ConsumedQuantity,
            line.Status.ToString());
    }

    // ───── Short-ship ─────

    [HttpPost("{id:guid}/short-ship")]
    public async Task<ActionResult<PickListResponseDto>> ShortShip(Guid id)
    {
        var pl = await _db.PickLists
            .Include(p => p.Job)
            .Include(p => p.Lines).ThenInclude(l => l.Kind)
            .Include(p => p.Lines).ThenInclude(l => l.Item)
            .Include(p => p.Lines).ThenInclude(l => l.SourceLocation)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pl is null) return NotFound();

        var pendingLines = pl.Lines.Where(l => l.Status == PickListLineStatus.Pending).ToList();
        if (!pendingLines.Any())
            return BadRequest("No pending lines to short-ship.");

        foreach (var line in pendingLines)
            line.Status = PickListLineStatus.ShortShipped;

        RecalculatePickListStatus(pl);
        await _db.SaveChangesAsync();

        return new PickListResponseDto(
            pl.Id, pl.JobId, pl.Job?.Code ?? "",
            pl.Status.ToString(), pl.GeneratedAt, pl.GeneratedByUserId,
            pl.Lines.Count,
            pl.Lines.Select(l => new PickListLineResponseDto(
                l.Id, l.KindId, l.Kind?.Code ?? "", l.Kind?.Name ?? "",
                l.Kind?.UnitOfMeasure,
                l.ItemId, l.Item?.SerialNumber,
                l.SourceLocationId, l.SourceLocation?.Code,
                l.RequiredQuantity, l.PickedQuantity, l.ConsumedQuantity,
                l.Status.ToString())).ToList());
    }

    // ───── Helpers ─────

    private static void RecalculatePickListStatus(PickList pl)
    {
        var lines = pl.Lines.ToList();
        if (lines.Count == 0) return;

        var allConsumed = lines.All(l => l.Status == PickListLineStatus.Consumed || l.Status == PickListLineStatus.ShortShipped);
        var allPicked = lines.All(l => l.Status == PickListLineStatus.Picked || l.Status == PickListLineStatus.ShortShipped || l.Status == PickListLineStatus.Consumed);
        var anyPicked = lines.Any(l => l.Status == PickListLineStatus.Picked || l.Status == PickListLineStatus.Consumed);

        if (allConsumed)
            pl.Status = PickListStatus.Consumed;
        else if (allPicked)
            pl.Status = PickListStatus.Picked;
        else if (anyPicked)
            pl.Status = PickListStatus.PartiallyPicked;
        else
            pl.Status = PickListStatus.Open;
    }
}
