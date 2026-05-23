using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
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
            .Include(f => f.InventoryLocations).ThenInclude(l => l.DesignatedKinds).ThenInclude(d => d.Kind)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp is null) return NotFound();

        return Ok(new FloorPlanDetailDto(
            fp.Id, fp.Code, fp.Name, fp.Description,
            fp.Version, fp.Status, fp.IsActive,
            fp.LayoutJson, fp.ThumbnailBase64,
            fp.Workstations.Select(MapWorkstation).ToList(),
            fp.InventoryLocations.Select(MapInventoryLocation).ToList(),
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

    // ── Workstation CAD Model (Phase 37) ──

    [HttpPost("{id:guid}/workstations/{wsId:guid}/model")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadWorkstationModel(
        Guid id, Guid wsId,
        [FromForm] ImageUploadRequest request,
        [FromServices] IImageStorageService storage)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();

        var file = request.File;
        if (file is null || file.Length == 0) return BadRequest(new { error = "no_file" });

        var classification = ProcessManager.Domain.Services.ModelFormatPolicy.Classify(file.FileName);
        if (!classification.IsSupported)
            return BadRequest(new
            {
                error = "unsupported_format",
                message = $"Allowed: {string.Join(", ", ProcessManager.Domain.Services.ModelFormatPolicy.AllowedExtensions)}"
            });

        // Remove any previous model artefacts.
        if (!string.IsNullOrEmpty(ws.ModelFileName))
            await storage.DeleteAsync($"floorplan-models/{ws.ModelFileName}");
        if (!string.IsNullOrEmpty(ws.ConvertedModelFileName))
            await storage.DeleteAsync($"floorplan-models/{ws.ConvertedModelFileName}");

        var (fileName, _) = await storage.SaveAsync(file, "floorplan-models");
        ws.ModelFileName = fileName;
        ws.ModelOriginalFileName = file.FileName;
        ws.ModelMimeType = ProcessManager.Domain.Services.ModelFormatPolicy.MimeTypeFor(file.FileName);
        ws.ConvertedModelFileName = null;
        ws.ConversionError = null;
        // Web-ready meshes are renderable immediately; CAD formats await conversion.
        ws.ConversionStatus = classification.InitialStatus;

        await _db.SaveChangesAsync();
        return Ok(MapWorkstationModel(ws));
    }

    /// <summary>
    /// Run (or retry) server-side CAD→glTF conversion for a workstation whose
    /// uploaded model needs it. Web-ready uploads return immediately. Heavy STEP
    /// assemblies are tessellated once here rather than in every browser.
    /// </summary>
    [HttpPost("{id:guid}/workstations/{wsId:guid}/model/convert")]
    public async Task<IActionResult> ConvertWorkstationModel(
        Guid id, Guid wsId,
        [FromServices] IStepConversionService converter)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        if (string.IsNullOrEmpty(ws.ModelFileName))
            return BadRequest(new { error = "no_model" });

        if (ws.ConversionStatus == Domain.Services.ModelConversionStatus.NotRequired
            || ws.ConversionStatus == Domain.Services.ModelConversionStatus.Converted)
            return Ok(MapWorkstationModel(ws)); // already renderable — no-op

        ws.ConversionStatus = Domain.Services.ModelConversionStatus.Converting;
        ws.ConversionError = null;
        await _db.SaveChangesAsync();

        var result = await converter.ConvertToGlbAsync($"floorplan-models/{ws.ModelFileName}");

        if (result.Success && !string.IsNullOrEmpty(result.ConvertedStorageKey))
        {
            ws.ConvertedModelFileName = result.ConvertedStorageKey.Contains('/')
                ? result.ConvertedStorageKey[(result.ConvertedStorageKey.LastIndexOf('/') + 1)..]
                : result.ConvertedStorageKey;
            ws.ConversionStatus = Domain.Services.ModelConversionStatus.Converted;
        }
        else
        {
            ws.ConversionStatus = Domain.Services.ModelConversionStatus.Failed;
            ws.ConversionError = result.Error;
        }

        await _db.SaveChangesAsync();
        return Ok(MapWorkstationModel(ws));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/workstations/{wsId:guid}/model/download")]
    public async Task<IActionResult> DownloadWorkstationModel(
        Guid id, Guid wsId,
        [FromQuery] bool converted,
        [FromServices] IImageStorageService storage)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();

        // Prefer the converted glTF when available and requested (or when the raw
        // upload isn't web-ready).
        var wantConverted = converted || ws.ConversionStatus == Domain.Services.ModelConversionStatus.Converted
                            && ws.ConversionStatus != Domain.Services.ModelConversionStatus.NotRequired;
        var fileName = wantConverted && !string.IsNullOrEmpty(ws.ConvertedModelFileName)
            ? ws.ConvertedModelFileName
            : ws.ModelFileName;
        if (string.IsNullOrEmpty(fileName)) return NotFound("No model attached.");

        var stream = await storage.GetStreamAsync($"floorplan-models/{fileName}");
        if (stream is null) return NotFound("Model file not found in storage.");

        var mime = fileName == ws.ConvertedModelFileName
            ? Domain.Services.ModelFormatPolicy.ConvertedMimeType
            : ws.ModelMimeType ?? "application/octet-stream";
        return File(stream, mime, ws.ModelOriginalFileName ?? fileName);
    }

    [HttpPut("{id:guid}/workstations/{wsId:guid}/model/transform")]
    public async Task<IActionResult> UpdateWorkstationModelTransform(
        Guid id, Guid wsId, [FromBody] FloorPlanWorkstationModelTransformDto dto)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();

        ws.ModelScale = dto.Scale <= 0 ? 1.0 : dto.Scale;
        ws.ModelYaw = dto.Yaw;
        ws.ModelOffsetX = dto.OffsetX;
        ws.ModelOffsetY = dto.OffsetY;
        ws.ModelOffsetZ = dto.OffsetZ;
        await _db.SaveChangesAsync();
        return Ok(MapWorkstationModel(ws));
    }

    [HttpDelete("{id:guid}/workstations/{wsId:guid}/model")]
    public async Task<IActionResult> DeleteWorkstationModel(
        Guid id, Guid wsId,
        [FromServices] IImageStorageService storage)
    {
        var ws = await _db.FloorPlanWorkstations.FirstOrDefaultAsync(w => w.Id == wsId && w.FloorPlanId == id);
        if (ws is null) return NotFound();
        if (string.IsNullOrEmpty(ws.ModelFileName)) return NotFound("No model attached.");

        await storage.DeleteAsync($"floorplan-models/{ws.ModelFileName}");
        if (!string.IsNullOrEmpty(ws.ConvertedModelFileName))
            await storage.DeleteAsync($"floorplan-models/{ws.ConvertedModelFileName}");

        ws.ModelFileName = null;
        ws.ModelOriginalFileName = null;
        ws.ModelMimeType = null;
        ws.ConvertedModelFileName = null;
        ws.ConversionError = null;
        ws.ConversionStatus = Domain.Services.ModelConversionStatus.None;
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

    // ── Inventory Location Designations (Phase 37 designed-flow) ──

    [HttpPost("{id:guid}/inventory-locations/{locId:guid}/designations")]
    public async Task<IActionResult> AddDesignation(Guid id, Guid locId, [FromBody] FloorPlanLocationDesignationCreateDto dto)
    {
        var loc = await _db.FloorPlanInventoryLocations.FirstOrDefaultAsync(l => l.Id == locId && l.FloorPlanId == id);
        if (loc is null) return NotFound();
        if (!await _db.Kinds.AnyAsync(k => k.Id == dto.KindId))
            return BadRequest(new { error = "kind_not_found" });
        if (await _db.FloorPlanInventoryLocationKinds.AnyAsync(d => d.FloorPlanInventoryLocationId == locId && d.KindId == dto.KindId))
            return BadRequest(new { error = "duplicate_designation" });

        var designation = new FloorPlanInventoryLocationKind
        {
            FloorPlanInventoryLocationId = locId,
            KindId = dto.KindId
        };
        _db.FloorPlanInventoryLocationKinds.Add(designation);
        await _db.SaveChangesAsync();
        return Created("", new { designation.Id });
    }

    [HttpDelete("{id:guid}/inventory-locations/{locId:guid}/designations/{designationId:guid}")]
    public async Task<IActionResult> RemoveDesignation(Guid id, Guid locId, Guid designationId)
    {
        var designation = await _db.FloorPlanInventoryLocationKinds
            .FirstOrDefaultAsync(d => d.Id == designationId && d.FloorPlanInventoryLocationId == locId);
        if (designation is null) return NotFound();
        _db.FloorPlanInventoryLocationKinds.Remove(designation);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Material Flow Analysis (Phase 37: dual-mode via MaterialFlowAnalyzer) ──

    [HttpPost("{id:guid}/analyse-material-flow")]
    public async Task<IActionResult> AnalyseMaterialFlow(Guid id, [FromBody] MaterialFlowRequestDto? dto = null)
    {
        dto ??= new MaterialFlowRequestDto();

        var fp = await _db.FloorPlans.AsNoTracking()
            .Include(f => f.Workstations).ThenInclude(w => w.Processes).ThenInclude(p => p.Process)
                .ThenInclude(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate).ThenInclude(st => st.Ports)
            .Include(f => f.InventoryLocations).ThenInclude(l => l.StorageLocation)
            .Include(f => f.InventoryLocations).ThenInclude(l => l.DesignatedKinds)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp is null) return NotFound();

        var layout = JsonSerializer.Deserialize<LayoutDocument>(fp.LayoutJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (layout is null) return BadRequest(new { error = "invalid_layout" });

        var elementPositions = layout.Elements
            .Where(e => e.Id != null)
            .ToDictionary(e => e.Id!, e => (CenterX: e.X + e.Width / 2.0, CenterY: e.Y + e.Height / 2.0, Label: e.Label ?? e.Id!));

        // On-hand inventory per location per kind (live mode).
        var invLocationIds = fp.InventoryLocations.Select(l => l.StorageLocationId).ToList();
        var onHand = await _db.Items
            .Where(i => i.StorageLocationId != null && invLocationIds.Contains(i.StorageLocationId.Value)
                        && i.Status == Domain.Enums.ItemStatus.Available)
            .GroupBy(i => new { i.StorageLocationId, i.KindId })
            .Select(g => new { g.Key.StorageLocationId, g.Key.KindId, Count = g.Count() })
            .ToListAsync();

        // Project workstations into analyzer inputs.
        var flowWorkstations = fp.Workstations
            .Where(ws => elementPositions.ContainsKey(ws.PlacementId))
            .Select(ws =>
            {
                var pos = elementPositions[ws.PlacementId];
                var requiredKinds = ws.Processes
                    .SelectMany(p => p.Process.ProcessSteps)
                    .SelectMany(ps => ps.StepTemplate.Ports)
                    .Where(port => port.Direction == Domain.Enums.PortDirection.Input
                                && port.PortType == Domain.Enums.PortType.Material
                                && port.KindId.HasValue)
                    .Select(port => port.KindId!.Value)
                    .Distinct()
                    .ToList();
                return new ProcessManager.Domain.Services.FlowWorkstation(
                    ws.PlacementId, pos.Label, pos.CenterX, pos.CenterY, requiredKinds);
            })
            .ToList();

        // Project inventory locations into analyzer inputs.
        var flowLocations = fp.InventoryLocations
            .Where(l => elementPositions.ContainsKey(l.PlacementId))
            .Select(l =>
            {
                var pos = elementPositions[l.PlacementId];
                var onHandByKind = onHand
                    .Where(x => x.StorageLocationId == l.StorageLocationId)
                    .ToDictionary(x => x.KindId, x => x.Count);
                var designated = l.DesignatedKinds.Select(d => d.KindId).ToHashSet();
                return new ProcessManager.Domain.Services.FlowInventoryLocation(
                    l.PlacementId, pos.Label, l.StorageLocation.Code, pos.CenterX, pos.CenterY,
                    onHandByKind, designated);
            })
            .ToList();

        // Kind metadata lookup for every kind referenced by a workstation.
        var allKindIds = flowWorkstations.SelectMany(w => w.RequiredKindIds).Distinct().ToList();
        var kindLookup = await _db.Kinds.AsNoTracking()
            .Where(k => allKindIds.Contains(k.Id))
            .ToDictionaryAsync(k => k.Id, k => new ProcessManager.Domain.Services.FlowKind(k.Id, k.Code, k.Name));

        var options = new ProcessManager.Domain.Services.MaterialFlowOptions(dto.Mode, dto.IncludeEmptyLocations);
        var result = ProcessManager.Domain.Services.MaterialFlowAnalyzer.Analyze(
            flowWorkstations, flowLocations, kindLookup, options);

        return Ok(new MaterialFlowResultDto(
            dto.Mode,
            result.Flows.Select(f => new MaterialFlowLineDto(
                f.WorkstationPlacementId, f.WorkstationLabel,
                f.KindId, f.KindCode, f.KindName,
                f.SourceLocationPlacementId, f.SourceLocationLabel, f.SourceLocationCode,
                f.OnHandQuantity, f.DistanceMm, f.DistanceM,
                new PointDto(f.FromPoint.X, f.FromPoint.Y),
                new PointDto(f.ToPoint.X, f.ToPoint.Y))).ToList(),
            result.Unresolved.Select(u => new UnresolvedMaterialDto(
                u.WorkstationPlacementId, u.KindId, u.KindCode, u.KindName, u.Reason)).ToList(),
            result.TotalTravelDistanceMm));
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
            t.Id, t.KindId, t.Kind.Code, t.Kind.Name, t.Quantity, t.Notes)).ToList(),
        w.ConversionStatus == Domain.Services.ModelConversionStatus.None ? null : MapWorkstationModel(w));

    private static FloorPlanWorkstationModelDto MapWorkstationModel(FloorPlanWorkstation w) => new(
        w.ModelOriginalFileName, w.ModelMimeType,
        w.ConversionStatus, w.HasRenderableModel, w.ConversionError,
        w.ModelScale, w.ModelYaw, w.ModelOffsetX, w.ModelOffsetY, w.ModelOffsetZ);

    private static FloorPlanInventoryLocationDto MapInventoryLocation(FloorPlanInventoryLocation l) => new(
        l.Id, l.PlacementId, l.StorageLocationId,
        l.StorageLocation.Code, l.StorageLocation.Name,
        l.DesignatedKinds.Select(d => new FloorPlanLocationDesignationDto(
            d.Id, d.KindId, d.Kind.Code, d.Kind.Name)).ToList());

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
