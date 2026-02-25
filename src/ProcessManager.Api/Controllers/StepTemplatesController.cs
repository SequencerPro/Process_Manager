using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StepTemplatesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public StepTemplatesController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StepTemplateResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.StepTemplates
            .Include(s => s.Ports).ThenInclude(p => p.Kind)
            .Include(s => s.Ports).ThenInclude(p => p.Grade)
            .Include(s => s.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Code.Contains(search) || s.Name.Contains(search));

        if (active.HasValue)
            query = query.Where(s => s.IsActive == active.Value);

        var totalCount = await query.CountAsync();

        var steps = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<StepTemplateResponseDto>(
            steps.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StepTemplateResponseDto>> GetById(Guid id)
    {
        var step = await _db.StepTemplates
            .Include(s => s.Ports).ThenInclude(p => p.Kind)
            .Include(s => s.Ports).ThenInclude(p => p.Grade)
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (step is null) return NotFound();
        return MapToDto(step);
    }

    [HttpPost]
    public async Task<ActionResult<StepTemplateResponseDto>> Create(StepTemplateCreateDto dto)
    {
        if (await _db.StepTemplates.AnyAsync(s => s.Code == dto.Code))
            return Conflict($"A StepTemplate with code '{dto.Code}' already exists.");

        // Validate ports
        var validationErrors = await ValidatePorts(dto.Ports, dto.Pattern);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        var step = new StepTemplate
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Pattern = dto.Pattern
        };

        foreach (var portDto in dto.Ports)
        {
            step.Ports.Add(new Port
            {
                Name = portDto.Name,
                Direction = portDto.Direction,
                KindId = portDto.KindId,
                GradeId = portDto.GradeId,
                QtyRuleMode = portDto.QtyRuleMode,
                QtyRuleN = portDto.QtyRuleN,
                QtyRuleMin = portDto.QtyRuleMin,
                QtyRuleMax = portDto.QtyRuleMax,
                SortOrder = portDto.SortOrder
            });
        }

        _db.StepTemplates.Add(step);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        var result = await _db.StepTemplates
            .Include(s => s.Ports).ThenInclude(p => p.Kind)
            .Include(s => s.Ports).ThenInclude(p => p.Grade)
            .Include(s => s.Images)
            .FirstAsync(s => s.Id == step.Id);

        return CreatedAtAction(nameof(GetById), new { id = step.Id }, MapToDto(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StepTemplateResponseDto>> Update(Guid id, StepTemplateUpdateDto dto)
    {
        var step = await _db.StepTemplates
            .Include(s => s.Ports).ThenInclude(p => p.Kind)
            .Include(s => s.Ports).ThenInclude(p => p.Grade)
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (step is null) return NotFound();

        step.Name = dto.Name;
        step.Description = dto.Description;
        step.Pattern = dto.Pattern;
        if (dto.IsActive.HasValue) step.IsActive = dto.IsActive.Value;
        step.Version++;

        await _db.SaveChangesAsync();
        return MapToDto(step);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var step = await _db.StepTemplates.FindAsync(id);
        if (step is null) return NotFound();

        if (await _db.ProcessSteps.AnyAsync(ps => ps.StepTemplateId == id))
            return Conflict("Cannot delete a StepTemplate that is used in one or more Processes.");

        _db.StepTemplates.Remove(step);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Port sub-resources ────────────

    [HttpPost("{stepTemplateId:guid}/ports")]
    public async Task<ActionResult<PortResponseDto>> AddPort(Guid stepTemplateId, PortCreateDto dto)
    {
        var step = await _db.StepTemplates.FindAsync(stepTemplateId);
        if (step is null) return NotFound("StepTemplate not found.");

        var portErrors = await ValidatePort(dto);
        if (portErrors.Count > 0)
            return BadRequest(new { errors = portErrors });

        var port = new Port
        {
            StepTemplateId = stepTemplateId,
            Name = dto.Name,
            Direction = dto.Direction,
            KindId = dto.KindId,
            GradeId = dto.GradeId,
            QtyRuleMode = dto.QtyRuleMode,
            QtyRuleN = dto.QtyRuleN,
            QtyRuleMin = dto.QtyRuleMin,
            QtyRuleMax = dto.QtyRuleMax,
            SortOrder = dto.SortOrder
        };

        _db.Ports.Add(port);
        step.Version++;
        await _db.SaveChangesAsync();

        // Reload with nav props
        var result = await _db.Ports
            .Include(p => p.Kind)
            .Include(p => p.Grade)
            .FirstAsync(p => p.Id == port.Id);

        return CreatedAtAction(nameof(GetById), new { id = stepTemplateId }, MapPortToDto(result));
    }

    [HttpPut("{stepTemplateId:guid}/ports/{portId:guid}")]
    public async Task<ActionResult<PortResponseDto>> UpdatePort(Guid stepTemplateId, Guid portId, PortUpdateDto dto)
    {
        var port = await _db.Ports
            .Include(p => p.Kind)
            .Include(p => p.Grade)
            .FirstOrDefaultAsync(p => p.Id == portId && p.StepTemplateId == stepTemplateId);

        if (port is null) return NotFound();

        // Validate grade belongs to kind
        var gradeValid = await _db.Grades.AnyAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId);
        if (!gradeValid)
            return BadRequest("Grade does not belong to the specified Kind.");

        port.Name = dto.Name;
        port.KindId = dto.KindId;
        port.GradeId = dto.GradeId;
        port.QtyRuleMode = dto.QtyRuleMode;
        port.QtyRuleN = dto.QtyRuleN;
        port.QtyRuleMin = dto.QtyRuleMin;
        port.QtyRuleMax = dto.QtyRuleMax;
        port.SortOrder = dto.SortOrder;

        // Bump step version
        var step = await _db.StepTemplates.FindAsync(stepTemplateId);
        if (step is not null) step.Version++;

        await _db.SaveChangesAsync();

        // Reload
        port = await _db.Ports
            .Include(p => p.Kind)
            .Include(p => p.Grade)
            .FirstAsync(p => p.Id == portId);

        return MapPortToDto(port);
    }

    [HttpDelete("{stepTemplateId:guid}/ports/{portId:guid}")]
    public async Task<IActionResult> DeletePort(Guid stepTemplateId, Guid portId)
    {
        var port = await _db.Ports.FirstOrDefaultAsync(p => p.Id == portId && p.StepTemplateId == stepTemplateId);
        if (port is null) return NotFound();

        // Check if any flows reference this port
        if (await _db.Flows.AnyAsync(f => f.SourcePortId == portId || f.TargetPortId == portId))
            return Conflict("Cannot delete a Port that is referenced by one or more Flows.");

        _db.Ports.Remove(port);

        var step = await _db.StepTemplates.FindAsync(stepTemplateId);
        if (step is not null) step.Version++;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Image sub-resources ────────────

    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StepTemplateImageResponseDto>> UploadImage(
        Guid id,
        [FromForm] ImageUploadRequest request,
        [FromServices] IImageStorageService imageStorage)
    {
        var step = await _db.StepTemplates.FindAsync(id);
        if (step is null) return NotFound();

        var file = request.File;
        if (file is null) return BadRequest("No file was provided.");

        var (fileName, _) = await imageStorage.SaveAsync(file, "steptemplates");

        var sortOrder = await _db.StepTemplateImages.CountAsync(i => i.StepTemplateId == id);
        var image = new StepTemplateImage
        {
            StepTemplateId = id,
            FileName = fileName,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType,
            SortOrder = sortOrder
        };

        _db.StepTemplateImages.Add(image);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id }, MapImageToDto(image));
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        Guid id,
        Guid imageId,
        [FromServices] IImageStorageService imageStorage)
    {
        var image = await _db.StepTemplateImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.StepTemplateId == id);
        if (image is null) return NotFound();

        await imageStorage.DeleteAsync($"uploads/steptemplates/{image.FileName}");
        _db.StepTemplateImages.Remove(image);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ──────────── Validation ────────────

    private async Task<List<string>> ValidatePorts(List<PortCreateDto> ports, StepPattern pattern)
    {
        var errors = new List<string>();

        var inputCount = ports.Count(p => p.Direction == PortDirection.Input);
        var outputCount = ports.Count(p => p.Direction == PortDirection.Output);

        switch (pattern)
        {
            case StepPattern.Transform:
                if (inputCount != 1) errors.Add("Transform pattern requires exactly 1 input port.");
                if (outputCount != 1) errors.Add("Transform pattern requires exactly 1 output port.");
                break;
            case StepPattern.Assembly:
                if (inputCount < 2) errors.Add("Assembly pattern requires at least 2 input ports.");
                if (outputCount != 1) errors.Add("Assembly pattern requires exactly 1 output port.");
                break;
            case StepPattern.Division:
                if (inputCount != 1) errors.Add("Division pattern requires exactly 1 input port.");
                if (outputCount < 2) errors.Add("Division pattern requires at least 2 output ports.");
                break;
            // General: no constraints
        }

        foreach (var port in ports)
        {
            errors.AddRange(await ValidatePort(port));
        }

        return errors;
    }

    private async Task<List<string>> ValidatePort(PortCreateDto dto)
    {
        var errors = new List<string>();

        // Kind must exist
        if (!await _db.Kinds.AnyAsync(k => k.Id == dto.KindId))
        {
            errors.Add($"Kind {dto.KindId} not found.");
            return errors; // Can't validate grade without kind
        }

        // Grade must exist and belong to the kind
        if (!await _db.Grades.AnyAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId))
            errors.Add($"Grade {dto.GradeId} does not belong to Kind {dto.KindId}.");

        // Quantity rule validation
        switch (dto.QtyRuleMode)
        {
            case QuantityRuleMode.Exactly:
            case QuantityRuleMode.ZeroOrN:
                if (dto.QtyRuleN is null or <= 0)
                    errors.Add($"Port '{dto.Name}': {dto.QtyRuleMode} mode requires QtyRuleN > 0.");
                break;
            case QuantityRuleMode.Range:
                if (dto.QtyRuleMin is null || dto.QtyRuleMax is null)
                    errors.Add($"Port '{dto.Name}': Range mode requires both QtyRuleMin and QtyRuleMax.");
                else if (dto.QtyRuleMin > dto.QtyRuleMax)
                    errors.Add($"Port '{dto.Name}': QtyRuleMin must be ≤ QtyRuleMax.");
                break;
            case QuantityRuleMode.Unbounded:
                if (dto.QtyRuleMin is null)
                    errors.Add($"Port '{dto.Name}': Unbounded mode requires QtyRuleMin.");
                break;
        }

        return errors;
    }

    // ──────────── Mapping ────────────

    private static StepTemplateResponseDto MapToDto(StepTemplate step) => new(
        step.Id, step.Code, step.Name, step.Description,
        step.Pattern, step.Version, step.IsActive,
        step.CreatedAt, step.UpdatedAt,
        step.Ports.OrderBy(p => p.Direction).ThenBy(p => p.SortOrder).Select(MapPortToDto).ToList(),
        step.Images.OrderBy(i => i.SortOrder).Select(MapImageToDto).ToList()
    );

    private static PortResponseDto MapPortToDto(Port port) => new(
        port.Id, port.StepTemplateId, port.Name, port.Direction,
        port.KindId, port.Kind.Code, port.Kind.Name,
        port.GradeId, port.Grade.Code, port.Grade.Name,
        port.QtyRuleMode, port.QtyRuleN, port.QtyRuleMin, port.QtyRuleMax,
        port.SortOrder, port.CreatedAt, port.UpdatedAt
    );

    private static StepTemplateImageResponseDto MapImageToDto(StepTemplateImage img) => new(
        img.Id, img.StepTemplateId, img.FileName, img.OriginalFileName,
        img.MimeType, img.SortOrder,
        $"uploads/steptemplates/{img.FileName}",
        img.CreatedAt
    );
}
