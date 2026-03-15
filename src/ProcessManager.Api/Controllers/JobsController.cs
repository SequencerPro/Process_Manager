using System.Security.Claims;
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
public class JobsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public JobsController(ProcessManagerDbContext db) => _db = db;

    // ───── Job CRUD ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<JobResponseDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? processId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Jobs.Include(j => j.Process).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(j => j.Code.Contains(search) || j.Name.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<JobStatus>(status, true, out var s))
            query = query.Where(j => j.Status == s);

        if (processId.HasValue)
            query = query.Where(j => j.ProcessId == processId.Value);

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<JobResponseDto>(
            jobs.Select(j => MapJobToDto(j)).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponseDto>> GetById(Guid id)
    {
        var job = await _db.Jobs
            .Include(j => j.Process)
            .Include(j => j.StepExecutions.OrderBy(se => se.Sequence))
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();
        return MapJobToDto(job, includeStepExecutions: true);
    }

    [HttpPost]
    public async Task<ActionResult<JobResponseDto>> Create(CreateJobDto dto)
    {
        if (await _db.Jobs.AnyAsync(j => j.Code == dto.Code))
            return Conflict($"A Job with code '{dto.Code}' already exists.");

        var process = await _db.Processes
            .Include(p => p.ProcessSteps)
            .FirstOrDefaultAsync(p => p.Id == dto.ProcessId);

        if (process is null)
            return BadRequest($"Process '{dto.ProcessId}' not found.");

        if (!process.IsActive)
            return BadRequest($"Process '{process.Code}' is not active.");

        if (process.Status != ProcessManager.Domain.Enums.ProcessStatus.Released &&
            process.Status != ProcessManager.Domain.Enums.ProcessStatus.Superseded)
            return BadRequest($"Process '{process.Code}' is not Released (current status: {process.Status}). Only Released or Superseded processes can be used for new Jobs.");

        // ── Training prerequisite enforcement (Phase 16) ──────────────────────
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId is not null)
        {
            var enforcedReqs = await _db.ProcessTrainingRequirements
                .Include(r => r.RequiredTrainingProcess)
                .Where(r => r.SubjectType == TrainingRequirementSubjectType.Process
                         && r.SubjectEntityId == dto.ProcessId
                         && r.IsEnforced)
                .ToListAsync();

            var missing = new List<string>();
            foreach (var req in enforcedReqs)
            {
                var hasCompetency = await _db.CompetencyRecords.AnyAsync(c =>
                    c.UserId == currentUserId
                    && c.TrainingProcessId == req.RequiredTrainingProcessId
                    && c.Status == CompetencyStatus.Current);

                if (!hasCompetency)
                    missing.Add(req.RequiredTrainingProcess?.CompetencyTitle
                             ?? req.RequiredTrainingProcess?.Name
                             ?? req.RequiredTrainingProcessId.ToString());
            }

            if (missing.Count > 0)
                return BadRequest(
                    $"You must hold current competency in: {string.Join(", ", missing)}.");
        }

        var job = new Job
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ProcessId = dto.ProcessId,
            Priority = dto.Priority,
            ProcessVersion = process.Version,
            Status = JobStatus.Created
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

        await _db.SaveChangesAsync();

        var result = await _db.Jobs
            .Include(j => j.Process)
            .Include(j => j.StepExecutions.OrderBy(se => se.Sequence))
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .FirstAsync(j => j.Id == job.Id);

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, MapJobToDto(result, includeStepExecutions: true));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponseDto>> Update(Guid id, UpdateJobDto dto)
    {
        var job = await _db.Jobs.Include(j => j.Process).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
            return BadRequest($"Cannot update a {job.Status} job.");

        job.Name = dto.Name;
        job.Description = dto.Description;
        job.Priority = dto.Priority;

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status == JobStatus.InProgress)
            return BadRequest("Cannot delete an in-progress job. Cancel it first.");

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Job Lifecycle Transitions ─────

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<JobResponseDto>> Start(Guid id)
    {
        var job = await _db.Jobs.Include(j => j.Process).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status != JobStatus.Created && job.Status != JobStatus.OnHold)
            return BadRequest($"Cannot start a job with status '{job.Status}'. Must be Created or OnHold.");

        job.Status = JobStatus.InProgress;
        job.StartedAt ??= DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<JobResponseDto>> Complete(Guid id)
    {
        var job = await _db.Jobs
            .Include(j => j.Process)
            .Include(j => j.StepExecutions)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        if (job.Status != JobStatus.InProgress)
            return BadRequest($"Cannot complete a job with status '{job.Status}'. Must be InProgress.");

        var incomplete = job.StepExecutions.Where(se =>
            se.Status != StepExecutionStatus.Completed &&
            se.Status != StepExecutionStatus.Skipped).ToList();

        if (incomplete.Any())
            return BadRequest($"Cannot complete job — {incomplete.Count} step(s) are not finished: " +
                string.Join(", ", incomplete.Select(se => $"Step {se.Sequence} ({se.Status})")));

        job.Status = JobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;

        // ── Auto-create CompetencyRecord for Training-role jobs (Phase 16) ─────
        if (job.Process.ProcessRole == ProcessRole.Training)
        {
            var traineeId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var traineeName = User.FindFirstValue("display_name")
                           ?? User.Identity?.Name
                           ?? "Unknown";

            if (traineeId is not null)
            {
                // Supersede any existing Current record for this trainee + training process
                var existing = await _db.CompetencyRecords
                    .Where(c => c.UserId == traineeId
                             && c.TrainingProcessId == job.ProcessId
                             && c.Status == CompetencyStatus.Current)
                    .ToListAsync();

                foreach (var old in existing)
                    old.Status = CompetencyStatus.Superseded;

                var now = DateTime.UtcNow;
                var expiresAt = job.Process.CompetencyExpiryDays.HasValue
                    ? now.AddDays(job.Process.CompetencyExpiryDays.Value)
                    : (DateTime?)null;

                _db.CompetencyRecords.Add(new CompetencyRecord
                {
                    UserId                 = traineeId,
                    UserDisplayName        = traineeName,
                    TrainingProcessId      = job.ProcessId,
                    TrainingProcessVersion = job.ProcessVersion,
                    JobId                  = job.Id,
                    CompletedAt            = now,
                    ExpiresAt              = expiresAt,
                    Status                 = CompetencyStatus.Current
                });
            }
        }

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<JobResponseDto>> Cancel(Guid id)
    {
        var job = await _db.Jobs.Include(j => j.Process).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
            return BadRequest($"Cannot cancel a {job.Status} job.");

        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    [HttpPost("{id:guid}/hold")]
    public async Task<ActionResult<JobResponseDto>> Hold(Guid id)
    {
        var job = await _db.Jobs.Include(j => j.Process).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status != JobStatus.InProgress)
            return BadRequest($"Cannot hold a job with status '{job.Status}'. Must be InProgress.");

        job.Status = JobStatus.OnHold;

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<JobResponseDto>> Resume(Guid id)
    {
        var job = await _db.Jobs.Include(j => j.Process).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        if (job.Status != JobStatus.OnHold)
            return BadRequest($"Cannot resume a job with status '{job.Status}'. Must be OnHold.");

        job.Status = JobStatus.InProgress;

        await _db.SaveChangesAsync();
        return MapJobToDto(job);
    }

    // ───── Job Sub-resources: Items ─────

    [HttpGet("{jobId:guid}/items")]
    public async Task<ActionResult<List<ItemResponseDto>>> GetItems(Guid jobId)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId)) return NotFound();

        var items = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.Grade)
            .Where(i => i.JobId == jobId)
            .OrderBy(i => i.SerialNumber)
            .ToListAsync();

        return items.Select(MapItemToDto).ToList();
    }

    [HttpGet("{jobId:guid}/batches")]
    public async Task<ActionResult<List<BatchResponseDto>>> GetBatches(Guid jobId)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId)) return NotFound();

        var batches = await _db.Batches
            .Include(b => b.Kind)
            .Include(b => b.Grade)
            .Include(b => b.Items)
            .Where(b => b.JobId == jobId)
            .OrderBy(b => b.Code)
            .ToListAsync();

        return batches.Select(MapBatchToDto).ToList();
    }

    [HttpGet("{jobId:guid}/step-executions")]
    public async Task<ActionResult<List<StepExecutionResponseDto>>> GetStepExecutions(Guid jobId)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId)) return NotFound();

        var executions = await _db.StepExecutions
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Where(se => se.JobId == jobId)
            .OrderBy(se => se.Sequence)
            .ToListAsync();

        return executions.Select(se => MapStepExecutionToDto(se)).ToList();
    }

    // ───── Mappers ─────

    private static JobResponseDto MapJobToDto(Job job, bool includeStepExecutions = false)
    {
        return new JobResponseDto(
            job.Id,
            job.Code,
            job.Name,
            job.Description,
            job.ProcessId,
            job.Process?.Name ?? "",
            job.Process?.Status.ToString() ?? "",
            job.ProcessVersion,
            job.Status.ToString(),
            job.Priority,
            job.StartedAt,
            job.CompletedAt,
            job.CreatedAt,
            job.UpdatedAt,
            includeStepExecutions
                ? job.StepExecutions.OrderBy(se => se.Sequence)
                    .Select(se => MapStepExecutionToDto(se)).ToList()
                : null,
            job.DocumentApprovalRequestId);
    }

    internal static StepExecutionResponseDto MapStepExecutionToDto(StepExecution se, bool includePortTransactions = false)
    {
        var stepName = se.ProcessStep?.StepTemplate?.Name ?? "";
        if (!string.IsNullOrEmpty(se.ProcessStep?.NameOverride))
            stepName = se.ProcessStep.NameOverride;

        return new StepExecutionResponseDto(
            se.Id,
            se.JobId,
            se.ProcessStepId,
            se.Sequence,
            stepName,
            se.Status.ToString(),
            se.StartedAt,
            se.CompletedAt,
            se.Notes,
            se.CreatedAt,
            se.UpdatedAt,
            includePortTransactions
                ? se.PortTransactions.Select(MapPortTransactionToDto).ToList()
                : null,
            se.Job?.Code,
            se.Job?.Name,
            se.ProcessStep?.ProcessId,
            se.ParallelGroup,
            se.AssignedToUserId);
    }

    internal static PortTransactionResponseDto MapPortTransactionToDto(PortTransaction pt)
    {
        return new PortTransactionResponseDto(
            pt.Id,
            pt.StepExecutionId,
            pt.PortId,
            pt.Port?.Name ?? "",
            pt.Port?.Direction.ToString() ?? "",
            pt.ItemId,
            pt.Item?.SerialNumber,
            pt.BatchId,
            pt.Batch?.Code,
            pt.Quantity,
            pt.CreatedAt);
    }

    internal static ItemResponseDto MapItemToDto(Item item)
    {
        return new ItemResponseDto(
            item.Id,
            item.SerialNumber,
            item.KindId,
            item.Kind?.Name ?? "",
            item.GradeId,
            item.Grade?.Name ?? "",
            item.JobId,
            item.Job?.Name ?? "",
            item.BatchId,
            item.Batch?.Code,
            item.Status.ToString(),
            item.CreatedAt,
            item.UpdatedAt);
    }

    internal static BatchResponseDto MapBatchToDto(Batch batch)
    {
        return new BatchResponseDto(
            batch.Id,
            batch.Code,
            batch.KindId,
            batch.Kind?.Name ?? "",
            batch.GradeId,
            batch.Grade?.Name ?? "",
            batch.JobId,
            batch.Job?.Name ?? "",
            batch.Quantity,
            batch.Status.ToString(),
            batch.Items?.Count ?? 0,
            batch.CreatedAt,
            batch.UpdatedAt);
    }
}
