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
public class WorkflowsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public WorkflowsController(ProcessManagerDbContext db) => _db = db;

    // ───────────────────── Workflow CRUD ─────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WorkflowResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Workflows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Code.Contains(search) || w.Name.Contains(search));

        if (active.HasValue)
            query = query.Where(w => w.IsActive == active.Value);

        var totalCount = await query.CountAsync();

        var workflows = await query
            .OrderBy(w => w.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<WorkflowResponseDto>(
            workflows.Select(w => MapWorkflowToDto(w)).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowResponseDto>> GetById(Guid id)
    {
        var workflow = await _db.Workflows
            .Include(w => w.WorkflowProcesses).ThenInclude(wp => wp.Process)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.SourceWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.TargetWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.Conditions).ThenInclude(c => c.Grade)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workflow is null) return NotFound();

        return MapWorkflowToDto(workflow, includeChildren: true);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowResponseDto>> Create(CreateWorkflowDto dto)
    {
        if (await _db.Workflows.AnyAsync(w => w.Code == dto.Code))
            return Conflict($"Workflow with code '{dto.Code}' already exists.");

        var workflow = new Workflow
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description
        };

        _db.Workflows.Add(workflow);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, MapWorkflowToDto(workflow));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowResponseDto>> Update(Guid id, UpdateWorkflowDto dto)
    {
        var workflow = await _db.Workflows.FindAsync(id);
        if (workflow is null) return NotFound();

        if (dto.Name is not null) workflow.Name = dto.Name;
        if (dto.Description is not null) workflow.Description = dto.Description;
        if (dto.IsActive.HasValue) workflow.IsActive = dto.IsActive.Value;
        workflow.Version++;

        await _db.SaveChangesAsync();
        return MapWorkflowToDto(workflow);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workflow = await _db.Workflows.FindAsync(id);
        if (workflow is null) return NotFound();

        _db.Workflows.Remove(workflow);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───────────────────── Workflow Validate ─────────────────────

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<WorkflowValidationResultDto>> Validate(Guid id)
    {
        var workflow = await _db.Workflows
            .Include(w => w.WorkflowProcesses).ThenInclude(wp => wp.Process)
                .ThenInclude(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate).ThenInclude(st => st.Ports)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.Conditions)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.SourceWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(w => w.WorkflowLinks).ThenInclude(wl => wl.TargetWorkflowProcess).ThenInclude(wp => wp.Process)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workflow is null) return NotFound();

        var errors = new List<string>();
        var warnings = new List<string>();

        // Rule 12: At least one entry point
        if (!workflow.WorkflowProcesses.Any(wp => wp.IsEntryPoint))
            errors.Add("Workflow must have at least one entry point.");

        // Rule 14: GradeBased links require conditions
        foreach (var link in workflow.WorkflowLinks.Where(l => l.RoutingType == RoutingType.GradeBased))
        {
            if (!link.Conditions.Any())
            {
                var srcName = link.SourceWorkflowProcess?.Process?.Name ?? link.SourceWorkflowProcessId.ToString();
                var tgtName = link.TargetWorkflowProcess?.Process?.Name ?? link.TargetWorkflowProcessId.ToString();
                errors.Add($"GradeBased link '{srcName}' → '{tgtName}' has no conditions.");
            }
        }

        // Rule 15: Process interface compatibility (warning)
        foreach (var link in workflow.WorkflowLinks)
        {
            var srcProcess = link.SourceWorkflowProcess?.Process;
            var tgtProcess = link.TargetWorkflowProcess?.Process;
            if (srcProcess?.ProcessSteps == null || tgtProcess?.ProcessSteps == null)
                continue;

            var lastStep = srcProcess.ProcessSteps.OrderByDescending(ps => ps.Sequence).FirstOrDefault();
            var firstStep = tgtProcess.ProcessSteps.OrderBy(ps => ps.Sequence).FirstOrDefault();
            if (lastStep?.StepTemplate?.Ports == null || firstStep?.StepTemplate?.Ports == null)
                continue;

            var outputKinds = lastStep.StepTemplate.Ports
                .Where(p => p.Direction == PortDirection.Output)
                .Select(p => p.KindId)
                .ToHashSet();

            var inputKinds = firstStep.StepTemplate.Ports
                .Where(p => p.Direction == PortDirection.Input)
                .Select(p => p.KindId)
                .ToHashSet();

            if (!outputKinds.Intersect(inputKinds).Any())
            {
                warnings.Add($"Link '{srcProcess.Name}' → '{tgtProcess.Name}': " +
                    "source output Kinds do not match target input Kinds.");
            }
        }

        // Check for unreachable nodes (no incoming links and not entry points)
        var hasIncoming = workflow.WorkflowLinks
            .Select(l => l.TargetWorkflowProcessId)
            .ToHashSet();
        foreach (var wp in workflow.WorkflowProcesses)
        {
            if (!wp.IsEntryPoint && !hasIncoming.Contains(wp.Id))
            {
                warnings.Add($"Process '{wp.Process?.Name ?? wp.ProcessId.ToString()}' " +
                    "is not an entry point and has no incoming links (unreachable).");
            }
        }

        return new WorkflowValidationResultDto(
            !errors.Any(),
            errors,
            warnings);
    }

    // ───────────────────── WorkflowProcess sub-resources ─────────────────────

    [HttpGet("{workflowId:guid}/processes")]
    public async Task<ActionResult<List<WorkflowProcessResponseDto>>> GetProcesses(Guid workflowId)
    {
        if (!await _db.Workflows.AnyAsync(w => w.Id == workflowId))
            return NotFound();

        var wps = await _db.WorkflowProcesses
            .Include(wp => wp.Process)
            .Where(wp => wp.WorkflowId == workflowId)
            .OrderBy(wp => wp.SortOrder)
            .ToListAsync();

        return wps.Select(MapWorkflowProcessToDto).ToList();
    }

    [HttpPost("{workflowId:guid}/processes")]
    public async Task<ActionResult<WorkflowProcessResponseDto>> AddProcess(
        Guid workflowId, AddWorkflowProcessDto dto)
    {
        var workflow = await _db.Workflows.FindAsync(workflowId);
        if (workflow is null) return NotFound();

        var process = await _db.Processes.FindAsync(dto.ProcessId);
        if (process is null)
            return BadRequest($"Process '{dto.ProcessId}' not found.");
        if (!process.IsActive)
            return BadRequest($"Process '{process.Code}' is not active.");

        var wp = new WorkflowProcess
        {
            WorkflowId = workflowId,
            ProcessId = dto.ProcessId,
            IsEntryPoint = dto.IsEntryPoint,
            SortOrder = dto.SortOrder,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Color = dto.Color
        };

        _db.WorkflowProcesses.Add(wp);
        await _db.SaveChangesAsync();

        wp.Process = process;
        return CreatedAtAction(nameof(GetById), new { id = workflowId }, MapWorkflowProcessToDto(wp));
    }

    [HttpPut("{workflowId:guid}/processes/{wpId:guid}")]
    public async Task<ActionResult<WorkflowProcessResponseDto>> UpdateProcess(
        Guid workflowId, Guid wpId, UpdateWorkflowProcessDto dto)
    {
        var wp = await _db.WorkflowProcesses
            .Include(wp => wp.Process)
            .FirstOrDefaultAsync(wp => wp.Id == wpId && wp.WorkflowId == workflowId);
        if (wp is null) return NotFound();

        if (dto.IsEntryPoint.HasValue) wp.IsEntryPoint = dto.IsEntryPoint.Value;
        if (dto.SortOrder.HasValue) wp.SortOrder = dto.SortOrder.Value;
        if (dto.PositionX.HasValue) wp.PositionX = dto.PositionX.Value;
        if (dto.PositionY.HasValue) wp.PositionY = dto.PositionY.Value;
        if (dto.Color is not null) wp.Color = string.IsNullOrEmpty(dto.Color) ? null : dto.Color;

        await _db.SaveChangesAsync();
        return MapWorkflowProcessToDto(wp);
    }

    [HttpDelete("{workflowId:guid}/processes/{wpId:guid}")]
    public async Task<IActionResult> RemoveProcess(Guid workflowId, Guid wpId)
    {
        var wp = await _db.WorkflowProcesses
            .FirstOrDefaultAsync(wp => wp.Id == wpId && wp.WorkflowId == workflowId);
        if (wp is null) return NotFound();

        // Check for links referencing this workflow process
        var hasLinks = await _db.WorkflowLinks.AnyAsync(
            wl => wl.SourceWorkflowProcessId == wpId || wl.TargetWorkflowProcessId == wpId);
        if (hasLinks)
            return Conflict("Cannot remove a process that has workflow links. Remove the links first.");

        _db.WorkflowProcesses.Remove(wp);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{workflowId:guid}/processes/positions")]
    public async Task<IActionResult> UpdatePositions(
        Guid workflowId, UpdateWorkflowProcessPositionsDto dto)
    {
        if (!await _db.Workflows.AnyAsync(w => w.Id == workflowId))
            return NotFound();

        var wps = await _db.WorkflowProcesses
            .Where(wp => wp.WorkflowId == workflowId)
            .ToListAsync();

        foreach (var pos in dto.Positions)
        {
            var wp = wps.FirstOrDefault(w => w.Id == pos.WorkflowProcessId);
            if (wp is not null)
            {
                wp.PositionX = pos.PositionX;
                wp.PositionY = pos.PositionY;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───────────────────── WorkflowLink sub-resources ─────────────────────

    [HttpGet("{workflowId:guid}/links")]
    public async Task<ActionResult<List<WorkflowLinkResponseDto>>> GetLinks(Guid workflowId)
    {
        if (!await _db.Workflows.AnyAsync(w => w.Id == workflowId))
            return NotFound();

        var links = await _db.WorkflowLinks
            .Include(wl => wl.SourceWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.TargetWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.Conditions).ThenInclude(c => c.Grade)
            .Where(wl => wl.WorkflowId == workflowId)
            .OrderBy(wl => wl.SortOrder)
            .ToListAsync();

        return links.Select(MapWorkflowLinkToDto).ToList();
    }

    [HttpPost("{workflowId:guid}/links")]
    public async Task<ActionResult<WorkflowLinkResponseDto>> CreateLink(
        Guid workflowId, CreateWorkflowLinkDto dto)
    {
        var workflow = await _db.Workflows.FindAsync(workflowId);
        if (workflow is null) return NotFound();

        // Validate source and target exist and belong to this workflow
        var source = await _db.WorkflowProcesses
            .Include(wp => wp.Process)
            .FirstOrDefaultAsync(wp => wp.Id == dto.SourceWorkflowProcessId && wp.WorkflowId == workflowId);
        if (source is null)
            return BadRequest("Source workflow process not found in this workflow.");

        var target = await _db.WorkflowProcesses
            .Include(wp => wp.Process)
            .FirstOrDefaultAsync(wp => wp.Id == dto.TargetWorkflowProcessId && wp.WorkflowId == workflowId);
        if (target is null)
            return BadRequest("Target workflow process not found in this workflow.");

        // No self-loops
        if (dto.SourceWorkflowProcessId == dto.TargetWorkflowProcessId)
            return BadRequest("Source and target must be different workflow processes.");

        // No duplicate links
        if (await _db.WorkflowLinks.AnyAsync(wl =>
            wl.SourceWorkflowProcessId == dto.SourceWorkflowProcessId &&
            wl.TargetWorkflowProcessId == dto.TargetWorkflowProcessId))
            return Conflict("A link between these two workflow processes already exists.");

        // Validate conditions for GradeBased
        if (dto.RoutingType == RoutingType.GradeBased &&
            (dto.ConditionGradeIds == null || !dto.ConditionGradeIds.Any()))
            return BadRequest("GradeBased links must have at least one condition grade.");

        var link = new WorkflowLink
        {
            WorkflowId = workflowId,
            SourceWorkflowProcessId = dto.SourceWorkflowProcessId,
            TargetWorkflowProcessId = dto.TargetWorkflowProcessId,
            RoutingType = dto.RoutingType,
            Name = dto.Name,
            SortOrder = dto.SortOrder,
            LineShape = dto.LineShape
        };

        _db.WorkflowLinks.Add(link);

        // Add conditions if grade-based
        if (dto.ConditionGradeIds != null)
        {
            foreach (var gradeId in dto.ConditionGradeIds)
            {
                var grade = await _db.Grades.FindAsync(gradeId);
                if (grade is null)
                    return BadRequest($"Grade '{gradeId}' not found.");

                _db.WorkflowLinkConditions.Add(new WorkflowLinkCondition
                {
                    WorkflowLinkId = link.Id,
                    GradeId = gradeId
                });
            }
        }

        await _db.SaveChangesAsync();

        // Reload with navigation properties for response
        var created = await _db.WorkflowLinks
            .Include(wl => wl.SourceWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.TargetWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.Conditions).ThenInclude(c => c.Grade)
            .FirstAsync(wl => wl.Id == link.Id);

        return CreatedAtAction(nameof(GetById), new { id = workflowId }, MapWorkflowLinkToDto(created));
    }

    [HttpPut("{workflowId:guid}/links/{linkId:guid}")]
    public async Task<ActionResult<WorkflowLinkResponseDto>> UpdateLink(
        Guid workflowId, Guid linkId, UpdateWorkflowLinkDto dto)
    {
        var link = await _db.WorkflowLinks
            .Include(wl => wl.SourceWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.TargetWorkflowProcess).ThenInclude(wp => wp.Process)
            .Include(wl => wl.Conditions).ThenInclude(c => c.Grade)
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorkflowId == workflowId);
        if (link is null) return NotFound();

        if (dto.Name is not null) link.Name = dto.Name;
        if (dto.SortOrder.HasValue) link.SortOrder = dto.SortOrder.Value;
        if (dto.LineShape is not null) link.LineShape = string.IsNullOrEmpty(dto.LineShape) ? null : dto.LineShape;

        await _db.SaveChangesAsync();
        return MapWorkflowLinkToDto(link);
    }

    [HttpDelete("{workflowId:guid}/links/{linkId:guid}")]
    public async Task<IActionResult> DeleteLink(Guid workflowId, Guid linkId)
    {
        var link = await _db.WorkflowLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorkflowId == workflowId);
        if (link is null) return NotFound();

        _db.WorkflowLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───────────────────── Link Conditions sub-resources ─────────────────────

    [HttpPost("{workflowId:guid}/links/{linkId:guid}/conditions")]
    public async Task<ActionResult<WorkflowLinkConditionResponseDto>> AddCondition(
        Guid workflowId, Guid linkId, AddWorkflowLinkConditionDto dto)
    {
        var link = await _db.WorkflowLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorkflowId == workflowId);
        if (link is null) return NotFound();

        if (link.RoutingType != RoutingType.GradeBased)
            return BadRequest("Conditions can only be added to GradeBased links.");

        var grade = await _db.Grades.FindAsync(dto.GradeId);
        if (grade is null) return BadRequest($"Grade '{dto.GradeId}' not found.");

        if (await _db.WorkflowLinkConditions.AnyAsync(
            c => c.WorkflowLinkId == linkId && c.GradeId == dto.GradeId))
            return Conflict("This grade condition already exists on this link.");

        var condition = new WorkflowLinkCondition
        {
            WorkflowLinkId = linkId,
            GradeId = dto.GradeId
        };

        _db.WorkflowLinkConditions.Add(condition);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workflowId },
            new WorkflowLinkConditionResponseDto(
                condition.Id, condition.WorkflowLinkId,
                grade.Id, grade.Code, grade.Name));
    }

    [HttpDelete("{workflowId:guid}/links/{linkId:guid}/conditions/{conditionId:guid}")]
    public async Task<IActionResult> RemoveCondition(
        Guid workflowId, Guid linkId, Guid conditionId)
    {
        var condition = await _db.WorkflowLinkConditions
            .Include(c => c.WorkflowLink)
            .FirstOrDefaultAsync(c => c.Id == conditionId && c.WorkflowLinkId == linkId);

        if (condition is null) return NotFound();
        if (condition.WorkflowLink.WorkflowId != workflowId) return NotFound();

        _db.WorkflowLinkConditions.Remove(condition);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───────────────────── Mappers ─────────────────────

    private static WorkflowResponseDto MapWorkflowToDto(Workflow w, bool includeChildren = false)
    {
        return new WorkflowResponseDto(
            w.Id, w.Code, w.Name, w.Description,
            w.Version, w.IsActive,
            w.CreatedAt, w.UpdatedAt,
            includeChildren ? w.WorkflowProcesses
                .OrderBy(wp => wp.SortOrder)
                .Select(MapWorkflowProcessToDto).ToList() : null,
            includeChildren ? w.WorkflowLinks
                .OrderBy(wl => wl.SortOrder)
                .Select(MapWorkflowLinkToDto).ToList() : null);
    }

    private static WorkflowProcessResponseDto MapWorkflowProcessToDto(WorkflowProcess wp)
    {
        return new WorkflowProcessResponseDto(
            wp.Id, wp.WorkflowId, wp.ProcessId,
            wp.Process?.Name ?? "", wp.Process?.Code ?? "",
            wp.IsEntryPoint, wp.SortOrder,
            wp.PositionX, wp.PositionY, wp.Color,
            wp.CreatedAt, wp.UpdatedAt);
    }

    private static WorkflowLinkResponseDto MapWorkflowLinkToDto(WorkflowLink wl)
    {
        return new WorkflowLinkResponseDto(
            wl.Id, wl.WorkflowId,
            wl.SourceWorkflowProcessId,
            wl.SourceWorkflowProcess?.Process?.Name ?? "",
            wl.TargetWorkflowProcessId,
            wl.TargetWorkflowProcess?.Process?.Name ?? "",
            wl.RoutingType, wl.Name, wl.SortOrder,
            wl.LineShape,
            wl.Conditions?.Select(c => new WorkflowLinkConditionResponseDto(
                c.Id, c.WorkflowLinkId, c.GradeId,
                c.Grade?.Code ?? "", c.Grade?.Name ?? "")).ToList(),
            wl.CreatedAt, wl.UpdatedAt);
    }
}
