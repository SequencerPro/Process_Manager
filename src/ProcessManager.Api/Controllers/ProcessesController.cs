using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;
using Process = ProcessManager.Domain.Entities.Process;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Processes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || p.Name.Contains(search));

        if (active.HasValue)
            query = query.Where(p => p.IsActive == active.Value);

        var totalCount = await query.CountAsync();

        var processes = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProcessSummaryResponseDto(
                p.Id, p.Code, p.Name, p.Description,
                p.Version, p.IsActive,
                p.ProcessSteps.Count,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

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

        var process = new Process
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description
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
            DescriptionOverride = dto.DescriptionOverride
        };

        _db.ProcessSteps.Add(processStep);
        process.Version++;
        await _db.SaveChangesAsync();

        // Reload with nav props
        var result = await _db.ProcessSteps
            .Include(ps => ps.StepTemplate)
            .FirstAsync(ps => ps.Id == processStep.Id);

        return CreatedAtAction(nameof(GetById), new { id = processId }, MapProcessStepToDto(result));
    }

    [HttpPut("{processId:guid}/steps/{stepId:guid}")]
    public async Task<ActionResult<ProcessStepResponseDto>> UpdateStep(
        Guid processId, Guid stepId, ProcessStepUpdateDto dto)
    {
        var processStep = await _db.ProcessSteps
            .Include(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(ps => ps.Id == stepId && ps.ProcessId == processId);

        if (processStep is null) return NotFound();

        processStep.Sequence = dto.Sequence;
        processStep.NameOverride = dto.NameOverride;
        processStep.DescriptionOverride = dto.DescriptionOverride;

        var process = await _db.Processes.FindAsync(processId);
        if (process is not null) process.Version++;

        await _db.SaveChangesAsync();
        return MapProcessStepToDto(processStep);
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

        // Validate type compatibility (Kind + Grade must match)
        if (sourcePort.KindId != targetPort.KindId || sourcePort.GradeId != targetPort.GradeId)
            return BadRequest(
                $"Type mismatch: source port flows {sourcePort.Kind.Code}/{sourcePort.Grade.Code} " +
                $"but target port expects {targetPort.Kind.Code}/{targetPort.Grade.Code}.");

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

    // ──────────── Helpers ────────────

    private async Task<Process?> LoadProcess(Guid id)
    {
        return await _db.Processes
            .Include(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate)
            .Include(p => p.Flows).ThenInclude(f => f.SourcePort)
            .Include(p => p.Flows).ThenInclude(f => f.TargetPort)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // ──────────── Mapping ────────────

    private static ProcessResponseDto MapToDto(Process process) => new(
        process.Id, process.Code, process.Name, process.Description,
        process.Version, process.IsActive,
        process.CreatedAt, process.UpdatedAt,
        process.ProcessSteps.OrderBy(ps => ps.Sequence).Select(MapProcessStepToDto).ToList(),
        process.Flows.Select(MapFlowToDto).ToList()
    );

    private static ProcessStepResponseDto MapProcessStepToDto(ProcessStep ps) => new(
        ps.Id, ps.ProcessId, ps.StepTemplateId,
        ps.StepTemplate.Code, ps.StepTemplate.Name,
        ps.Sequence, ps.NameOverride, ps.DescriptionOverride,
        ps.CreatedAt, ps.UpdatedAt
    );

    private static FlowResponseDto MapFlowToDto(Flow f) => new(
        f.Id, f.ProcessId,
        f.SourceProcessStepId, f.SourcePortId, f.SourcePort.Name,
        f.TargetProcessStepId, f.TargetPortId, f.TargetPort.Name,
        f.CreatedAt, f.UpdatedAt
    );
}
