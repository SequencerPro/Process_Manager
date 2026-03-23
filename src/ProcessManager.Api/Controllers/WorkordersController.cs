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

        // Pre-populate WorkorderJob rows for ALL non-terminal nodes with Pending status
        var allNonTerminalNodes = workflow.WorkflowProcesses
            .Where(wp => !wp.IsTerminalNode && wp.ProcessId.HasValue)
            .ToList();

        foreach (var wp in allNonTerminalNodes)
        {
            _db.WorkorderJobs.Add(new WorkorderJob
            {
                WorkorderId = workorder.Id,
                WorkflowProcessId = wp.Id,
                JobId = null,
                NodeStatus = WorkflowNodeStatus.Pending
            });
        }

        // Create Jobs for each entry-point process and activate those WorkorderJob rows
        foreach (var ep in entryPoints.OrderBy(e => e.SortOrder))
        {
            var process = ep.Process!;
            var jobCode = $"{dto.Code}-{process.Code}";

            // Ensure unique job code by checking both DB and change tracker (for multi-entry-point workflows)
            var suffix = 1;
            while (await _db.Jobs.AnyAsync(j => j.Code == jobCode)
                   || _db.Jobs.Local.Any(j => j.Code == jobCode))
            {
                jobCode = $"{dto.Code}-{process.Code}-{suffix:D2}";
                suffix++;
            }

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

            // Activate the pre-populated WorkorderJob row for this entry point
            var existingWj = _db.WorkorderJobs.Local
                .FirstOrDefault(wj => wj.WorkorderId == workorder.Id && wj.WorkflowProcessId == ep.Id);

            if (existingWj is not null)
            {
                existingWj.JobId = job.Id;
                existingWj.NodeStatus = WorkflowNodeStatus.Active;
            }
            else
            {
                _db.WorkorderJobs.Add(new WorkorderJob
                {
                    WorkorderId = workorder.Id,
                    WorkflowProcessId = ep.Id,
                    JobId = job.Id,
                    NodeStatus = WorkflowNodeStatus.Active
                });
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            return Conflict($"Failed to create workorder: {ex.InnerException?.Message ?? ex.Message}");
        }

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

        if (workorder.WorkorderJobs.Any(wj => wj.Job != null && wj.Job.Status == JobStatus.InProgress))
            return BadRequest("Cannot delete a workorder with in-progress jobs. Cancel it first.");

        // Remove associated jobs that are still Created
        var jobsToRemove = workorder.WorkorderJobs
            .Where(wj => wj.Job != null && wj.Job.Status == JobStatus.Created)
            .Select(wj => wj.Job!)
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

        // Start all Active (entry-point) jobs
        foreach (var wj in workorder.WorkorderJobs.Where(wj => wj.NodeStatus == WorkflowNodeStatus.Active && wj.Job != null))
        {
            if (wj.Job!.Status == JobStatus.Created)
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

        // Cancel all non-completed jobs and mark Pending nodes as Skipped
        foreach (var wj in workorder.WorkorderJobs)
        {
            if (wj.NodeStatus == WorkflowNodeStatus.Pending)
            {
                wj.NodeStatus = WorkflowNodeStatus.Skipped;
            }
            else if (wj.Job != null && wj.Job.Status != JobStatus.Completed && wj.Job.Status != JobStatus.Cancelled)
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

        if (sourceWj is null || sourceWj.Job?.Status != JobStatus.Completed)
            return BadRequest("The source job for this link has not been completed.");

        // Check if a job already exists for the target (Active or Complete)
        if (workorder.WorkorderJobs.Any(wj => wj.WorkflowProcessId == link.TargetWorkflowProcessId
                && wj.NodeStatus != WorkflowNodeStatus.Pending))
            return BadRequest("A job already exists for the target workflow process.");

        if (link.TargetWorkflowProcess.IsTerminalNode)
        {
            // Check if workorder should complete
            await CheckWorkorderCompletion(workorder);
            await _db.SaveChangesAsync();
            return await GetByIdInternal(workorder.Id);
        }

        // Activate job for target process
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

        // Mark the completed node as Complete
        completedWj.NodeStatus = WorkflowNodeStatus.Complete;

        // Get all outgoing links that can auto-fire: Always and GradeBased (Manual links are never auto-fired)
        var outgoingLinks = await _db.WorkflowLinks
            .Include(l => l.TargetWorkflowProcess)
                .ThenInclude(wp => wp.Process)
                    .ThenInclude(p => p!.ProcessSteps)
            .Include(l => l.Conditions)
            .Where(l => l.WorkflowId == workorder.WorkflowId
                     && l.SourceWorkflowProcessId == completedWj.WorkflowProcessId
                     && (l.RoutingType == RoutingType.Always || l.RoutingType == RoutingType.GradeBased))
            .ToListAsync();

        // Load the completed job's item grades once (only if GradeBased links exist)
        HashSet<Guid>? completedJobGradeIds = null;
        if (outgoingLinks.Any(l => l.RoutingType == RoutingType.GradeBased))
        {
            var gradeIds = await _db.Items
                .Where(i => i.JobId == completedJobId)
                .Select(i => i.GradeId)
                .Distinct()
                .ToListAsync();
            completedJobGradeIds = gradeIds.ToHashSet();
        }

        foreach (var link in outgoingLinks)
        {
            // GradeBased: fire only if any item grade matches a condition grade
            if (link.RoutingType == RoutingType.GradeBased)
            {
                var conditionGradeIds = link.Conditions.Select(c => c.GradeId).ToHashSet();
                if (completedJobGradeIds is null || !completedJobGradeIds.Overlaps(conditionGradeIds))
                    continue;
            }

            var targetWpId = link.TargetWorkflowProcessId;

            // Skip if a job already exists (Active or Complete) for this target
            if (workorder.WorkorderJobs.Any(wj => wj.WorkflowProcessId == targetWpId
                    && wj.NodeStatus != WorkflowNodeStatus.Pending))
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
                return sourceWj is not null && sourceWj.Job?.Status == JobStatus.Completed;
            });

            if (!allPredecessorsComplete) continue;

            // All predecessors complete — activate the next job
            var targetProcess = link.TargetWorkflowProcess.Process!;
            await CreateJobForWorkflowProcess(workorder, link.TargetWorkflowProcess, targetProcess);
        }

        // Mark definitively-skipped nodes: Pending nodes whose ALL predecessor source WJs are Complete
        await MarkSkippedNodes(workorder);

        // Check if the workorder should auto-complete
        await CheckWorkorderCompletion(workorder);

        await _db.SaveChangesAsync();
    }

    // ───── Helpers ─────

    private async Task CreateJobForWorkflowProcess(Workorder workorder, WorkflowProcess wp, Process process)
    {
        var jobCode = $"{workorder.Code}-{process.Code}";
        var suffix = 1;
        while (await _db.Jobs.AnyAsync(j => j.Code == jobCode)
               || _db.Jobs.Local.Any(j => j.Code == jobCode))
        {
            jobCode = $"{workorder.Code}-{process.Code}-{suffix:D2}";
            suffix++;
        }

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

        // Find the existing Pending WorkorderJob row for this node and activate it
        var existingWj = workorder.WorkorderJobs
            .FirstOrDefault(wj => wj.WorkflowProcessId == wp.Id
                               && wj.NodeStatus == WorkflowNodeStatus.Pending);

        if (existingWj is not null)
        {
            existingWj.JobId = job.Id;
            existingWj.NodeStatus = WorkflowNodeStatus.Active;
        }
        else
        {
            // Edge case: no pre-populated row exists, create one
            _db.WorkorderJobs.Add(new WorkorderJob
            {
                WorkorderId = workorder.Id,
                WorkflowProcessId = wp.Id,
                JobId = job.Id,
                NodeStatus = WorkflowNodeStatus.Active
            });
        }
    }

    private async Task MarkSkippedNodes(Workorder workorder)
    {
        // Load all workflow links for this workflow
        var allLinks = await _db.WorkflowLinks
            .Where(l => l.WorkflowId == workorder.WorkflowId)
            .ToListAsync();

        // For each Pending node, check if ALL predecessors are Complete/Skipped
        // and no remaining path (Manual link) could still activate it
        foreach (var pendingWj in workorder.WorkorderJobs
            .Where(wj => wj.NodeStatus == WorkflowNodeStatus.Pending).ToList())
        {
            var incomingLinks = allLinks
                .Where(l => l.TargetWorkflowProcessId == pendingWj.WorkflowProcessId)
                .ToList();

            if (incomingLinks.Count == 0) continue;

            // If any incoming link is Manual, the user might still manually advance to this node
            // — do not mark it Skipped automatically
            if (incomingLinks.Any(l => l.RoutingType == RoutingType.Manual)) continue;

            // All predecessors must have Complete or Skipped status
            var allPredecessorsResolved = incomingLinks.All(link =>
            {
                var predWj = workorder.WorkorderJobs
                    .FirstOrDefault(wj => wj.WorkflowProcessId == link.SourceWorkflowProcessId);
                return predWj is not null
                    && (predWj.NodeStatus == WorkflowNodeStatus.Complete
                        || predWj.NodeStatus == WorkflowNodeStatus.Skipped);
            });

            if (allPredecessorsResolved)
            {
                // All predecessors are done and no manual path remains — mark as Skipped
                pendingWj.NodeStatus = WorkflowNodeStatus.Skipped;
            }
        }
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

                if (sourceWj is null || sourceWj.Job?.Status != JobStatus.Completed)
                {
                    allTerminalsSatisfied = false;
                    break;
                }
            }

            if (!allTerminalsSatisfied) break;
        }

        if (allTerminalsSatisfied)
        {
            // Mark any remaining Pending nodes as Skipped
            foreach (var wj in workorder.WorkorderJobs.Where(wj => wj.NodeStatus == WorkflowNodeStatus.Pending))
            {
                wj.NodeStatus = WorkflowNodeStatus.Skipped;
            }

            workorder.Status = WorkorderStatus.Completed;
            workorder.CompletedAt = DateTime.UtcNow;
        }
    }

    private bool ComputeCanStart(WorkorderJob wj, List<WorkflowLink> workflowLinks, Workorder workorder)
    {
        // Only Active nodes with a job in Created status can start
        if (wj.NodeStatus != WorkflowNodeStatus.Active) return false;
        if (wj.Job == null) return false;
        if (wj.Job.Status != JobStatus.Created) return false;

        return true;
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
                ? w.WorkorderJobs.Select(wj => MapWorkorderJobToDto(wj, workflowLinks, w)).ToList()
                : null);
    }

    private WorkorderJobResponseDto MapWorkorderJobToDto(WorkorderJob wj, List<WorkflowLink> workflowLinks, Workorder workorder)
    {
        return new WorkorderJobResponseDto(
            wj.Id,
            wj.WorkorderId,
            wj.WorkflowProcessId,
            wj.WorkflowProcess?.Process?.Name ?? "(Terminal)",
            wj.WorkflowProcess?.Process?.Code ?? "",
            wj.JobId,
            wj.Job?.Code,
            wj.Job?.Name,
            wj.Job?.Status.ToString(),
            ComputeCanStart(wj, workflowLinks, workorder),
            wj.NodeStatus.ToString(),
            wj.JobId.HasValue);
    }
}
