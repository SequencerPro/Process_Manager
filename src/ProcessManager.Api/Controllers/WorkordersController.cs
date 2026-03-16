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
public class WorkordersController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public WorkordersController(ProcessManagerDbContext db) => _db = db;

    // ───── CRUD ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WorkorderResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? workflowId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Workorders.Include(w => w.Workflow).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Code.Contains(search) || w.Name.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkorderStatus>(status, true, out var s))
            query = query.Where(w => w.Status == s);

        if (workflowId.HasValue)
            query = query.Where(w => w.WorkflowId == workflowId.Value);

        var totalCount = await query.CountAsync();

        var workorders = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<WorkorderResponseDto>(
            workorders.Select(w => MapToDto(w)).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkorderResponseDto>> GetById(Guid id)
    {
        var workorder = await _db.Workorders
            .Include(w => w.Workflow)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.WorkflowProcess)
                    .ThenInclude(wp => wp.Process)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workorder is null) return NotFound();

        // Load workflow links for CanStart computation
        var workflowLinks = await _db.WorkflowLinks
            .Where(wl => wl.WorkflowId == workorder.WorkflowId)
            .ToListAsync();

        return MapToDto(workorder, includeJobs: true, workflowLinks: workflowLinks);
    }

    [HttpPost]
    public async Task<ActionResult<WorkorderResponseDto>> Create(CreateWorkorderDto dto)
    {
        if (await _db.Workorders.AnyAsync(w => w.Code == dto.Code))
            return Conflict($"A Workorder with code '{dto.Code}' already exists.");

        var workflow = await _db.Workflows
            .Include(wf => wf.WorkflowProcesses.Where(wp => !wp.IsTerminalNode))
                .ThenInclude(wp => wp.Process)
                    .ThenInclude(p => p!.ProcessSteps)
            .FirstOrDefaultAsync(wf => wf.Id == dto.WorkflowId);

        if (workflow is null)
            return BadRequest($"Workflow '{dto.WorkflowId}' not found.");

        if (!workflow.IsActive)
            return BadRequest($"Workflow '{workflow.Code}' is not active.");

        var entryPoints = workflow.WorkflowProcesses
            .Where(wp => wp.IsEntryPoint && wp.ProcessId.HasValue)
            .ToList();

        if (entryPoints.Count == 0)
            return BadRequest("Workflow has no entry points with processes assigned.");

        // Validate all entry-point processes are Released/Superseded and Active
        foreach (var ep in entryPoints)
        {
            var proc = ep.Process!;
            if (!proc.IsActive)
                return BadRequest($"Process '{proc.Code}' is not active.");
            if (proc.Status != ProcessStatus.Released && proc.Status != ProcessStatus.Superseded)
                return BadRequest($"Process '{proc.Code}' is not Released (current status: {proc.Status}).");
        }

        var workorder = new Workorder
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            WorkflowId = dto.WorkflowId,
            WorkflowVersion = workflow.Version,
            Priority = dto.Priority,
            Status = WorkorderStatus.Created
        };

        _db.Workorders.Add(workorder);

        // Create Jobs for each entry-point process
        var jobIndex = 1;
        foreach (var ep in entryPoints.OrderBy(e => e.SortOrder))
        {
            var process = ep.Process!;
            var jobCode = $"{dto.Code}-{process.Code}";

            // Ensure unique job code by appending index if needed
            if (await _db.Jobs.AnyAsync(j => j.Code == jobCode))
                jobCode = $"{dto.Code}-{jobIndex:D2}";

            var job = new Job
            {
                Code = jobCode,
                Name = $"{dto.Name} - {process.Name}",
                Description = $"Auto-created by workorder {dto.Code}",
                ProcessId = process.Id,
                ProcessVersion = process.Version,
                Priority = dto.Priority,
                Status = JobStatus.Created,
                WorkorderId = workorder.Id
            };

            _db.Jobs.Add(job);

            // Auto-create StepExecutions for each ProcessStep
            foreach (var ps in process.ProcessSteps.OrderBy(ps => ps.Sequence))
            {
                _db.StepExecutions.Add(new StepExecution
                {
                    JobId = job.Id,
                    ProcessStepId = ps.Id,
                    Sequence = ps.Sequence,
                    Status = StepExecutionStatus.Pending
                });
            }

            _db.WorkorderJobs.Add(new WorkorderJob
            {
                WorkorderId = workorder.Id,
                WorkflowProcessId = ep.Id,
                JobId = job.Id
            });

            jobIndex++;
        }

        await _db.SaveChangesAsync();

        // Reload with full nav properties
        return CreatedAtAction(nameof(GetById), new { id = workorder.Id },
            await GetByIdInternal(workorder.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkorderResponseDto>> Update(Guid id, UpdateWorkorderDto dto)
    {
        var workorder = await _db.Workorders.Include(w => w.Workflow).FirstOrDefaultAsync(w => w.Id == id);
        if (workorder is null) return NotFound();

        if (workorder.Status == WorkorderStatus.Completed || workorder.Status == WorkorderStatus.Cancelled)
            return BadRequest($"Cannot update a {workorder.Status} workorder.");

        workorder.Name = dto.Name;
        workorder.Description = dto.Description;
        workorder.Priority = dto.Priority;

        await _db.SaveChangesAsync();
        return MapToDto(workorder);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workorder = await _db.Workorders
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workorder is null) return NotFound();

        if (workorder.WorkorderJobs.Any(wj => wj.Job.Status == JobStatus.InProgress))
            return BadRequest("Cannot delete a workorder with in-progress jobs. Cancel it first.");

        // Remove associated jobs that are still Created
        var jobsToRemove = workorder.WorkorderJobs
            .Where(wj => wj.Job.Status == JobStatus.Created)
            .Select(wj => wj.Job)
            .ToList();

        _db.Jobs.RemoveRange(jobsToRemove);
        _db.Workorders.Remove(workorder);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Lifecycle Transitions ─────

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<WorkorderResponseDto>> Start(Guid id)
    {
        var workorder = await _db.Workorders
            .Include(w => w.Workflow)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.WorkflowProcess)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workorder is null) return NotFound();

        if (workorder.Status != WorkorderStatus.Created)
            return BadRequest($"Cannot start a workorder with status '{workorder.Status}'. Must be Created.");

        workorder.Status = WorkorderStatus.InProgress;
        workorder.StartedAt = DateTime.UtcNow;

        // Start all entry-point jobs
        foreach (var wj in workorder.WorkorderJobs.Where(wj => wj.WorkflowProcess.IsEntryPoint))
        {
            if (wj.Job.Status == JobStatus.Created)
            {
                wj.Job.Status = JobStatus.InProgress;
                wj.Job.StartedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return await GetByIdInternal(workorder.Id);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<WorkorderResponseDto>> Cancel(Guid id)
    {
        var workorder = await _db.Workorders
            .Include(w => w.Workflow)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workorder is null) return NotFound();

        if (workorder.Status == WorkorderStatus.Completed || workorder.Status == WorkorderStatus.Cancelled)
            return BadRequest($"Cannot cancel a {workorder.Status} workorder.");

        workorder.Status = WorkorderStatus.Cancelled;
        workorder.CompletedAt = DateTime.UtcNow;

        // Cancel all non-completed jobs
        foreach (var wj in workorder.WorkorderJobs)
        {
            if (wj.Job.Status != JobStatus.Completed && wj.Job.Status != JobStatus.Cancelled)
            {
                wj.Job.Status = JobStatus.Cancelled;
                wj.Job.CompletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return await GetByIdInternal(workorder.Id);
    }

    [HttpPost("{id:guid}/advance")]
    public async Task<ActionResult<WorkorderResponseDto>> Advance(Guid id, AdvanceWorkorderDto dto)
    {
        var workorder = await _db.Workorders
            .Include(w => w.Workflow)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workorder is null) return NotFound();

        if (workorder.Status != WorkorderStatus.InProgress)
            return BadRequest("Workorder must be InProgress to advance.");

        var link = await _db.WorkflowLinks
            .Include(l => l.TargetWorkflowProcess)
                .ThenInclude(wp => wp.Process)
                    .ThenInclude(p => p!.ProcessSteps)
            .FirstOrDefaultAsync(l => l.Id == dto.WorkflowLinkId && l.WorkflowId == workorder.WorkflowId);

        if (link is null)
            return BadRequest("Workflow link not found or does not belong to this workorder's workflow.");

        // Check that the source job is completed
        var sourceWj = workorder.WorkorderJobs
            .FirstOrDefault(wj => wj.WorkflowProcessId == link.SourceWorkflowProcessId);

        if (sourceWj is null || sourceWj.Job.Status != JobStatus.Completed)
            return BadRequest("The source job for this link has not been completed.");

        // Check if a job already exists for the target
        if (workorder.WorkorderJobs.Any(wj => wj.WorkflowProcessId == link.TargetWorkflowProcessId))
            return BadRequest("A job already exists for the target workflow process.");

        if (link.TargetWorkflowProcess.IsTerminalNode)
        {
            // Check if workorder should complete
            await CheckWorkorderCompletion(workorder);
            await _db.SaveChangesAsync();
            return await GetByIdInternal(workorder.Id);
        }

        // Create job for target process
        var targetProcess = link.TargetWorkflowProcess.Process!;
        await CreateJobForWorkflowProcess(workorder, link.TargetWorkflowProcess, targetProcess);

        await _db.SaveChangesAsync();
        return await GetByIdInternal(workorder.Id);
    }

    // ───── Auto-Progression (called from JobsController) ─────

    /// <summary>
    /// Called when a job belonging to a workorder is completed.
    /// Evaluates outgoing WorkflowLinks and creates successor jobs where all predecessors are done.
    /// </summary>
    internal async Task ProgressWorkorder(Guid workorderId, Guid completedJobId)
    {
        var workorder = await _db.Workorders
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .FirstOrDefaultAsync(w => w.Id == workorderId);

        if (workorder is null || workorder.Status != WorkorderStatus.InProgress)
            return;

        // Find which WorkflowProcess the completed job fulfills
        var completedWj = workorder.WorkorderJobs.FirstOrDefault(wj => wj.JobId == completedJobId);
        if (completedWj is null) return;

        // Get all outgoing links from this workflow process (Always routing only)
        var outgoingLinks = await _db.WorkflowLinks
            .Include(l => l.TargetWorkflowProcess)
                .ThenInclude(wp => wp.Process)
                    .ThenInclude(p => p!.ProcessSteps)
            .Where(l => l.WorkflowId == workorder.WorkflowId
                     && l.SourceWorkflowProcessId == completedWj.WorkflowProcessId
                     && l.RoutingType == RoutingType.Always)
            .ToListAsync();

        foreach (var link in outgoingLinks)
        {
            var targetWpId = link.TargetWorkflowProcessId;

            // Skip if a job already exists for this target
            if (workorder.WorkorderJobs.Any(wj => wj.WorkflowProcessId == targetWpId))
                continue;

            if (link.TargetWorkflowProcess.IsTerminalNode)
            {
                // Terminal node: check if all incoming links have completed jobs
                // (completion check happens below)
                continue;
            }

            // Check if ALL incoming links to target have their source jobs completed (merge-point logic)
            var allIncomingLinks = await _db.WorkflowLinks
                .Where(l => l.WorkflowId == workorder.WorkflowId
                         && l.TargetWorkflowProcessId == targetWpId)
                .ToListAsync();

            var allPredecessorsComplete = allIncomingLinks.All(inLink =>
            {
                var sourceWj = workorder.WorkorderJobs
                    .FirstOrDefault(wj => wj.WorkflowProcessId == inLink.SourceWorkflowProcessId);
                return sourceWj is not null && sourceWj.Job.Status == JobStatus.Completed;
            });

            if (!allPredecessorsComplete) continue;

            // All predecessors complete — create the next job
            var targetProcess = link.TargetWorkflowProcess.Process!;
            await CreateJobForWorkflowProcess(workorder, link.TargetWorkflowProcess, targetProcess);
        }

        // Check if the workorder should auto-complete
        await CheckWorkorderCompletion(workorder);

        await _db.SaveChangesAsync();
    }

    // ───── Helpers ─────

    private async Task CreateJobForWorkflowProcess(Workorder workorder, WorkflowProcess wp, Process process)
    {
        var jobCode = $"{workorder.Code}-{process.Code}";
        if (await _db.Jobs.AnyAsync(j => j.Code == jobCode))
            jobCode = $"{workorder.Code}-{process.Code}-{Guid.NewGuid().ToString()[..4]}";

        var job = new Job
        {
            Code = jobCode,
            Name = $"{workorder.Name} - {process.Name}",
            Description = $"Auto-created by workorder {workorder.Code}",
            ProcessId = process.Id,
            ProcessVersion = process.Version,
            Priority = workorder.Priority,
            Status = JobStatus.Created,
            WorkorderId = workorder.Id
        };

        _db.Jobs.Add(job);

        foreach (var ps in process.ProcessSteps.OrderBy(ps => ps.Sequence))
        {
            _db.StepExecutions.Add(new StepExecution
            {
                JobId = job.Id,
                ProcessStepId = ps.Id,
                Sequence = ps.Sequence,
                Status = StepExecutionStatus.Pending
            });
        }

        _db.WorkorderJobs.Add(new WorkorderJob
        {
            WorkorderId = workorder.Id,
            WorkflowProcessId = wp.Id,
            JobId = job.Id
        });
    }

    private async Task CheckWorkorderCompletion(Workorder workorder)
    {
        // Get all terminal nodes in the workflow
        var terminalNodes = await _db.WorkflowProcesses
            .Where(wp => wp.WorkflowId == workorder.WorkflowId && wp.IsTerminalNode)
            .ToListAsync();

        if (terminalNodes.Count == 0) return;

        // For each terminal node, check that all incoming links have completed source jobs
        var allTerminalsSatisfied = true;
        foreach (var terminal in terminalNodes)
        {
            var incomingLinks = await _db.WorkflowLinks
                .Where(l => l.WorkflowId == workorder.WorkflowId
                         && l.TargetWorkflowProcessId == terminal.Id)
                .ToListAsync();

            foreach (var link in incomingLinks)
            {
                var sourceWj = workorder.WorkorderJobs
                    .FirstOrDefault(wj => wj.WorkflowProcessId == link.SourceWorkflowProcessId);

                if (sourceWj is null || sourceWj.Job.Status != JobStatus.Completed)
                {
                    allTerminalsSatisfied = false;
                    break;
                }
            }

            if (!allTerminalsSatisfied) break;
        }

        if (allTerminalsSatisfied)
        {
            workorder.Status = WorkorderStatus.Completed;
            workorder.CompletedAt = DateTime.UtcNow;
        }
    }

    private bool ComputeCanStart(WorkorderJob wj, List<WorkflowLink> workflowLinks, Workorder workorder)
    {
        // If the job is already started/completed/cancelled, CanStart is false
        if (wj.Job.Status != JobStatus.Created) return false;

        // Entry points can always start (no incoming links)
        if (wj.WorkflowProcess.IsEntryPoint) return true;

        // Find all incoming links to this workflow process
        var incomingLinks = workflowLinks
            .Where(l => l.TargetWorkflowProcessId == wj.WorkflowProcessId)
            .ToList();

        if (incomingLinks.Count == 0) return true;

        // All predecessors must have completed jobs
        return incomingLinks.All(link =>
        {
            var sourceWj = workorder.WorkorderJobs
                .FirstOrDefault(w => w.WorkflowProcessId == link.SourceWorkflowProcessId);
            return sourceWj is not null && sourceWj.Job.Status == JobStatus.Completed;
        });
    }

    private async Task<WorkorderResponseDto> GetByIdInternal(Guid id)
    {
        var workorder = await _db.Workorders
            .Include(w => w.Workflow)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.Job)
            .Include(w => w.WorkorderJobs)
                .ThenInclude(wj => wj.WorkflowProcess)
                    .ThenInclude(wp => wp.Process)
            .FirstAsync(w => w.Id == id);

        var workflowLinks = await _db.WorkflowLinks
            .Where(wl => wl.WorkflowId == workorder.WorkflowId)
            .ToListAsync();

        return MapToDto(workorder, includeJobs: true, workflowLinks: workflowLinks);
    }

    // ───── Mappers ─────

    private WorkorderResponseDto MapToDto(Workorder w, bool includeJobs = false, List<WorkflowLink>? workflowLinks = null)
    {
        return new WorkorderResponseDto(
            w.Id,
            w.Code,
            w.Name,
            w.Description,
            w.WorkflowId,
            w.Workflow?.Name ?? "",
            w.WorkflowVersion,
            w.Status.ToString(),
            w.Priority,
            w.StartedAt,
            w.CompletedAt,
            w.CreatedAt,
            w.UpdatedAt,
            includeJobs && workflowLinks is not null
                ? w.WorkorderJobs.Select(wj => new WorkorderJobResponseDto(
                    wj.Id,
                    wj.WorkorderId,
                    wj.WorkflowProcessId,
                    wj.WorkflowProcess?.Process?.Name ?? "(Terminal)",
                    wj.WorkflowProcess?.Process?.Code ?? "",
                    wj.JobId,
                    wj.Job.Code,
                    wj.Job.Name,
                    wj.Job.Status.ToString(),
                    ComputeCanStart(wj, workflowLinks, w)
                )).ToList()
                : null);
    }
}
