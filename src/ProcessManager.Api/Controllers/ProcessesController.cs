using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Enums;
using Process = ProcessManager.Domain.Entities.Process;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProcessesController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ProcessesController(ProcessManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProcessSummaryResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] string? status = null,
        [FromQuery] string? processRole = null,
        [FromQuery] bool excludeDocumentRoles = false,
        [FromQuery] bool documentRolesOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Processes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || p.Name.Contains(search));

        if (active.HasValue)
            query = query.Where(p => p.IsActive == active.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ProcessStatus>(status, ignoreCase: true, out var statusEnum))
            query = query.Where(p => p.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(processRole) &&
            Enum.TryParse<ProcessRole>(processRole, ignoreCase: true, out var roleEnum))
            query = query.Where(p => p.ProcessRole == roleEnum);
        else if (documentRolesOnly)
            query = query.Where(p => p.ProcessRole == Domain.Enums.ProcessRole.QmsDocument
                                  || p.ProcessRole == Domain.Enums.ProcessRole.WorkInstruction);
        else if (excludeDocumentRoles)
            query = query.Where(p => p.ProcessRole != Domain.Enums.ProcessRole.QmsDocument
                                  && p.ProcessRole != Domain.Enums.ProcessRole.WorkInstruction);

        var totalCount = await query.CountAsync();

        var rawProcesses = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.Id, p.Code, p.Name, p.Description,
                p.Version, p.Status, p.IsActive,
                StepCount = p.ProcessSteps.Count,
                p.CreatedAt, p.UpdatedAt,
                p.ProcessRole, p.ApprovalProcessId,
                p.RevisionCode, p.ChangeDescription, p.EffectiveDate
            })
            .ToListAsync();

        var processes = rawProcesses.Select(p => new ProcessSummaryResponseDto(
                p.Id, p.Code, p.Name, p.Description,
                p.Version, p.Status.ToString(), p.IsActive,
                p.StepCount, p.CreatedAt, p.UpdatedAt,
                p.ProcessRole.ToString(), p.ApprovalProcessId,
                p.RevisionCode, p.ChangeDescription, p.EffectiveDate)).ToList();

        return new PaginatedResponse<ProcessSummaryResponseDto>(
            processes, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcessResponseDto>> GetById(Guid id)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();
        return MapToDto(process);
    }

    [HttpPost]
    public async Task<ActionResult<ProcessResponseDto>> Create(ProcessCreateDto dto)
    {
        if (await _db.Processes.AnyAsync(p => p.Code == dto.Code))
            return Conflict($"A Process with code '{dto.Code}' already exists.");

        var processRole = Enum.TryParse<ProcessRole>(dto.ProcessRole, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : ProcessRole.ManufacturingProcess;

        var process = new Process
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ProcessRole = processRole
        };

        _db.Processes.Add(process);
        await _db.SaveChangesAsync();

        var result = await LoadProcess(process.Id);
        return CreatedAtAction(nameof(GetById), new { id = process.Id }, MapToDto(result!));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProcessResponseDto>> Update(Guid id, ProcessUpdateDto dto)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();

        if (process.Status == ProcessStatus.Released || process.Status == ProcessStatus.PendingApproval)
            return BadRequest($"A {process.Status} process cannot be edited directly. Create a new revision instead.");

        process.Name = dto.Name;
        process.Description = dto.Description;
        if (dto.IsActive.HasValue) process.IsActive = dto.IsActive.Value;
        process.Version++;

        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var process = await _db.Processes.FindAsync(id);
        if (process is null) return NotFound();

        _db.Processes.Remove(process); // Cascade deletes ProcessSteps and Flows
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── ProcessStep sub-resources ────────────

    [HttpPost("{processId:guid}/steps")]
    public async Task<ActionResult<ProcessStepResponseDto>> AddStep(Guid processId, ProcessStepCreateDto dto)
    {
        var process = await _db.Processes
            .Include(p => p.ProcessSteps)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process is null) return NotFound("Process not found.");

        var stepTemplate = await _db.StepTemplates.FindAsync(dto.StepTemplateId);
        if (stepTemplate is null) return BadRequest("StepTemplate not found.");

        // Validate sequence is contiguous
        var existingMax = process.ProcessSteps.Any()
            ? process.ProcessSteps.Max(ps => ps.Sequence)
            : 0;

        if (dto.Sequence != existingMax + 1)
            return BadRequest($"Sequence must be {existingMax + 1} (next in order). " +
                              $"Reordering is done via update.");

        var processStep = new ProcessStep
        {
            ProcessId = processId,
            StepTemplateId = dto.StepTemplateId,
            Sequence = dto.Sequence,
            NameOverride = dto.NameOverride,
            DescriptionOverride = dto.DescriptionOverride,
            PatternOverride = dto.PatternOverride
        };

        _db.ProcessSteps.Add(processStep);

        // Add port overrides if provided
        if (dto.PortOverrides is not null)
        {
            foreach (var po in dto.PortOverrides)
            {
                _db.ProcessStepPortOverrides.Add(new ProcessStepPortOverride
                {
                    ProcessStepId = processStep.Id,
                    PortId = po.PortId,
                    NameOverride = po.NameOverride,
                    DirectionOverride = po.DirectionOverride,
                    KindIdOverride = po.KindIdOverride,
                    GradeIdOverride = po.GradeIdOverride,
                    QtyRuleModeOverride = po.QtyRuleModeOverride,
                    QtyRuleNOverride = po.QtyRuleNOverride,
                    SortOrderOverride = po.SortOrderOverride
                });
            }
        }

        process.Version++;
        await _db.SaveChangesAsync();

        // Reload with nav props
        var result = await _db.ProcessSteps
            .Include(ps => ps.StepTemplate)
            .Include(ps => ps.PortOverrides).ThenInclude(po => po.KindOverride)
            .Include(ps => ps.PortOverrides).ThenInclude(po => po.GradeOverride)
            .FirstAsync(ps => ps.Id == processStep.Id);

        return CreatedAtAction(nameof(GetById), new { id = processId }, MapProcessStepToDto(result));
    }

    [HttpPut("{processId:guid}/steps/{stepId:guid}")]
    public async Task<ActionResult<ProcessStepResponseDto>> UpdateStep(
        Guid processId, Guid stepId, ProcessStepUpdateDto dto)
    {
        var processStep = await _db.ProcessSteps
            .Include(ps => ps.StepTemplate)
            .Include(ps => ps.PortOverrides)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);

        if (processStep is null) return NotFound();

        processStep.Sequence = dto.Sequence;
        processStep.NameOverride = dto.NameOverride;
        processStep.DescriptionOverride = dto.DescriptionOverride;
        processStep.PatternOverride = dto.PatternOverride;

        // Replace port overrides if provided
        if (dto.PortOverrides is not null)
        {
            _db.ProcessStepPortOverrides.RemoveRange(processStep.PortOverrides);

            foreach (var po in dto.PortOverrides)
            {
                _db.ProcessStepPortOverrides.Add(new ProcessStepPortOverride
                {
                    ProcessStepId = stepId,
                    PortId = po.PortId,
                    NameOverride = po.NameOverride,
                    DirectionOverride = po.DirectionOverride,
                    KindIdOverride = po.KindIdOverride,
                    GradeIdOverride = po.GradeIdOverride,
                    QtyRuleModeOverride = po.QtyRuleModeOverride,
                    QtyRuleNOverride = po.QtyRuleNOverride,
                    SortOrderOverride = po.SortOrderOverride
                });
            }
        }

        var process = await _db.Processes.FindAsync(processId);
        if (process is not null) process.Version++;

        await _db.SaveChangesAsync();

        // Reload with Kind/Grade nav props for response
        var result = await _db.ProcessSteps
            .Include(ps => ps.StepTemplate)
            .Include(ps => ps.PortOverrides).ThenInclude(po => po.KindOverride)
            .Include(ps => ps.PortOverrides).ThenInclude(po => po.GradeOverride)
            .FirstAsync(ps => ps.Id == stepId);

        return MapProcessStepToDto(result);
    }

    [HttpDelete("{processId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(Guid processId, Guid stepId)
    {
        var processStep = await _db.ProcessSteps
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);

        if (processStep is null) return NotFound();

        // Remove any flows referencing this step
        var relatedFlows = await _db.Flows
            .Where(f => f.SourceProcessStepId == stepId || f.TargetProcessStepId == stepId)
            .ToListAsync();

        _db.Flows.RemoveRange(relatedFlows);
        _db.ProcessSteps.Remove(processStep);

        // Resequence remaining steps
        var remainingSteps = await _db.ProcessSteps
            .Where(ps => ps.ProcessId == processId && ps.Id != stepId)
            .OrderBy(ps => ps.Sequence)
            .ToListAsync();

        for (int i = 0; i < remainingSteps.Count; i++)
            remainingSteps[i].Sequence = i + 1;

        var process = await _db.Processes.FindAsync(processId);
        if (process is not null) process.Version++;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Flow sub-resources ────────────

    [HttpPost("{processId:guid}/flows")]
    public async Task<ActionResult<FlowResponseDto>> AddFlow(Guid processId, FlowCreateDto dto)
    {
        var process = await _db.Processes
            .Include(p => p.ProcessSteps)
            .FirstOrDefaultAsync(p => p.Id == processId);

        if (process is null) return NotFound("Process not found.");

        // Validate source and target steps belong to this process
        var sourceStep = process.ProcessSteps.FirstOrDefault(ps => ps.Id == dto.SourceProcessStepId);
        var targetStep = process.ProcessSteps.FirstOrDefault(ps => ps.Id == dto.TargetProcessStepId);

        if (sourceStep is null) return BadRequest("Source step does not belong to this process.");
        if (targetStep is null) return BadRequest("Target step does not belong to this process.");

        // Validate adjacency
        if (sourceStep.Sequence + 1 != targetStep.Sequence)
            return BadRequest($"Flows must connect adjacent steps. " +
                              $"Source is at sequence {sourceStep.Sequence}, " +
                              $"target is at sequence {targetStep.Sequence}.");

        // Validate ports
        var sourcePort = await _db.Ports.Include(p => p.Kind).Include(p => p.Grade)
            .FirstOrDefaultAsync(p => p.Id == dto.SourcePortId
                && p.StepTemplateId == sourceStep.StepTemplateId
                && p.Direction == PortDirection.Output);

        var targetPort = await _db.Ports.Include(p => p.Kind).Include(p => p.Grade)
            .FirstOrDefaultAsync(p => p.Id == dto.TargetPortId
                && p.StepTemplateId == targetStep.StepTemplateId
                && p.Direction == PortDirection.Input);

        if (sourcePort is null)
            return BadRequest("Source port not found or is not an output port on the source step's template.");
        if (targetPort is null)
            return BadRequest("Target port not found or is not an input port on the target step's template.");

        // Flows are only allowed between Material ports
        if (sourcePort.PortType != PortType.Material || targetPort.PortType != PortType.Material)
            return BadRequest("Flows can only connect Material ports. Non-material ports (Parameter, Characteristic, Condition) are connected analytically, not via flows.");

        // Validate type compatibility (Kind + Grade must match)
        if (sourcePort.KindId != targetPort.KindId || sourcePort.GradeId != targetPort.GradeId)
            return BadRequest(
                $"Type mismatch: source port flows {sourcePort.Kind?.Code}/{sourcePort.Grade?.Code} " +
                $"but target port expects {targetPort.Kind?.Code}/{targetPort.Grade?.Code}.");

        // Check for duplicate connections
        if (await _db.Flows.AnyAsync(f => f.SourcePortId == dto.SourcePortId && f.ProcessId == processId))
            return Conflict("Source port is already connected to a flow in this process.");
        if (await _db.Flows.AnyAsync(f => f.TargetPortId == dto.TargetPortId && f.ProcessId == processId))
            return Conflict("Target port is already connected to a flow in this process.");

        var flow = new Flow
        {
            ProcessId = processId,
            SourceProcessStepId = dto.SourceProcessStepId,
            SourcePortId = dto.SourcePortId,
            TargetProcessStepId = dto.TargetProcessStepId,
            TargetPortId = dto.TargetPortId
        };

        _db.Flows.Add(flow);
        process.Version++;
        await _db.SaveChangesAsync();

        // Reload
        var result = await _db.Flows
            .Include(f => f.SourcePort)
            .Include(f => f.TargetPort)
            .FirstAsync(f => f.Id == flow.Id);

        return CreatedAtAction(nameof(GetById), new { id = processId }, MapFlowToDto(result));
    }

    [HttpDelete("{processId:guid}/flows/{flowId:guid}")]
    public async Task<IActionResult> DeleteFlow(Guid processId, Guid flowId)
    {
        var flow = await _db.Flows.FirstOrDefaultAsync(f => f.Id == flowId && f.ProcessId == processId);
        if (flow is null) return NotFound();

        _db.Flows.Remove(flow);

        var process = await _db.Processes.FindAsync(processId);
        if (process is not null) process.Version++;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── ProcessStepContent sub-resources ────────────

    [HttpGet("{processId:guid}/steps/{stepId:guid}/content")]
    public async Task<ActionResult<List<ProcessStepContentResponseDto>>> GetStepContent(
        Guid processId, Guid stepId)
    {
        var step = await _db.ProcessSteps
            .Include(ps => ps.Contents)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (step is null) return NotFound();

        return step.Contents
            .OrderBy(c => c.SortOrder)
            .Select(MapContentToDto)
            .ToList();
    }

    [HttpPost("{processId:guid}/steps/{stepId:guid}/content/text")]
    public async Task<ActionResult<ProcessStepContentResponseDto>> AddTextBlock(
        Guid processId, Guid stepId, AddTextBlockDto dto)
    {
        var step = await _db.ProcessSteps
            .Include(ps => ps.Contents)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (step is null) return NotFound();

        var sortOrder = step.Contents.Any() ? step.Contents.Max(c => c.SortOrder) + 1 : 0;
        var block = new ProcessStepContent
        {
            ProcessStepId = stepId,
            ContentType = StepContentType.Text,
            SortOrder = sortOrder,
            Body = dto.Body
        };

        _db.ProcessStepContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStepContent), new { processId, stepId }, MapContentToDto(block));
    }

    [HttpPost("{processId:guid}/steps/{stepId:guid}/content/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProcessStepContentResponseDto>> AddImageBlock(
        Guid processId, Guid stepId,
        [FromForm] ImageUploadRequest request,
        [FromServices] IImageStorageService imageStorage)
    {
        var step = await _db.ProcessSteps
            .Include(ps => ps.Contents)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (step is null) return NotFound();

        var file = request.File;
        if (file is null) return BadRequest("No file was provided.");

        var (fileName, _) = await imageStorage.SaveAsync(file, "process-steps");

        var sortOrder = step.Contents.Any() ? step.Contents.Max(c => c.SortOrder) + 1 : 0;
        var block = new ProcessStepContent
        {
            ProcessStepId = stepId,
            ContentType = StepContentType.Image,
            SortOrder = sortOrder,
            FileName = fileName,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType
        };

        _db.ProcessStepContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStepContent), new { processId, stepId }, MapContentToDto(block));
    }

    [HttpPut("{processId:guid}/steps/{stepId:guid}/content/{contentId:guid}")]
    public async Task<ActionResult<ProcessStepContentResponseDto>> UpdateTextBlock(
        Guid processId, Guid stepId, Guid contentId, UpdateTextBlockDto dto)
    {
        var stepExists = await _db.ProcessSteps
            .AnyAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (!stepExists) return NotFound();

        var block = await _db.ProcessStepContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.ProcessStepId == stepId);
        if (block is null) return NotFound();
        if (block.ContentType != StepContentType.Text)
            return BadRequest("Only Text blocks can be updated via this endpoint.");

        block.Body = dto.Body;
        await _db.SaveChangesAsync();
        return MapContentToDto(block);
    }

    [HttpPost("{processId:guid}/steps/{stepId:guid}/content/prompt")]
    public async Task<ActionResult<ProcessStepContentResponseDto>> AddPromptBlock(
        Guid processId, Guid stepId, AddPromptBlockDto dto)
    {
        var step = await _db.ProcessSteps
            .Include(ps => ps.Contents)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (step is null) return NotFound();

        if (!Enum.TryParse<PromptType>(dto.PromptType, ignoreCase: true, out var promptType))
            return BadRequest($"Unknown PromptType '{dto.PromptType}'.");

        var sortOrder = step.Contents.Any() ? step.Contents.Max(c => c.SortOrder) + 1 : 0;
        var block = new ProcessStepContent
        {
            ProcessStepId = stepId,
            ContentType = StepContentType.Prompt,
            SortOrder = sortOrder,
            PromptType = promptType,
            Label = dto.Label,
            IsRequired = dto.IsRequired,
            Units = dto.Units,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Choices = dto.Choices
        };

        _db.ProcessStepContents.Add(block);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStepContent), new { processId, stepId }, MapContentToDto(block));
    }

    [HttpPut("{processId:guid}/steps/{stepId:guid}/content/{contentId:guid}/prompt")]
    public async Task<ActionResult<ProcessStepContentResponseDto>> UpdatePromptBlock(
        Guid processId, Guid stepId, Guid contentId, UpdatePromptBlockDto dto)
    {
        var stepExists = await _db.ProcessSteps
            .AnyAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (!stepExists) return NotFound();

        var block = await _db.ProcessStepContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.ProcessStepId == stepId);
        if (block is null) return NotFound();
        if (block.ContentType != StepContentType.Prompt)
            return BadRequest("Only Prompt blocks can be updated via this endpoint.");

        block.Label = dto.Label;
        block.IsRequired = dto.IsRequired;
        block.Units = dto.Units;
        block.MinValue = dto.MinValue;
        block.MaxValue = dto.MaxValue;
        block.Choices = dto.Choices;
        await _db.SaveChangesAsync();
        return MapContentToDto(block);
    }

    [HttpPut("{processId:guid}/steps/{stepId:guid}/content/reorder")]
    public async Task<IActionResult> ReorderContent(
        Guid processId, Guid stepId, ReorderContentBlocksDto dto)
    {
        var stepExists = await _db.ProcessSteps
            .AnyAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (!stepExists) return NotFound();

        var blocks = await _db.ProcessStepContents
            .Where(c => c.ProcessStepId == stepId)
            .ToListAsync();

        for (int i = 0; i < dto.OrderedIds.Count; i++)
        {
            var block = blocks.FirstOrDefault(c => c.Id == dto.OrderedIds[i]);
            if (block is not null) block.SortOrder = i;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{processId:guid}/steps/{stepId:guid}/content/{contentId:guid}")]
    public async Task<IActionResult> DeleteContentBlock(
        Guid processId, Guid stepId, Guid contentId,
        [FromServices] IImageStorageService imageStorage)
    {
        var stepExists = await _db.ProcessSteps
            .AnyAsync(ps => ps.Id == stepId && ps.ProcessId == processId);
        if (!stepExists) return NotFound();

        var block = await _db.ProcessStepContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.ProcessStepId == stepId);
        if (block is null) return NotFound();

        if (block.ContentType == StepContentType.Image && block.FileName is not null)
            await imageStorage.DeleteAsync($"uploads/process-steps/{block.FileName}");

        _db.ProcessStepContents.Remove(block);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────── Validation Endpoint ────────────

    [HttpGet("{processId:guid}/validate")]
    public async Task<ActionResult<ProcessValidationResultDto>> Validate(Guid processId)
    {
        var process = await LoadProcess(processId);
        if (process is null) return NotFound();

        var warnings = new List<string>();
        var errors = new List<string>();

        var steps = process.ProcessSteps.OrderBy(ps => ps.Sequence).ToList();

        if (steps.Count == 0)
        {
            warnings.Add("Process has no steps.");
            return Ok(new ProcessValidationResultDto(errors, warnings));
        }

        // Check sequence contiguity
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].Sequence != i + 1)
                errors.Add($"Step sequence gap: expected {i + 1}, found {steps[i].Sequence}.");
        }

        // For each adjacent pair, check flow coverage
        for (int i = 0; i < steps.Count - 1; i++)
        {
            var current = steps[i];
            var next = steps[i + 1];

            // Load ports for both steps
            var currentOutputPorts = await _db.Ports
                .Where(p => p.StepTemplateId == current.StepTemplateId && p.Direction == PortDirection.Output)
                .ToListAsync();

            var nextInputPorts = await _db.Ports
                .Where(p => p.StepTemplateId == next.StepTemplateId && p.Direction == PortDirection.Input)
                .ToListAsync();

            var flowsBetween = process.Flows
                .Where(f => f.SourceProcessStepId == current.Id && f.TargetProcessStepId == next.Id)
                .ToList();

            // Check for unconnected output ports (interior steps)
            foreach (var port in currentOutputPorts)
            {
                if (!flowsBetween.Any(f => f.SourcePortId == port.Id))
                    warnings.Add($"Step {current.Sequence} output port '{port.Name}' is not connected to step {next.Sequence}.");
            }

            // Check for unconnected input ports
            foreach (var port in nextInputPorts)
            {
                if (!flowsBetween.Any(f => f.TargetPortId == port.Id))
                    warnings.Add($"Step {next.Sequence} input port '{port.Name}' is not connected from step {current.Sequence}.");
            }
        }

        return Ok(new ProcessValidationResultDto(errors, warnings));
    }

    // ──────────── Lifecycle ────────────

    /// <summary>Submit a Draft for approval. Status → PendingApproval.</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ProcessResponseDto>> Submit(Guid id, SubmitForApprovalDto dto)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();
        if (process.Status != ProcessStatus.Draft)
            return BadRequest($"Only Draft processes can be submitted (current: {process.Status}).");

        process.Status = ProcessStatus.PendingApproval;
        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            EntityType = "Process",
            EntityId = process.Id,
            ProcessId = process.Id,
            EntityVersion = process.Version,
            SubmittedBy = dto.SubmittedBy,
            SubmittedAt = DateTime.UtcNow,
            Decision = "Pending",
            Notes = dto.SubmissionNotes
        });
        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    /// <summary>Approve a PendingApproval process. Status → Released; supersedes prior Released.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ProcessResponseDto>> Approve(Guid id, ApproveDto dto)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();
        if (process.Status != ProcessStatus.PendingApproval)
            return BadRequest($"Only PendingApproval processes can be approved (current: {process.Status}).");

        // Supersede any currently Released version with the same code
        var priorReleased = await _db.Processes
            .Where(p => p.Code == process.Code && p.Status == ProcessStatus.Released && p.Id != process.Id)
            .ToListAsync();
        foreach (var prior in priorReleased)
            prior.Status = ProcessStatus.Superseded;

        process.Status = ProcessStatus.Released;

        // Flag linked PFMEAs as stale
        var linkedPfmeas = await _db.Pfmeas
            .Where(p => p.ProcessId == process.Id && !p.IsStale)
            .ToListAsync();
        foreach (var pfmea in linkedPfmeas)
        {
            pfmea.IsStale = true;
            pfmea.ProcessVersion = process.Version;
        }

        // Flag linked Control Plans as stale
        var linkedControlPlans = await _db.ControlPlans
            .Where(cp => cp.ProcessId == process.Id && !cp.IsStale)
            .ToListAsync();
        foreach (var cp in linkedControlPlans)
        {
            cp.IsStale = true;
            cp.ProcessVersion = process.Version;
        }

        // Update approval record
        var record = await _db.ApprovalRecords
            .Where(a => a.ProcessId == process.Id && a.Decision == "Pending")
            .OrderByDescending(a => a.SubmittedAt).FirstOrDefaultAsync();
        if (record is not null)
        {
            record.ReviewedBy = dto.ApprovedBy;
            record.ReviewedAt = DateTime.UtcNow;
            record.Decision = "Approved";
            record.Notes ??= dto.ApprovalNotes;
        }

        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    /// <summary>Reject a PendingApproval process. Status → Draft with rejection reason.</summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ProcessResponseDto>> Reject(Guid id, RejectDto dto)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();
        if (process.Status != ProcessStatus.PendingApproval)
            return BadRequest($"Only PendingApproval processes can be rejected (current: {process.Status}).");

        process.Status = ProcessStatus.Draft;
        var record = await _db.ApprovalRecords
            .Where(a => a.ProcessId == process.Id && a.Decision == "Pending")
            .OrderByDescending(a => a.SubmittedAt).FirstOrDefaultAsync();
        if (record is not null)
        {
            record.ReviewedBy = dto.RejectedBy;
            record.ReviewedAt = DateTime.UtcNow;
            record.Decision = "Rejected";
            record.Notes = dto.RejectionReason;
        }

        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    /// <summary>Create a new Draft revision from a Released process. Steps are copied; flows are not.</summary>
    [HttpPost("{id:guid}/new-revision")]
    public async Task<ActionResult<ProcessResponseDto>> NewRevision(Guid id, NewRevisionDto dto)
    {
        var source = await LoadProcess(id);
        if (source is null) return NotFound();
        if (source.Status != ProcessStatus.Released)
            return BadRequest($"Only Released processes can have new revisions created (current: {source.Status}).");

        var newProcess = new Process
        {
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            Version = source.Version + 1,
            IsActive = source.IsActive,
            Status = ProcessStatus.Draft,
            ParentProcessId = source.Id
        };
        _db.Processes.Add(newProcess);
        await _db.SaveChangesAsync();

        foreach (var ps in source.ProcessSteps.OrderBy(s => s.Sequence))
        {
            _db.ProcessSteps.Add(new ProcessStep
            {
                ProcessId = newProcess.Id,
                StepTemplateId = ps.StepTemplateId,
                Sequence = ps.Sequence,
                NameOverride = ps.NameOverride,
                DescriptionOverride = ps.DescriptionOverride
            });
        }
        await _db.SaveChangesAsync();

        var result = await LoadProcess(newProcess.Id);
        return CreatedAtAction(nameof(GetById), new { id = newProcess.Id }, MapToDto(result!));
    }

    /// <summary>Retire a Released or Superseded process.</summary>
    [HttpPost("{id:guid}/retire")]
    public async Task<ActionResult<ProcessResponseDto>> Retire(Guid id)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();
        if (process.Status == ProcessStatus.Draft || process.Status == ProcessStatus.PendingApproval)
            return BadRequest($"Draft and PendingApproval processes should be deleted, not retired.");

        process.Status = ProcessStatus.Retired;
        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    /// <summary>
    /// Admin bypass — directly release a QMS document from Draft or PendingApproval.
    /// Required for bootstrapping the QMS so the first ApprovalProcess templates can be
    /// released before the self-referential approval machinery is operational.
    /// Audited. Use sparingly after initial setup.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/admin-release")]
    public async Task<ActionResult<ProcessResponseDto>> AdminRelease(Guid id, [FromBody] AdminReleaseDocumentDto dto)
    {
        var process = await LoadProcess(id);
        if (process is null) return NotFound();

        if (process.Status != ProcessStatus.Draft && process.Status != ProcessStatus.PendingApproval)
            return BadRequest($"Process must be in Draft or PendingApproval to admin-release. Current status: {process.Status}.");

        if (!string.IsNullOrWhiteSpace(dto.ChangeDescription))
            process.ChangeDescription = dto.ChangeDescription;
        if (!string.IsNullOrWhiteSpace(dto.RevisionCode))
            process.RevisionCode = dto.RevisionCode;

        var releasedAt = DateTime.UtcNow;
        process.EffectiveDate = dto.EffectiveDate ?? releasedAt;

        // If there was a pending approval request, mark it superseded by admin action
        var pendingDar = await _db.DocumentApprovalRequests
            .Where(d => d.ProcessId == id && d.Status == DocumentApprovalStatus.Pending)
            .FirstOrDefaultAsync();

        if (pendingDar is not null)
        {
            pendingDar.Status = DocumentApprovalStatus.Withdrawn;
            var approvalJob = await _db.Jobs.FindAsync(pendingDar.ApprovalJobId);
            if (approvalJob is not null)
            {
                approvalJob.Status = JobStatus.Completed;
                approvalJob.CompletedAt = releasedAt;
            }
            var openSteps = await _db.StepExecutions
                .Where(s => s.JobId == pendingDar.ApprovalJobId
                    && (s.Status == StepExecutionStatus.Pending || s.Status == StepExecutionStatus.InProgress))
                .ToListAsync();
            foreach (var step in openSteps)
                step.Status = StepExecutionStatus.Skipped;
        }

        // Supersede previous Released revision in the same lineage
        if (process.ParentProcessId.HasValue)
        {
            var previousReleased = await _db.Processes
                .Where(p => p.Id == process.ParentProcessId.Value && p.Status == ProcessStatus.Released)
                .FirstOrDefaultAsync();
            if (previousReleased is not null)
                previousReleased.Status = ProcessStatus.Superseded;
        }

        process.Status = ProcessStatus.Released;
        await _db.SaveChangesAsync();
        return MapToDto(process);
    }

    // ──────────── Helpers ────────────

    private async Task<Process?> LoadProcess(Guid id)
    {
        return await _db.Processes
            .Include(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate).ThenInclude(st => st.Contents)
            .Include(p => p.ProcessSteps).ThenInclude(ps => ps.PortOverrides).ThenInclude(po => po.KindOverride)
            .Include(p => p.ProcessSteps).ThenInclude(ps => ps.PortOverrides).ThenInclude(po => po.GradeOverride)
            .Include(p => p.Flows).ThenInclude(f => f.SourcePort)
            .Include(p => p.Flows).ThenInclude(f => f.TargetPort)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // ──────────── Mapping ────────────

    private static ProcessResponseDto MapToDto(Process process) => new(
        process.Id, process.Code, process.Name, process.Description,
        process.Version, process.Status.ToString(), process.IsActive,
        process.CreatedAt, process.UpdatedAt,
        process.ProcessSteps.OrderBy(ps => ps.Sequence).Select(MapProcessStepToDto).ToList(),
        process.Flows.Select(MapFlowToDto).ToList(),
        process.ProcessRole.ToString(),
        process.ApprovalProcessId,
        process.RevisionCode,
        process.ChangeDescription,
        process.EffectiveDate
    );

    private static ProcessStepResponseDto MapProcessStepToDto(ProcessStep ps) => new(
        ps.Id, ps.ProcessId, ps.StepTemplateId,
        ps.StepTemplate.Code, ps.StepTemplate.Name,
        ps.Sequence, ps.NameOverride, ps.DescriptionOverride,
        ps.CreatedAt, ps.UpdatedAt,
        MaturityScoringService.Summarise(ps.StepTemplate),
        ps.PatternOverride,
        ps.PortOverrides.Select(po => new ProcessStepPortOverrideResponseDto(
            po.Id, po.PortId,
            po.NameOverride, po.DirectionOverride,
            po.KindIdOverride, po.KindOverride?.Name,
            po.GradeIdOverride, po.GradeOverride?.Name,
            po.QtyRuleModeOverride, po.QtyRuleNOverride,
            po.SortOrderOverride)).ToList()
    );

    private static FlowResponseDto MapFlowToDto(Flow f) => new(
        f.Id, f.ProcessId,
        f.SourceProcessStepId, f.SourcePortId, f.SourcePort.Name,
        f.TargetProcessStepId, f.TargetPortId, f.TargetPort.Name,
        f.CreatedAt, f.UpdatedAt
    );

    private static ProcessStepContentResponseDto MapContentToDto(ProcessStepContent c) => new(
        c.Id, c.ProcessStepId,
        c.ContentType.ToString(),
        c.SortOrder,
        c.Body,
        c.FileName, c.OriginalFileName, c.MimeType,
        c.ContentType == StepContentType.Image && c.FileName is not null
            ? $"uploads/process-steps/{c.FileName}"
            : null,
        c.CreatedAt,
        c.PromptType?.ToString(), c.Label, c.IsRequired, c.Units, c.MinValue, c.MaxValue, c.Choices,
        c.ContentCategory?.ToString(), c.AcknowledgmentRequired, c.NominalValue, c.IsHardLimit
    );
}
