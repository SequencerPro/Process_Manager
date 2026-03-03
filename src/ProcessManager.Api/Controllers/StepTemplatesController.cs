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
            .Include(s => s.Contents)
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
            .Include(s => s.Contents)
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
                PortType = portDto.PortType,
                KindId = portDto.KindId,
                GradeId = portDto.GradeId,
                QtyRuleMode = portDto.QtyRuleMode,
                QtyRuleN = portDto.QtyRuleN,
                QtyRuleMin = portDto.QtyRuleMin,
                QtyRuleMax = portDto.QtyRuleMax,
                DataType = portDto.DataType,
                Units = portDto.Units,
                NominalValue = portDto.NominalValue,
                LowerTolerance = portDto.LowerTolerance,
                UpperTolerance = portDto.UpperTolerance,
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
            .Include(s => s.Contents)
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
            .Include(s => s.Contents)
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
            PortType = dto.PortType,
            KindId = dto.KindId,
            GradeId = dto.GradeId,
            QtyRuleMode = dto.QtyRuleMode,
            QtyRuleN = dto.QtyRuleN,
            QtyRuleMin = dto.QtyRuleMin,
            QtyRuleMax = dto.QtyRuleMax,
            DataType = dto.DataType,
            Units = dto.Units,
            NominalValue = dto.NominalValue,
            LowerTolerance = dto.LowerTolerance,
            UpperTolerance = dto.UpperTolerance,
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

        port.Name = dto.Name;
        port.PortType = dto.PortType;

        if (dto.PortType == PortType.Material)
        {
            // Validate grade belongs to kind
            var gradeValid = await _db.Grades.AnyAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId);
            if (!gradeValid)
                return BadRequest("Grade does not belong to the specified Kind.");

            port.KindId = dto.KindId;
            port.GradeId = dto.GradeId;
            port.QtyRuleMode = dto.QtyRuleMode;
            port.QtyRuleN = dto.QtyRuleN;
            port.QtyRuleMin = dto.QtyRuleMin;
            port.QtyRuleMax = dto.QtyRuleMax;
            port.DataType = null;
            port.Units = null;
            port.NominalValue = null;
            port.LowerTolerance = null;
            port.UpperTolerance = null;
        }
        else if (dto.PortType is PortType.Parameter or PortType.Characteristic)
        {
            port.KindId = null;
            port.GradeId = null;
            port.QtyRuleMode = null;
            port.QtyRuleN = null;
            port.QtyRuleMin = null;
            port.QtyRuleMax = null;
            port.DataType = dto.DataType;
            port.Units = dto.Units;
            port.NominalValue = dto.NominalValue;
            port.LowerTolerance = dto.LowerTolerance;
            port.UpperTolerance = dto.UpperTolerance;
        }
        else // Condition
        {
            port.KindId = null;
            port.GradeId = null;
            port.QtyRuleMode = null;
            port.QtyRuleN = null;
            port.QtyRuleMin = null;
            port.QtyRuleMax = null;
            port.DataType = null;
            port.Units = null;
            port.NominalValue = null;
            port.LowerTolerance = null;
            port.UpperTolerance = null;
        }

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

    // ──────────── StepTemplateContent sub-resources ────────────

    [HttpGet("{id:guid}/content")]
    public async Task<ActionResult<List<StepTemplateContentResponseDto>>> GetContent(Guid id)
    {
        var st = await _db.StepTemplates
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (st is null) return NotFound();

        return st.Contents
            .OrderBy(c => c.SortOrder)
            .Select(MapStepTemplateContentToDto)
            .ToList();
    }

    [HttpPost("{id:guid}/content/text")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> AddTextBlock(
        Guid id, AddStepTemplateTextBlockDto dto)
    {
        var st = await _db.StepTemplates
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (st is null) return NotFound();

        var sortOrder = st.Contents.Any() ? st.Contents.Max(c => c.SortOrder) + 1 : 0;

        ContentCategory? category = null;
        if (dto.ContentCategory is not null &&
            Enum.TryParse<ContentCategory>(dto.ContentCategory, ignoreCase: true, out var parsedCat))
            category = parsedCat;

        var block = new StepTemplateContent
        {
            StepTemplateId = id,
            ContentType = StepContentType.Text,
            SortOrder = sortOrder,
            Body = dto.Body,
            ContentCategory = category,
            AcknowledgmentRequired = category == ContentCategory.Safety
        };

        _db.StepTemplateContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContent), new { id }, MapStepTemplateContentToDto(block));
    }

    [HttpPost("{id:guid}/content/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> AddImageBlock(
        Guid id,
        [FromForm] ImageUploadRequest request,
        [FromServices] IImageStorageService imageStorage)
    {
        var st = await _db.StepTemplates
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (st is null) return NotFound();

        var file = request.File;
        if (file is null) return BadRequest("No file was provided.");

        var (fileName, _) = await imageStorage.SaveAsync(file, "step-template-content");

        var sortOrder = st.Contents.Any() ? st.Contents.Max(c => c.SortOrder) + 1 : 0;
        var block = new StepTemplateContent
        {
            StepTemplateId = id,
            ContentType = StepContentType.Image,
            SortOrder = sortOrder,
            FileName = fileName,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType
        };

        _db.StepTemplateContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContent), new { id }, MapStepTemplateContentToDto(block));
    }

    [HttpPut("{id:guid}/content/{contentId:guid}")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> UpdateTextBlock(
        Guid id, Guid contentId, UpdateStepTemplateTextBlockDto dto)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var block = await _db.StepTemplateContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.StepTemplateId == id);
        if (block is null) return NotFound();
        if (block.ContentType != StepContentType.Text)
            return BadRequest("Only Text blocks can be updated via this endpoint.");

        block.Body = dto.Body;

        if (dto.ContentCategory is not null &&
            Enum.TryParse<ContentCategory>(dto.ContentCategory, ignoreCase: true, out var parsedCat))
        {
            block.ContentCategory = parsedCat;
            if (parsedCat == ContentCategory.Safety) block.AcknowledgmentRequired = true;
        }

        await _db.SaveChangesAsync();
        return MapStepTemplateContentToDto(block);
    }

    [HttpPost("{id:guid}/content/prompt")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> AddPromptBlock(
        Guid id, AddStepTemplatePromptBlockDto dto)
    {
        var st = await _db.StepTemplates
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (st is null) return NotFound();

        if (!Enum.TryParse<PromptType>(dto.PromptType, ignoreCase: true, out var promptType))
            return BadRequest($"Unknown PromptType '{dto.PromptType}'.");

        ContentCategory? category = null;
        if (dto.ContentCategory is not null &&
            Enum.TryParse<ContentCategory>(dto.ContentCategory, ignoreCase: true, out var parsedCat))
            category = parsedCat;

        var sortOrder = st.Contents.Any() ? st.Contents.Max(c => c.SortOrder) + 1 : 0;
        var block = new StepTemplateContent
        {
            StepTemplateId = id,
            ContentType = StepContentType.Prompt,
            SortOrder = sortOrder,
            PromptType = promptType,
            Label = dto.Label,
            IsRequired = dto.IsRequired,
            Units = dto.Units,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Choices = dto.Choices,
            ContentCategory = category,
            AcknowledgmentRequired = category == ContentCategory.Safety,
            NominalValue = dto.NominalValue,
            IsHardLimit = dto.IsHardLimit
        };

        _db.StepTemplateContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContent), new { id }, MapStepTemplateContentToDto(block));
    }

    [HttpPut("{id:guid}/content/{contentId:guid}/prompt")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> UpdatePromptBlock(
        Guid id, Guid contentId, UpdateStepTemplatePromptBlockDto dto)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var block = await _db.StepTemplateContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.StepTemplateId == id);
        if (block is null) return NotFound();
        if (block.ContentType != StepContentType.Prompt)
            return BadRequest("Only Prompt blocks can be updated via this endpoint.");

        block.Label = dto.Label;
        block.IsRequired = dto.IsRequired;
        block.Units = dto.Units;
        block.MinValue = dto.MinValue;
        block.MaxValue = dto.MaxValue;
        block.Choices = dto.Choices;

        if (dto.ContentCategory is not null &&
            Enum.TryParse<ContentCategory>(dto.ContentCategory, ignoreCase: true, out var parsedCat))
        {
            block.ContentCategory = parsedCat;
            if (parsedCat == ContentCategory.Safety) block.AcknowledgmentRequired = true;
        }

        block.NominalValue = dto.NominalValue;
        block.IsHardLimit = dto.IsHardLimit;

        await _db.SaveChangesAsync();
        return MapStepTemplateContentToDto(block);
    }

    // PATCH: update ContentCategory (and optionally AcknowledgmentRequired) on any block type
    [HttpPatch("{id:guid}/content/{contentId:guid}/category")]
    public async Task<ActionResult<StepTemplateContentResponseDto>> PatchContentCategory(
        Guid id, Guid contentId, PatchContentCategoryDto dto)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var block = await _db.StepTemplateContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.StepTemplateId == id);
        if (block is null) return NotFound();

        if (dto.ContentCategory is null)
        {
            block.ContentCategory = null;
            block.AcknowledgmentRequired = false;
        }
        else
        {
            if (!Enum.TryParse<ContentCategory>(dto.ContentCategory, ignoreCase: true, out var cat))
                return BadRequest($"Unknown ContentCategory '{dto.ContentCategory}'.");
            block.ContentCategory = cat;
            // Safety blocks always require acknowledgment; others use the explicit flag if provided
            block.AcknowledgmentRequired = cat == ContentCategory.Safety
                || (dto.AcknowledgmentRequired ?? false);
        }

        await _db.SaveChangesAsync();
        return MapStepTemplateContentToDto(block);
    }

    [HttpPut("{id:guid}/content/reorder")]
    public async Task<IActionResult> ReorderContent(
        Guid id, ReorderStepTemplateContentBlocksDto dto)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var blocks = await _db.StepTemplateContents
            .Where(c => c.StepTemplateId == id)
            .ToListAsync();

        for (int i = 0; i < dto.OrderedIds.Count; i++)
        {
            var block = blocks.FirstOrDefault(c => c.Id == dto.OrderedIds[i]);
            if (block is not null) block.SortOrder = i;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/content/{contentId:guid}")]
    public async Task<IActionResult> DeleteContentBlock(
        Guid id, Guid contentId,
        [FromServices] IImageStorageService imageStorage)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var block = await _db.StepTemplateContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.StepTemplateId == id);
        if (block is null) return NotFound();

        if (block.ContentType == StepContentType.Image && block.FileName is not null)
            await imageStorage.DeleteAsync($"uploads/step-template-content/{block.FileName}");

        _db.StepTemplateContents.Remove(block);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Run Chart Widgets ────────────

    [HttpGet("{id:guid}/runcharts")]
    public async Task<ActionResult<List<RunChartWidgetResponseDto>>> GetRunChartWidgets(Guid id)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound();

        var widgets = await _db.RunChartWidgets
            .Include(w => w.SourceContent).ThenInclude(c => c.StepTemplate)
            .Where(w => w.StepTemplateId == id)
            .OrderBy(w => w.DisplayOrder)
            .ToListAsync();

        return widgets.Select(MapRunChartWidgetToDto).ToList();
    }

    [HttpPost("{id:guid}/runcharts")]
    public async Task<ActionResult<RunChartWidgetResponseDto>> AddRunChartWidget(
        Guid id, RunChartWidgetCreateDto dto)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound("StepTemplate not found.");

        var source = await _db.StepTemplateContents
            .Include(c => c.StepTemplate)
            .FirstOrDefaultAsync(c => c.Id == dto.SourceContentId);
        if (source is null) return BadRequest("SourceContentId does not reference a known content block.");
        if (source.ContentType != ProcessManager.Domain.Enums.StepContentType.Prompt ||
            source.PromptType != ProcessManager.Domain.Enums.PromptType.NumericEntry)
            return BadRequest("Source content block must be a NumericEntry prompt.");

        var widget = new ProcessManager.Domain.Entities.RunChartWidget
        {
            StepTemplateId = id,
            SourceContentId = dto.SourceContentId,
            Label = dto.Label,
            ChartWindowSize = dto.ChartWindowSize,
            SpecMin = dto.SpecMin,
            SpecMax = dto.SpecMax,
            DisplayOrder = dto.DisplayOrder
        };

        _db.RunChartWidgets.Add(widget);
        await _db.SaveChangesAsync();

        widget.SourceContent = source;
        return CreatedAtAction(nameof(GetRunChartWidgets), new { id }, MapRunChartWidgetToDto(widget));
    }

    [HttpPut("{id:guid}/runcharts/{widgetId:guid}")]
    public async Task<ActionResult<RunChartWidgetResponseDto>> UpdateRunChartWidget(
        Guid id, Guid widgetId, RunChartWidgetUpdateDto dto)
    {
        var widget = await _db.RunChartWidgets
            .Include(w => w.SourceContent).ThenInclude(c => c.StepTemplate)
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.StepTemplateId == id);
        if (widget is null) return NotFound();

        widget.Label = dto.Label;
        widget.ChartWindowSize = dto.ChartWindowSize;
        widget.SpecMin = dto.SpecMin;
        widget.SpecMax = dto.SpecMax;
        widget.DisplayOrder = dto.DisplayOrder;
        await _db.SaveChangesAsync();

        return MapRunChartWidgetToDto(widget);
    }

    [HttpDelete("{id:guid}/runcharts/{widgetId:guid}")]
    public async Task<IActionResult> DeleteRunChartWidget(Guid id, Guid widgetId)
    {
        var widget = await _db.RunChartWidgets
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.StepTemplateId == id);
        if (widget is null) return NotFound();

        _db.RunChartWidgets.Remove(widget);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Prompt History ────────────

    [HttpGet("{id:guid}/content/{contentId:guid}/prompt-history")]
    public async Task<ActionResult<List<PromptHistoryPointDto>>> GetPromptHistory(
        Guid id, Guid contentId, [FromQuery] int limit = 30)
    {
        var stExists = await _db.StepTemplates.AnyAsync(s => s.Id == id);
        if (!stExists) return NotFound("StepTemplate not found.");

        var contentExists = await _db.StepTemplateContents.AnyAsync(c => c.Id == contentId);
        if (!contentExists) return NotFound("Content block not found.");

        var points = await _db.PromptResponses
            .Where(r => r.StepTemplateContentId == contentId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new { r.CreatedAt, r.ResponseValue, r.IsOutOfRange })
            .ToListAsync();

        var result = points
            .Where(p => double.TryParse(p.ResponseValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            .Select(p => new PromptHistoryPointDto(
                p.CreatedAt,
                double.Parse(p.ResponseValue, System.Globalization.CultureInfo.InvariantCulture),
                p.IsOutOfRange))
            .OrderBy(p => p.Timestamp)
            .ToList();

        return result;
    }

    // ──────────── Maturity Scoring (Phase 8b) ────────────

    [HttpGet("{id:guid}/maturity")]
    public async Task<ActionResult<MaturityReportDto>> GetMaturity(Guid id)
    {
        var step = await _db.StepTemplates
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (step is null) return NotFound();

        return MaturityScoringService.Evaluate(step);
    }

    // ──────────── Validation ────────────

    private async Task<List<string>> ValidatePorts(List<PortCreateDto> ports, StepPattern pattern)
    {
        var errors = new List<string>();

        // Pattern checks apply to Material ports only
        var inputCount = ports.Count(p => p.Direction == PortDirection.Input && p.PortType == PortType.Material);
        var outputCount = ports.Count(p => p.Direction == PortDirection.Output && p.PortType == PortType.Material);

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

        // Kind/Grade and quantity-rule validation only apply to Material ports
        if (dto.PortType == PortType.Material)
        {
            // Kind must exist
            if (dto.KindId is null || !await _db.Kinds.AnyAsync(k => k.Id == dto.KindId))
            {
                errors.Add($"Port '{dto.Name}': KindId is required for Material ports and must exist.");
                return errors; // Can't validate grade without kind
            }

            // Grade must exist and belong to the kind
            if (!await _db.Grades.AnyAsync(g => g.Id == dto.GradeId && g.KindId == dto.KindId))
                errors.Add($"Port '{dto.Name}': Grade {dto.GradeId} does not belong to Kind {dto.KindId}.");

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
        }

        return errors;
    }

    // ──────────── Mapping ────────────

    private static RunChartWidgetResponseDto MapRunChartWidgetToDto(
        ProcessManager.Domain.Entities.RunChartWidget w) => new(
        w.Id, w.StepTemplateId, w.SourceContentId,
        w.SourceContent?.StepTemplateId ?? Guid.Empty,
        w.SourceContent?.StepTemplate?.Code ?? "",
        w.SourceContent?.StepTemplate?.Name ?? "",
        w.SourceContent?.Label,
        w.SourceContent?.Units,
        w.SourceContent?.MinValue,
        w.SourceContent?.MaxValue,
        w.Label, w.ChartWindowSize, w.SpecMin, w.SpecMax, w.DisplayOrder,
        w.CreatedAt, w.UpdatedAt
    );

    private static StepTemplateResponseDto MapToDto(StepTemplate step) => new(
        step.Id, step.Code, step.Name, step.Description,
        step.Pattern, step.Version, step.IsActive,
        step.CreatedAt, step.UpdatedAt,
        step.Ports.OrderBy(p => p.Direction).ThenBy(p => p.SortOrder).Select(MapPortToDto).ToList(),
        step.Images.OrderBy(i => i.SortOrder).Select(MapImageToDto).ToList(),
        MaturityScoringService.Summarise(step)
    );

    private static PortResponseDto MapPortToDto(Port port) => new(
        port.Id, port.StepTemplateId, port.Name, port.Direction,
        port.PortType,
        port.KindId, port.Kind?.Code, port.Kind?.Name,
        port.GradeId, port.Grade?.Code, port.Grade?.Name,
        port.QtyRuleMode, port.QtyRuleN, port.QtyRuleMin, port.QtyRuleMax,
        port.DataType, port.Units, port.NominalValue, port.LowerTolerance, port.UpperTolerance,
        port.SortOrder, port.CreatedAt, port.UpdatedAt
    );

    private static StepTemplateImageResponseDto MapImageToDto(StepTemplateImage img) => new(
        img.Id, img.StepTemplateId, img.FileName, img.OriginalFileName,
        img.MimeType, img.SortOrder,
        $"uploads/steptemplates/{img.FileName}",
        img.CreatedAt
    );

    private static StepTemplateContentResponseDto MapStepTemplateContentToDto(StepTemplateContent c) => new(
        c.Id, c.StepTemplateId,
        c.ContentType.ToString(),
        c.SortOrder,
        c.Body,
        c.FileName, c.OriginalFileName, c.MimeType,
        c.ContentType == StepContentType.Image && c.FileName is not null
            ? $"uploads/step-template-content/{c.FileName}"
            : null,
        c.CreatedAt,
        c.PromptType?.ToString(), c.Label, c.IsRequired, c.Units, c.MinValue, c.MaxValue, c.Choices,
        c.ContentCategory?.ToString(), c.AcknowledgmentRequired, c.NominalValue, c.IsHardLimit
    );
}

