using System.Security.Claims;
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
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IWebhookEventPublisher? _webhooks;

    public WarehouseController(ProcessManagerDbContext db, IWebhookEventPublisher? webhooks = null)
    {
        _db = db;
        _webhooks = webhooks;
    }

    // ───── Storage Locations ─────

    [HttpGet("locations")]
    public async Task<ActionResult<PaginatedResponse<StorageLocationResponseDto>>> GetLocations(
        [FromQuery] string? search = null,
        [FromQuery] string? zone = null,
        [FromQuery] bool? active = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.StorageLocations.Include(sl => sl.Items).Include(sl => sl.Parent).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(sl => sl.Code.ToLower().Contains(s) || sl.Name.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(zone))
            query = query.Where(sl => sl.Zone != null && sl.Zone.ToLower() == zone.Trim().ToLower());

        if (active.HasValue)
            query = query.Where(sl => sl.IsActive == active.Value);

        if (parentId.HasValue)
            query = query.Where(sl => sl.ParentId == parentId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(sl => sl.Zone).ThenBy(sl => sl.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<StorageLocationResponseDto>(
            items.Select(MapLocationToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("locations/{id:guid}")]
    public async Task<ActionResult<StorageLocationDetailDto>> GetLocation(Guid id)
    {
        var loc = await _db.StorageLocations
            .Include(sl => sl.Parent)
            .Include(sl => sl.Children)
            .Include(sl => sl.Items).ThenInclude(i => i.Kind)
            .FirstOrDefaultAsync(sl => sl.Id == id);

        if (loc is null) return NotFound();

        var children = loc.Children.Select(c => new StorageLocationResponseDto(
            c.Id, c.Code, c.Name, c.Zone, c.Aisle, c.Bay, c.Bin,
            c.ParentId, loc.Code, c.Description, c.IsActive, 0,
            c.CreatedAt, c.UpdatedAt)).ToList();

        var onHand = loc.Items
            .Where(i => i.StorageLocationId == id)
            .GroupBy(i => new { i.KindId, i.Kind.Code, i.Kind.Name, i.Kind.UnitOfMeasure, i.Kind.ReorderThreshold, i.Kind.ReorderQuantity })
            .Select(g => new OnHandSummaryDto(
                g.Key.KindId, g.Key.Code, g.Key.Name, g.Key.UnitOfMeasure,
                id, loc.Code, loc.Name, g.Count(),
                g.Key.ReorderThreshold, g.Key.ReorderQuantity))
            .ToList();

        var recentTxns = await _db.InventoryTransactions
            .Include(t => t.Item).ThenInclude(i => i.Kind)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Where(t => t.FromLocationId == id || t.ToLocationId == id)
            .OrderByDescending(t => t.TransactedAt)
            .Take(20)
            .ToListAsync();

        return new StorageLocationDetailDto(
            loc.Id, loc.Code, loc.Name, loc.Zone, loc.Aisle, loc.Bay, loc.Bin,
            loc.ParentId, loc.Parent?.Code, loc.Description, loc.IsActive,
            loc.Items.Count, loc.CreatedAt, loc.UpdatedAt,
            children, onHand, recentTxns.Select(MapTransactionToDto).ToList());
    }

    [HttpPost("locations")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<StorageLocationResponseDto>> CreateLocation(CreateStorageLocationDto dto)
    {
        if (await _db.StorageLocations.AnyAsync(sl => sl.Code == dto.Code))
            return Conflict($"A location with code '{dto.Code}' already exists.");

        if (dto.ParentId.HasValue)
        {
            var parent = await _db.StorageLocations.FindAsync(dto.ParentId.Value);
            if (parent is null) return BadRequest("Parent location not found.");
        }

        var loc = new StorageLocation
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Zone = dto.Zone?.Trim(),
            Aisle = dto.Aisle?.Trim(),
            Bay = dto.Bay?.Trim(),
            Bin = dto.Bin?.Trim(),
            ParentId = dto.ParentId,
            Description = dto.Description?.Trim()
        };

        _db.StorageLocations.Add(loc);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocation), new { id = loc.Id }, MapLocationToDto(loc));
    }

    [HttpPut("locations/{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<StorageLocationResponseDto>> UpdateLocation(Guid id, UpdateStorageLocationDto dto)
    {
        var loc = await _db.StorageLocations
            .Include(sl => sl.Items)
            .Include(sl => sl.Parent)
            .FirstOrDefaultAsync(sl => sl.Id == id);

        if (loc is null) return NotFound();

        // Prevent parent cycle
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
                return BadRequest("A location cannot be its own parent.");

            var ancestorId = dto.ParentId.Value;
            var visited = new HashSet<Guid> { id };
            while (ancestorId != Guid.Empty)
            {
                if (!visited.Add(ancestorId))
                    return BadRequest("Setting this parent would create a circular reference.");

                var ancestor = await _db.StorageLocations.FindAsync(ancestorId);
                if (ancestor is null) break;
                ancestorId = ancestor.ParentId ?? Guid.Empty;
            }
        }

        loc.Name = dto.Name.Trim();
        loc.Zone = dto.Zone?.Trim();
        loc.Aisle = dto.Aisle?.Trim();
        loc.Bay = dto.Bay?.Trim();
        loc.Bin = dto.Bin?.Trim();
        loc.ParentId = dto.ParentId;
        loc.Description = dto.Description?.Trim();
        loc.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return MapLocationToDto(loc);
    }

    [HttpDelete("locations/{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> DeactivateLocation(Guid id)
    {
        var loc = await _db.StorageLocations.Include(sl => sl.Items).FirstOrDefaultAsync(sl => sl.Id == id);
        if (loc is null) return NotFound();

        if (loc.Items.Any(i => i.StorageLocationId == id))
            return Conflict("Cannot deactivate a location that has items. Move items first.");

        loc.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── On-Hand ─────

    [HttpGet("on-hand")]
    public async Task<ActionResult<List<OnHandSummaryDto>>> GetOnHand(
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? kindId = null,
        [FromQuery] string? zone = null,
        [FromQuery] bool lowStockOnly = false)
    {
        var query = _db.Items
            .Include(i => i.Kind)
            .Include(i => i.StorageLocation)
            .Where(i => i.StorageLocationId != null)
            .AsQueryable();

        if (locationId.HasValue)
            query = query.Where(i => i.StorageLocationId == locationId.Value);

        if (kindId.HasValue)
            query = query.Where(i => i.KindId == kindId.Value);

        if (!string.IsNullOrWhiteSpace(zone))
            query = query.Where(i => i.StorageLocation != null && i.StorageLocation.Zone != null
                && i.StorageLocation.Zone.ToLower() == zone.Trim().ToLower());

        var items = await query.ToListAsync();

        var grouped = items
            .GroupBy(i => new
            {
                i.KindId,
                KindCode = i.Kind.Code,
                KindName = i.Kind.Name,
                i.Kind.UnitOfMeasure,
                i.Kind.ReorderThreshold,
                i.Kind.ReorderQuantity,
                i.StorageLocationId,
                LocationCode = i.StorageLocation?.Code,
                LocationName = i.StorageLocation?.Name
            })
            .Select(g => new OnHandSummaryDto(
                g.Key.KindId, g.Key.KindCode, g.Key.KindName, g.Key.UnitOfMeasure,
                g.Key.StorageLocationId, g.Key.LocationCode, g.Key.LocationName,
                g.Count(), g.Key.ReorderThreshold, g.Key.ReorderQuantity))
            .ToList();

        if (lowStockOnly)
            grouped = grouped.Where(g => g.ReorderThreshold.HasValue && g.QuantityOnHand < g.ReorderThreshold.Value).ToList();

        return grouped.OrderBy(g => g.KindCode).ThenBy(g => g.LocationCode).ToList();
    }

    // ───── Inventory Transactions ─────

    [HttpPost("transactions")]
    public async Task<ActionResult<InventoryTransactionResponseDto>> CreateTransaction(CreateInventoryTransactionDto dto)
    {
        if (!Enum.TryParse<InventoryTransactionType>(dto.TransactionType, true, out var txnType))
            return BadRequest($"Invalid transaction type '{dto.TransactionType}'.");

        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId);

        if (item is null) return BadRequest("Item not found.");

        StorageLocation? fromLoc = null;
        StorageLocation? toLoc = null;

        switch (txnType)
        {
            case InventoryTransactionType.Receipt:
                if (dto.FromLocationId.HasValue)
                    return BadRequest("Receipt transactions must not have a FromLocationId.");
                if (!dto.ToLocationId.HasValue)
                    return BadRequest("Receipt transactions require a ToLocationId.");
                toLoc = await _db.StorageLocations.FindAsync(dto.ToLocationId.Value);
                if (toLoc is null || !toLoc.IsActive)
                    return BadRequest("Destination location not found or inactive.");
                item.StorageLocationId = toLoc.Id;
                break;

            case InventoryTransactionType.Issue:
                if (!dto.FromLocationId.HasValue)
                    return BadRequest("Issue transactions require a FromLocationId.");
                if (dto.ToLocationId.HasValue)
                    return BadRequest("Issue transactions must not have a ToLocationId.");
                fromLoc = await _db.StorageLocations.FindAsync(dto.FromLocationId.Value);
                if (fromLoc is null) return BadRequest("Source location not found.");
                if (item.StorageLocationId != fromLoc.Id)
                    return BadRequest("Item is not in the specified source location.");
                item.StorageLocationId = null;
                break;

            case InventoryTransactionType.Transfer:
                if (!dto.FromLocationId.HasValue || !dto.ToLocationId.HasValue)
                    return BadRequest("Transfer transactions require both FromLocationId and ToLocationId.");
                if (dto.FromLocationId.Value == dto.ToLocationId.Value)
                    return BadRequest("Cannot transfer to the same location.");
                fromLoc = await _db.StorageLocations.FindAsync(dto.FromLocationId.Value);
                if (fromLoc is null) return BadRequest("Source location not found.");
                if (item.StorageLocationId != fromLoc.Id)
                    return BadRequest("Item is not in the specified source location.");
                toLoc = await _db.StorageLocations.FindAsync(dto.ToLocationId.Value);
                if (toLoc is null || !toLoc.IsActive)
                    return BadRequest("Destination location not found or inactive.");
                item.StorageLocationId = toLoc.Id;
                break;

            case InventoryTransactionType.Adjustment:
                if (!dto.ToLocationId.HasValue)
                    return BadRequest("Adjustment transactions require a ToLocationId.");
                toLoc = await _db.StorageLocations.FindAsync(dto.ToLocationId.Value);
                if (toLoc is null || !toLoc.IsActive)
                    return BadRequest("Destination location not found or inactive.");
                if (dto.FromLocationId.HasValue)
                {
                    fromLoc = await _db.StorageLocations.FindAsync(dto.FromLocationId.Value);
                    if (fromLoc is null) return BadRequest("Source location not found.");
                }
                item.StorageLocationId = toLoc.Id;
                break;

            default:
                return BadRequest("Manual transactions of this type are not allowed.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

        var txn = new InventoryTransaction
        {
            TransactionType = txnType,
            ItemId = item.Id,
            FromLocationId = fromLoc?.Id,
            ToLocationId = toLoc?.Id,
            Quantity = dto.Quantity,
            Notes = dto.Notes?.Trim(),
            TransactedAt = DateTime.UtcNow,
            TransactedByUserId = userId
        };

        _db.InventoryTransactions.Add(txn);
        await _db.SaveChangesAsync();

        // Reload with navigations
        txn.Item = item;
        txn.FromLocation = fromLoc;
        txn.ToLocation = toLoc;

        return CreatedAtAction(nameof(GetTransactions), null, MapTransactionToDto(txn));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PaginatedResponse<InventoryTransactionResponseDto>>> GetTransactions(
        [FromQuery] Guid? itemId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? transactionType = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.InventoryTransactions
            .Include(t => t.Item).ThenInclude(i => i.Kind)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .AsQueryable();

        if (itemId.HasValue)
            query = query.Where(t => t.ItemId == itemId.Value);

        if (locationId.HasValue)
            query = query.Where(t => t.FromLocationId == locationId.Value || t.ToLocationId == locationId.Value);

        if (!string.IsNullOrEmpty(transactionType) && Enum.TryParse<InventoryTransactionType>(transactionType, true, out var tt))
            query = query.Where(t => t.TransactionType == tt);

        if (dateFrom.HasValue)
            query = query.Where(t => t.TransactedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(t => t.TransactedAt <= dateTo.Value);

        var totalCount = await query.CountAsync();
        var txns = await query
            .OrderByDescending(t => t.TransactedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<InventoryTransactionResponseDto>(
            txns.Select(MapTransactionToDto).ToList(), totalCount, page, pageSize);
    }

    // ───── Dashboard ─────

    [HttpGet("dashboard")]
    public async Task<ActionResult<WarehouseDashboardDto>> GetDashboard()
    {
        var totalLocations = await _db.StorageLocations.CountAsync(sl => sl.IsActive);
        var totalOnHand = await _db.Items.CountAsync(i => i.StorageLocationId != null);

        // Low stock: group items by KindId, compare count to Kind.ReorderThreshold
        var itemsByKind = await _db.Items
            .Where(i => i.StorageLocationId != null)
            .Include(i => i.Kind)
            .GroupBy(i => new { i.KindId, i.Kind.Code, i.Kind.Name, i.Kind.UnitOfMeasure, i.Kind.ReorderThreshold, i.Kind.ReorderQuantity })
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var lowStock = itemsByKind
            .Where(g => g.Key.ReorderThreshold.HasValue && g.Count < g.Key.ReorderThreshold.Value)
            .Select(g => new LowStockKindDto(
                g.Key.KindId, g.Key.Code, g.Key.Name, g.Count,
                g.Key.ReorderThreshold!.Value, g.Key.ReorderQuantity, g.Key.UnitOfMeasure))
            .ToList();

        var pendingPickLists = await _db.PickLists.CountAsync(pl =>
            pl.Status == PickListStatus.Open || pl.Status == PickListStatus.PartiallyPicked);

        var recentTxns = await _db.InventoryTransactions
            .Include(t => t.Item).ThenInclude(i => i.Kind)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .OrderByDescending(t => t.TransactedAt)
            .Take(10)
            .ToListAsync();

        return new WarehouseDashboardDto(
            totalLocations, totalOnHand, lowStock.Count, pendingPickLists,
            lowStock, recentTxns.Select(MapTransactionToDto).ToList());
    }

    // ───── Receive from Job ─────

    [HttpPost("receive-from-job")]
    public async Task<ActionResult<List<InventoryTransactionResponseDto>>> ReceiveFromJob(ReceiveItemsToWarehouseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
        var transactions = new List<InventoryTransaction>();

        foreach (var line in dto.Items)
        {
            var item = await _db.Items.Include(i => i.Kind).FirstOrDefaultAsync(i => i.Id == line.ItemId);
            if (item is null) return BadRequest($"Item {line.ItemId} not found.");

            var loc = await _db.StorageLocations.FindAsync(line.StorageLocationId);
            if (loc is null || !loc.IsActive)
                return BadRequest($"Location {line.StorageLocationId} not found or inactive.");

            item.StorageLocationId = loc.Id;

            var txn = new InventoryTransaction
            {
                TransactionType = InventoryTransactionType.Receipt,
                ItemId = item.Id,
                ToLocationId = loc.Id,
                Quantity = 1,
                ReferenceType = InventoryReferenceType.Job,
                ReferenceId = item.JobId,
                TransactedAt = DateTime.UtcNow,
                TransactedByUserId = userId
            };

            _db.InventoryTransactions.Add(txn);
            txn.Item = item;
            txn.ToLocation = loc;
            transactions.Add(txn);
        }

        await _db.SaveChangesAsync();
        return transactions.Select(MapTransactionToDto).ToList();
    }

    // ───── Barcode Scan (Phase 21) ─────

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponseDto>> Scan(ScanRequestDto dto)
    {
        var workstationIdClaim = User.FindFirstValue("workstation_id");
        var apiKeyIdClaim = User.FindFirstValue("api_key_id");

        if (string.IsNullOrEmpty(workstationIdClaim) || string.IsNullOrEmpty(apiKeyIdClaim))
            return Unauthorized("This endpoint requires API key authentication with a workstation-scoped key.");

        var workstationId = Guid.Parse(workstationIdClaim);
        var apiKeyId = Guid.Parse(apiKeyIdClaim);
        var tenantIdClaim = User.FindFirstValue("tenant_id");
        var tenantId = !string.IsNullOrEmpty(tenantIdClaim) ? Guid.Parse(tenantIdClaim) : Guid.Empty;

        var ws = await _db.Workstations
            .Include(w => w.FixedLocation)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == workstationId);

        if (ws is null || !ws.IsActive || !ws.FixedLocation.IsActive)
        {
            var scanEvtInactive = new ScanEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkstationId = workstationId,
                ApiKeyId = apiKeyId,
                ScannedBarcode = dto.Barcode,
                Result = ScanResult.WorkstationInactive,
                ErrorMessage = "Workstation or its fixed location is deactivated.",
                ScannedAt = DateTime.UtcNow
            };
            _db.ScanEvents.Add(scanEvtInactive);
            await _db.SaveChangesAsync();
            return BadRequest(new ScanResponseDto("workstation_inactive", null, null, null, null, null, DateTime.UtcNow, "Workstation or its fixed location is deactivated."));
        }

        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.Barcode == dto.Barcode);

        if (item is null)
            item = await _db.Items
                .Include(i => i.Kind)
                .Include(i => i.StorageLocation)
                .FirstOrDefaultAsync(i => i.SerialNumber == dto.Barcode);

        if (item is null)
        {
            var scanEvtUnknown = new ScanEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkstationId = workstationId,
                ApiKeyId = apiKeyId,
                ScannedBarcode = dto.Barcode,
                Result = ScanResult.UnknownBarcode,
                ErrorMessage = $"Barcode '{dto.Barcode}' not found.",
                ScannedAt = DateTime.UtcNow
            };
            _db.ScanEvents.Add(scanEvtUnknown);
            await _db.SaveChangesAsync();
            FireScanWebhook("UnknownBarcode", scanEvtUnknown, null, ws, null, null);
            return NotFound(new ScanResponseDto("unknown_barcode", null, null, null, null, null, DateTime.UtcNow, $"Barcode '{dto.Barcode}' not found."));
        }

        if (item.Status != ItemStatus.Available && item.Status != ItemStatus.InProcess)
        {
            var scanEvtStatus = new ScanEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkstationId = workstationId,
                ApiKeyId = apiKeyId,
                ScannedBarcode = dto.Barcode,
                ItemId = item.Id,
                Result = ScanResult.InvalidItemStatus,
                ErrorMessage = $"Item status is '{item.Status}' — only Available or InProcess items can be scanned.",
                ScannedAt = DateTime.UtcNow
            };
            _db.ScanEvents.Add(scanEvtStatus);
            await _db.SaveChangesAsync();
            FireScanWebhook("InvalidItemStatus", scanEvtStatus, null, ws, item, null);
            return Conflict(new ScanResponseDto("invalid_item_status", null,
                new ScanItemDto(item.Id, item.Barcode, item.SerialNumber, item.Kind.Code, item.Kind.Name),
                null, null, null, DateTime.UtcNow,
                $"Item status is '{item.Status}'."));
        }

        if (item.StorageLocationId == ws.FixedLocationId)
        {
            var scanEvtAlready = new ScanEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkstationId = workstationId,
                ApiKeyId = apiKeyId,
                ScannedBarcode = dto.Barcode,
                ItemId = item.Id,
                Result = ScanResult.AlreadyAtLocation,
                ScannedAt = DateTime.UtcNow
            };
            _db.ScanEvents.Add(scanEvtAlready);
            await _db.SaveChangesAsync();
            FireScanWebhook("AlreadyAtLocation", scanEvtAlready, null, ws, item, null);
            return Ok(new ScanResponseDto("already_at_location", null,
                new ScanItemDto(item.Id, item.Barcode, item.SerialNumber, item.Kind.Code, item.Kind.Name),
                item.StorageLocationId.HasValue ? new ScanLocationDto(item.StorageLocationId.Value, item.StorageLocation?.Code ?? "") : null,
                new ScanLocationDto(ws.FixedLocationId, ws.FixedLocation.Code),
                new ScanWorkstationDto(ws.Id, ws.Code),
                DateTime.UtcNow, null));
        }

        var txnType = item.StorageLocationId.HasValue
            ? InventoryTransactionType.Transfer
            : InventoryTransactionType.Receipt;

        var fromLocationId = item.StorageLocationId;
        var fromLocationCode = item.StorageLocation?.Code;

        var txn = new InventoryTransaction
        {
            TransactionType = txnType,
            ItemId = item.Id,
            FromLocationId = fromLocationId,
            ToLocationId = ws.FixedLocationId,
            Quantity = 1,
            ReferenceType = InventoryReferenceType.Workstation,
            ReferenceId = ws.Id,
            TransactedAt = DateTime.UtcNow,
            TransactedByUserId = $"apikey:{User.FindFirstValue("api_key_prefix") ?? ""}"
        };

        item.StorageLocationId = ws.FixedLocationId;

        _db.InventoryTransactions.Add(txn);

        var scanEvt = new ScanEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkstationId = workstationId,
            ApiKeyId = apiKeyId,
            ScannedBarcode = dto.Barcode,
            ItemId = item.Id,
            TransactionId = txn.Id,
            Result = ScanResult.Transferred,
            ScannedAt = DateTime.UtcNow
        };
        _db.ScanEvents.Add(scanEvt);

        await _db.SaveChangesAsync();

        FireScanWebhook("Transferred", scanEvt, txn, ws, item, fromLocationCode);

        return Ok(new ScanResponseDto("transferred", txn.Id,
            new ScanItemDto(item.Id, item.Barcode, item.SerialNumber, item.Kind.Code, item.Kind.Name),
            fromLocationId.HasValue ? new ScanLocationDto(fromLocationId.Value, fromLocationCode ?? "") : null,
            new ScanLocationDto(ws.FixedLocationId, ws.FixedLocation.Code),
            new ScanWorkstationDto(ws.Id, ws.Code),
            txn.TransactedAt, null));
    }

    // ───── Scan Events Query (Phase 21) ─────

    [HttpGet("scan-events")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<PaginatedResponse<ScanEventResponseDto>>> GetScanEvents(
        [FromQuery] Guid? workstationId = null,
        [FromQuery] string? result = null,
        [FromQuery] string? barcode = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.ScanEvents
            .Include(s => s.Workstation)
            .Include(s => s.Item)
            .AsQueryable();

        if (workstationId.HasValue)
            query = query.Where(s => s.WorkstationId == workstationId.Value);

        if (!string.IsNullOrEmpty(result) && Enum.TryParse<ScanResult>(result, true, out var sr))
            query = query.Where(s => s.Result == sr);

        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(s => s.ScannedBarcode.Contains(barcode.Trim()));

        if (dateFrom.HasValue)
            query = query.Where(s => s.ScannedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.ScannedAt <= dateTo.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.ScannedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<ScanEventResponseDto>(
            items.Select(s => new ScanEventResponseDto(
                s.Id, s.WorkstationId, s.Workstation?.Code ?? "",
                s.ScannedBarcode, s.ItemId, s.Item?.SerialNumber,
                s.TransactionId, s.Result.ToString(), s.ErrorMessage, s.ScannedAt)).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Mapping Helpers ─────

    private void FireScanWebhook(string result, ScanEvent scanEvt, InventoryTransaction? txn, Workstation ws, Item? item, string? fromLocationCode)
    {
        _webhooks?.Publish("inventory.scan", new
        {
            result,
            scanEventId = scanEvt.Id,
            transactionId = txn?.Id,
            workstation = new { id = ws.Id, code = ws.Code },
            item = item is null ? null : new { id = item.Id, barcode = item.Barcode, kindCode = item.Kind?.Code },
            fromLocationCode,
            toLocationCode = ws.FixedLocation?.Code
        });
    }

    private static StorageLocationResponseDto MapLocationToDto(StorageLocation sl) => new(
        sl.Id, sl.Code, sl.Name, sl.Zone, sl.Aisle, sl.Bay, sl.Bin,
        sl.ParentId, sl.Parent?.Code, sl.Description, sl.IsActive,
        sl.Items?.Count ?? 0, sl.CreatedAt, sl.UpdatedAt);

    internal static InventoryTransactionResponseDto MapTransactionToDto(InventoryTransaction t) => new(
        t.Id, t.TransactionType.ToString(),
        t.ItemId, t.Item?.SerialNumber,
        t.Item?.Kind?.Code ?? "", t.Item?.Kind?.Name ?? "",
        t.FromLocationId, t.FromLocation?.Code,
        t.ToLocationId, t.ToLocation?.Code,
        t.Quantity, t.ReferenceType?.ToString(), t.ReferenceId,
        t.Notes, t.TransactedAt, t.TransactedByUserId);
}
