using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using System.Text.Json;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/floor-plans")]
[Authorize(Roles = "Admin,Engineer")]
public class FloorPlansController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public FloorPlansController(ProcessManagerDbContext db) => _db = db;

    // ── Floor Plan CRUD ──

    [HttpGet]
    public async Task<IActionResult> GetAll(
        string? search = null, string? status = null, bool? active = null,
        int page = 1, int pageSize = 25)
    {
        var q = _db.FloorPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(f => f.Code.Contains(search) || f.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FloorPlanStatus>(status, true, out var s))
            q = q.Where(f => f.Status == s);
        if (active.HasValue)
            q = q.Where(f => f.IsActive == active.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(f => f.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new FloorPlanSummaryDto(
                f.Id, f.Code, f.Name, f.Description,
                f.Version, f.Status, f.IsActive, f.ThumbnailBase64,
                f.Workstations.Count, f.InventoryLocations.Count,
                f.CreatedAt, f.UpdatedAt, f.CreatedBy))
            .ToListAsync();

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var fp = await _db.FloorPlans.AsNoTracking()
            .Include(f => f.Workstations).ThenInclude(w => w.Equipment)
            .Include(f => f.Workstations).ThenInclude(w => w.OrgUnit)
            .Include(f => f.Workstations).ThenInclude(w => w.StorageLocation)
            .Include(f => f.Workstations).ThenInclude(w => w.Processes).ThenInclude(p => p.Process)
            .Include(f => f.Workstations).ThenInclude(w => w.Tools).ThenInclude(t => t.Kind)
            .Include(f => f.InventoryLocations).ThenInclude(l => l.StorageLocation)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp is null) return NotFound();

        return Ok(new FloorPlanDetailDto(
            fp.Id, fp.Code, fp.Name, fp.Description,
            fp.Version, fp.Status, fp.IsActive,
            fp.LayoutJson, fp.ThumbnailBase64,
            fp.Workstations.Select(MapWorkstation).ToList(),
            fp.InventoryLocations.Select(l => new FloorPlanInventoryLocationDto(
                l.Id, l.PlacementId, l.StorageLocationId,
                l.StorageLocation.Code, l.StorageLocation.Name)).ToList(),
            fp.CreatedAt, fp.UpdatedAt, fp.CreatedBy));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FloorPlanCreateDto dto)
    {
        if (await _db.FloorPlans.AnyAsync(f => f.Code == dto.Code))
            return BadRequest(new { error = "duplicate_code", message = $"Floor plan with code '{dto.Code}' already exists." });

        var fp = new FloorPlan
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description
        };
        _db.FloorPlans.Add(fp);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = fp.Id },
            new FloorPlanSummaryDto(fp.Id, fp.Code, fp.Name, fp.Description,
                fp.Version, fp.Status, fp.IsActive, null, 0, 0,
                fp.CreatedAt, fp.UpdatedAt, fp.CreatedBy));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FloorPlanUpdateDto dto)
    {
        var fp = await _db.FloorPlans.FindAsync(id);
        if (fp is null) return NotFound();

        fp.Name = dto.Name;
        fp.Description = dto.Description;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/layout")]
    public async Task<IActionResult> SaveLayout(Guid id, [FromBody] FloorPlanLayoutSaveDto dto)
    {
        var fp = await _db.FloorPlans.FindAsync(id);
        if (fp is null) return NotFound();
        if (fp.Status == FloorPlanStatus.Archived)
            return BadRequest(new { error = "archived", message = "Cannot modify an archived floor plan." });

        fp.LayoutJson = dto.LayoutJson;
        fp.Version++;
        await _db.SaveChangesAsync();
        return Ok(new { fp.Version });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var fp = await _db.FloorPlans.FindAsync(id);
        if (fp is null) return NotFound();
        fp.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var fp = await _db.FloorPlans.FindAsync(id);
        if (fp is null) return NotFound();
        if (fp.Status != FloorPlanStatus.Draft)
            return BadRequest(new { error = "invalid_status", message = "Only Draft floor plans can be published." });
        fp.Status = FloorPlanStatus.Published;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var fp = await _db.FloorPlans.FindAsync(id);
        if (fp is null) return NotFound();
        if (fp.Status != FloorPlanStatus.Published)
            return BadRequest(new { error = "invalid_status", message = "Only Published floor plans can be archived." });
        fp.Status = FloorPlanStatus.Archived;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Workstations ──

    [HttpGet("{id:guid}/workstations")]
    public async Task<IActionResult> GetWorkstations(Guid id)
    {
        if (!await _db.FloorPlans.AnyAsync(f => f.Id == id)) return NotFound();

        var ws = await _db.FloorPlanWorkstations.AsNoTracking()
            .Where(w => w.FloorPlanId == id)
            .Include(w => w.Equipment)
            .Include(w => w.OrgUnit)
            .Include(w => w.StorageLocation)
            .Include(w => w.Processes).ThenInclude(p => p.Process)
            .Include(w => w.Tools).ThenInclude(t => t.Kind)
            .ToListAsync();

        return Ok(ws.Select(MapWorkstation));
    }

    [HttpPost("{id:guid}/workstations")]
    public async Task<IActionResult> CreateWorkstation(Guid id, [FromBody] FloorPlanWorkstationCreateDto dto)
    {
        if (!await _db.FloorPlans.AnyAsync(f => f.Id == id)) return NotFound();
        if (await _db.FloorPlanWorkstations.AnyAsync(w => w.FloorPlanId == id && w.PlacementId == dto.PlacementId))
            return BadRequest(new { error = "duplicate_placement", message = $"Placement '{dto.PlacementId}' already linked." });

        var ws = new FloorPlanWorkstation
        {
            FloorPlanId = id,
            PlacementId = dto.PlacementId,
            EquipmentId = dto.EquipmentId,
            OrgUnitId = dto.OrgUnitId,
            StorageLocationId = dto.StorageLocationId
        };
        _db.FloorPlanWorkstations.Add(ws);
        await _db.SaveChangesAsync();
        return Created($"/api/floor-plans/{id}/workstations/{ws.Id}", new { ws.Id });
    }

    [HttpPut("{id:guid}/workstations/{wsId:guid}")]
    public async Task<IActionResult> UpdateWorkstation(Guid id, Guid wsId, [FromBody] FloorPlanWorkstationUpdateDto dto)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        ws.EquipmentId = dto.EquipmentId;
        ws.OrgUnitId = dto.OrgUnitId;
        ws.StorageLocationId = dto.StorageLocationId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/workstations/{wsId:guid}")]
    public async Task<IActionResult> DeleteWorkstation(Guid id, Guid wsId)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        _db.FloorPlanWorkstations.Remove(ws);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Workstation Processes ──

    [HttpPost("{id:guid}/workstations/{wsId:guid}/processes")]
    public async Task<IActionResult> AddProcess(Guid id, Guid wsId, [FromBody] FloorPlanWorkstationProcessCreateDto dto)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        if (!await _db.Set<Process>().AnyAsync(p => p.Id == dto.ProcessId))
            return BadRequest(new { error = "process_not_found" });
        if (await _db.FloorPlanWorkstationProcesses.AnyAsync(p => p.FloorPlanWorkstationId == wsId && p.ProcessId == dto.ProcessId))
            return BadRequest(new { error = "duplicate_process" });

        var wsp = new FloorPlanWorkstationProcess
        {
            FloorPlanWorkstationId = wsId,
            ProcessId = dto.ProcessId,
            SortOrder = dto.SortOrder
        };
        _db.FloorPlanWorkstationProcesses.Add(wsp);
        await _db.SaveChangesAsync();
        return Created("", new { wsp.Id });
    }

    [HttpDelete("{id:guid}/workstations/{wsId:guid}/processes/{procId:guid}")]
    public async Task<IActionResult> RemoveProcess(Guid id, Guid wsId, Guid procId)
    {
        var wsp = await _db.FloorPlanWorkstationProcesses
            .FirstOrDefaultAsync(p => p.Id == procId && p.FloorPlanWorkstationId == wsId);
        if (wsp is null) return NotFound();
        _db.FloorPlanWorkstationProcesses.Remove(wsp);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Workstation Tools ──

    [HttpPost("{id:guid}/workstations/{wsId:guid}/tools")]
    public async Task<IActionResult> AddTool(Guid id, Guid wsId, [FromBody] FloorPlanWorkstationToolCreateDto dto)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        if (!await _db.Kinds.AnyAsync(k => k.Id == dto.KindId))
            return BadRequest(new { error = "kind_not_found" });
        if (await _db.FloorPlanWorkstationTools.AnyAsync(t => t.FloorPlanWorkstationId == wsId && t.KindId == dto.KindId))
            return BadRequest(new { error = "duplicate_tool" });

        var tool = new FloorPlanWorkstationTool
        {
            FloorPlanWorkstationId = wsId,
            KindId = dto.KindId,
            Quantity = dto.Quantity,
            Notes = dto.Notes
        };
        _db.FloorPlanWorkstationTools.Add(tool);
        await _db.SaveChangesAsync();
        return Created("", new { tool.Id });
    }

    [HttpPut("{id:guid}/workstations/{wsId:guid}/tools/{toolId:guid}")]
    public async Task<IActionResult> UpdateTool(Guid id, Guid wsId, Guid toolId, [FromBody] FloorPlanWorkstationToolUpdateDto dto)
    {
        var tool = await _db.FloorPlanWorkstationTools
            .FirstOrDefaultAsync(t => t.Id == toolId && t.FloorPlanWorkstationId == wsId);
        if (tool is null) return NotFound();
        tool.Quantity = dto.Quantity;
        tool.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/workstations/{wsId:guid}/tools/{toolId:guid}")]
    public async Task<IActionResult> RemoveTool(Guid id, Guid wsId, Guid toolId)
    {
        var tool = await _db.FloorPlanWorkstationTools
            .FirstOrDefaultAsync(t => t.Id == toolId && t.FloorPlanWorkstationId == wsId);
        if (tool is null) return NotFound();
        _db.FloorPlanWorkstationTools.Remove(tool);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Inventory Locations ──

    [HttpPost("{id:guid}/inventory-locations")]
    public async Task<IActionResult> AddInventoryLocation(Guid id, [FromBody] FloorPlanInventoryLocationCreateDto dto)
    {
        if (!await _db.FloorPlans.AnyAsync(f => f.Id == id)) return NotFound();
        if (!await _db.StorageLocations.AnyAsync(l => l.Id == dto.StorageLocationId))
            return BadRequest(new { error = "location_not_found" });
        if (await _db.FloorPlanInventoryLocations.AnyAsync(l => l.FloorPlanId == id && l.StorageLocationId == dto.StorageLocationId))
            return BadRequest(new { error = "duplicate_location" });

        var loc = new FloorPlanInventoryLocation
        {
            FloorPlanId = id,
            PlacementId = dto.PlacementId,
            StorageLocationId = dto.StorageLocationId
        };
        _db.FloorPlanInventoryLocations.Add(loc);
        await _db.SaveChangesAsync();
        return Created("", new { loc.Id });
    }

    [HttpDelete("{id:guid}/inventory-locations/{locId:guid}")]
    public async Task<IActionResult> RemoveInventoryLocation(Guid id, Guid locId)
    {
        var loc = await _db.FloorPlanInventoryLocations
            .FirstOrDefaultAsync(l => l.Id == locId && l.FloorPlanId == id);
        if (loc is null) return NotFound();
        _db.FloorPlanInventoryLocations.Remove(loc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Material Flow Analysis ──

    [HttpPost("{id:guid}/analyse-material-flow")]
    public async Task<IActionResult> AnalyseMaterialFlow(Guid id, [FromBody] MaterialFlowRequestDto? dto = null)
    {
        var fp = await _db.FloorPlans.AsNoTracking()
            .Include(f => f.Workstations).ThenInclude(w => w.Processes).ThenInclude(p => p.Process)
                .ThenInclude(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate).ThenInclude(st => st.Ports)
            .Include(f => f.InventoryLocations).ThenInclude(l => l.StorageLocation)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp is null) return NotFound();

        // Parse layout to get element positions
        var layout = JsonSerializer.Deserialize<LayoutDocument>(fp.LayoutJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (layout is null) return BadRequest(new { error = "invalid_layout" });

        var elementPositions = layout.Elements
            .Where(e => e.Id != null)
            .ToDictionary(e => e.Id!, e => (CenterX: e.X + e.Width / 2.0, CenterY: e.Y + e.Height / 2.0, e.X, e.Y, e.Width, e.Height, Label: e.Label ?? e.Id!));

        // Collect material requirements per workstation
        var flows = new List<MaterialFlowLineDto>();
        var unresolved = new List<UnresolvedMaterialDto>();

        // Get on-hand inventory per location per kind
        var invLocationIds = fp.InventoryLocations.Select(l => l.StorageLocationId).ToList();
        var onHand = await _db.Items
            .Where(i => i.StorageLocationId != null && invLocationIds.Contains(i.StorageLocationId.Value)
                        && i.Status == Domain.Enums.ItemStatus.Available)
            .GroupBy(i => new { i.StorageLocationId, i.KindId })
            .Select(g => new { g.Key.StorageLocationId, g.Key.KindId, Count = g.Count() })
            .ToListAsync();

        var onHandLookup = onHand.ToLookup(x => x.KindId);

        foreach (var ws in fp.Workstations)
        {
            if (!elementPositions.TryGetValue(ws.PlacementId, out var wsPos)) continue;

            // Collect all input material KindIds from this workstation's processes
            var requiredKinds = ws.Processes
                .SelectMany(p => p.Process.ProcessSteps)
                .SelectMany(ps => ps.StepTemplate.Ports)
                .Where(port => port.Direction == Domain.Enums.PortDirection.Input
                            && port.PortType == Domain.Enums.PortType.Material
                            && port.KindId.HasValue)
                .Select(port => port.KindId!.Value)
                .Distinct()
                .ToList();

            foreach (var kindId in requiredKinds)
            {
                var kindEntity = await _db.Kinds.AsNoTracking().FirstOrDefaultAsync(k => k.Id == kindId);
                if (kindEntity is null) continue;

                // Find nearest inventory location with stock for this kind
                var candidates = fp.InventoryLocations
                    .Where(l => elementPositions.ContainsKey(l.PlacementId))
                    .Select(l =>
                    {
                        var locPos = elementPositions[l.PlacementId];
                        var stock = onHand.FirstOrDefault(x => x.StorageLocationId == l.StorageLocationId && x.KindId == kindId);
                        var qty = stock?.Count ?? 0;
                        var dx = wsPos.CenterX - locPos.CenterX;
                        var dy = wsPos.CenterY - locPos.CenterY;
                        var dist = Math.Sqrt(dx * dx + dy * dy);
                        return new { Location = l, Pos = locPos, Quantity = qty, Distance = dist };
                    })
                    .Where(c => dto?.IncludeEmptyLocations == true || c.Quantity > 0)
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();

                if (candidates is null)
                {
                    unresolved.Add(new UnresolvedMaterialDto(
                        ws.PlacementId, kindId, kindEntity.Code, kindEntity.Name, "no_inventory_location_with_stock"));
                    continue;
                }

                flows.Add(new MaterialFlowLineDto(
                    ws.PlacementId, elementPositions[ws.PlacementId].Label,
                    kindId, kindEntity.Code, kindEntity.Name,
                    candidates.Location.PlacementId, candidates.Pos.Label, candidates.Location.StorageLocation.Code,
                    candidates.Quantity, Math.Round(candidates.Distance, 1), Math.Round(candidates.Distance / 1000.0, 2),
                    new PointDto(candidates.Pos.CenterX, candidates.Pos.CenterY),
                    new PointDto(wsPos.CenterX, wsPos.CenterY)));
            }
        }

        return Ok(new MaterialFlowResultDto(flows, unresolved));
    }

    // ── Private helpers ──

    private static FloorPlanWorkstationDto MapWorkstation(FloorPlanWorkstation w) => new(
        w.Id, w.PlacementId,
        w.EquipmentId, w.Equipment?.Code, w.Equipment?.Name,
        w.OrgUnitId, w.OrgUnit?.Code, w.OrgUnit?.Name,
        w.StorageLocationId, w.StorageLocation?.Code,
        w.Processes.OrderBy(p => p.SortOrder).Select(p => new FloorPlanWorkstationProcessDto(
            p.Id, p.ProcessId, p.Process.Code, p.Process.Name, p.SortOrder)).ToList(),
        w.Tools.Select(t => new FloorPlanWorkstationToolDto(
            t.Id, t.KindId, t.Kind.Code, t.Kind.Name, t.Quantity, t.Notes)).ToList());

    // ── Layout JSON deserialization models (internal) ──

    private class LayoutDocument
    {
        public double CanvasWidth { get; set; }
        public double CanvasHeight { get; set; }
        public int GridSize { get; set; }
        public string? BackgroundColor { get; set; }
        public List<LayoutElement> Elements { get; set; } = new();
    }

    private class LayoutElement
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Label { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }
        public string? Fill { get; set; }
        public string? Stroke { get; set; }
        public double StrokeWidth { get; set; }
        public string? Icon { get; set; }
        public int ZIndex { get; set; }
        public string? UtilityType { get; set; }
        public List<PointModel>? Points { get; set; }
        public string? DashPattern { get; set; }
        public double FontSize { get; set; }
        public string? Color { get; set; }
        public bool Locked { get; set; }
    }

    private class PointModel
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
