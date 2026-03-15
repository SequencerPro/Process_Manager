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
[Route("api/step-executions")]
public class StepExecutionsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public StepExecutionsController(ProcessManagerDbContext db) => _db = db;

    // ───── List All ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<StepExecutionResponseDto>>> GetAll(
        [FromQuery] Guid? jobId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.StepExecutions
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Include(se => se.Job)
            .AsQueryable();

        if (jobId.HasValue)
            query = query.Where(se => se.JobId == jobId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StepExecutionStatus>(status, true, out var s))
            query = query.Where(se => se.Status == s);

        var totalCount = await query.CountAsync();

        var executions = await query
            .OrderByDescending(se => se.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<StepExecutionResponseDto>(
            executions.Select(se => JobsController.MapStepExecutionToDto(se)).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get Detail ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StepExecutionResponseDto>> GetById(Guid id)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Include(s => s.Job)
            .Include(s => s.PortTransactions)
                .ThenInclude(pt => pt.Port)
            .Include(s => s.PortTransactions)
                .ThenInclude(pt => pt.Item)
            .Include(s => s.PortTransactions)
                .ThenInclude(pt => pt.Batch)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();
        return JobsController.MapStepExecutionToDto(se, includePortTransactions: true);
    }

    // ───── Lifecycle Transitions ─────

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<StepExecutionResponseDto>> Start(Guid id)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .Include(s => s.Job)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        if (se.Status != StepExecutionStatus.Pending)
            return BadRequest($"Cannot start a step execution with status '{se.Status}'. Must be Pending.");

        if (se.Job.Status != JobStatus.InProgress)
            return BadRequest("Cannot start a step execution when the job is not InProgress.");

        // Enforce ordering: previous step must be completed or skipped
        if (se.Sequence > 1)
        {
            var prev = await _db.StepExecutions
                .FirstOrDefaultAsync(s => s.JobId == se.JobId && s.Sequence == se.Sequence - 1);

            if (prev != null &&
                prev.Status != StepExecutionStatus.Completed &&
                prev.Status != StepExecutionStatus.Skipped)
            {
                return BadRequest($"Cannot start step {se.Sequence} — step {prev.Sequence} is still '{prev.Status}'.");
            }
        }

        se.Status = StepExecutionStatus.InProgress;
        se.StartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return JobsController.MapStepExecutionToDto(se);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<StepExecutionResponseDto>> Complete(Guid id)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        if (se.Status != StepExecutionStatus.InProgress)
            return BadRequest($"Cannot complete a step execution with status '{se.Status}'. Must be InProgress.");

        se.Status = StepExecutionStatus.Completed;
        se.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // ── Phase 14: Approval completion hook ──────────────────────────────
        // If this step is part of an approval job, evaluate the decision prompt.
        var job = await _db.Jobs.FindAsync(se.JobId);
        if (job?.DocumentApprovalRequestId.HasValue == true)
            await ProcessApprovalStepCompletion(se, job.DocumentApprovalRequestId.Value);

        return JobsController.MapStepExecutionToDto(se);
    }

    /// <summary>
    /// Inspects the Decision prompt response on this approval step and drives
    /// DocumentApprovalRequest / Process lifecycle transitions.
    /// </summary>
    private async Task ProcessApprovalStepCompletion(StepExecution completedStep, Guid darId)
    {
        var dar = await _db.DocumentApprovalRequests.FindAsync(darId);
        if (dar is null || dar.Status != DocumentApprovalStatus.Pending) return;

        // Read the Decision prompt response for this step execution
        var decisionResponse = await _db.PromptResponses
            .Where(r => r.StepExecutionId == completedStep.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.ResponseValue == "Reject" || r.ResponseValue == "Approve");

        if (decisionResponse is null) return; // No decision prompt found — nothing to drive

        var process = await _db.Processes.FindAsync(dar.ProcessId);
        if (process is null) return;

        if (decisionResponse.ResponseValue == "Reject")
        {
            // Cancel all remaining open step executions in the job
            var openSteps = await _db.StepExecutions
                .Where(s => s.JobId == completedStep.JobId
                    && s.Id != completedStep.Id
                    && (s.Status == StepExecutionStatus.Pending || s.Status == StepExecutionStatus.InProgress))
                .ToListAsync();

            foreach (var step in openSteps)
            {
                step.Status = StepExecutionStatus.Skipped;
            }

            // Close the job
            var job = await _db.Jobs.FindAsync(completedStep.JobId);
            if (job is not null)
            {
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
            }

            dar.Status = DocumentApprovalStatus.Rejected;
            process.Status = ProcessStatus.Draft;
        }
        else if (decisionResponse.ResponseValue == "Approve")
        {
            // Check if all parallel steps in the job are now complete
            var allSteps = await _db.StepExecutions
                .Where(s => s.JobId == completedStep.JobId)
                .ToListAsync();

            var allApproved = allSteps.All(s =>
                s.Status == StepExecutionStatus.Completed ||
                s.Status == StepExecutionStatus.Skipped);

            if (allApproved)
            {
                var approvedAt = DateTime.UtcNow;
                dar.Status = DocumentApprovalStatus.Approved;
                process.Status = ProcessStatus.Released;
                process.EffectiveDate ??= approvedAt;

                // Close the job
                var job = await _db.Jobs.FindAsync(completedStep.JobId);
                if (job is not null)
                {
                    job.Status = JobStatus.Completed;
                    job.CompletedAt = approvedAt;
                }

                // Supersede the previous Released revision in the same lineage
                if (process.ParentProcessId.HasValue)
                {
                    var previousReleased = await _db.Processes
                        .Where(p => p.Id == process.ParentProcessId.Value
                            && p.Status == ProcessStatus.Released)
                        .FirstOrDefaultAsync();

                    if (previousReleased is not null)
                        previousReleased.Status = ProcessStatus.Superseded;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    [HttpPost("{id:guid}/skip")]
    public async Task<ActionResult<StepExecutionResponseDto>> Skip(Guid id)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        if (se.Status != StepExecutionStatus.Pending)
            return BadRequest($"Cannot skip a step execution with status '{se.Status}'. Must be Pending.");

        se.Status = StepExecutionStatus.Skipped;

        await _db.SaveChangesAsync();
        return JobsController.MapStepExecutionToDto(se);
    }

    [HttpPost("{id:guid}/fail")]
    public async Task<ActionResult<StepExecutionResponseDto>> Fail(Guid id)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        if (se.Status != StepExecutionStatus.InProgress)
            return BadRequest($"Cannot fail a step execution with status '{se.Status}'. Must be InProgress.");

        se.Status = StepExecutionStatus.Failed;
        se.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return JobsController.MapStepExecutionToDto(se);
    }

    [HttpPut("{id:guid}/notes")]
    public async Task<ActionResult<StepExecutionResponseDto>> UpdateNotes(Guid id, UpdateStepExecutionNotesDto dto)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        se.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return JobsController.MapStepExecutionToDto(se);
    }

    // ───── Port Transactions ─────

    [HttpGet("{id:guid}/port-transactions")]
    public async Task<ActionResult<List<PortTransactionResponseDto>>> GetPortTransactions(Guid id)
    {
        if (!await _db.StepExecutions.AnyAsync(se => se.Id == id)) return NotFound();

        var transactions = await _db.PortTransactions
            .Include(pt => pt.Port)
            .Include(pt => pt.Item)
            .Include(pt => pt.Batch)
            .Where(pt => pt.StepExecutionId == id)
            .OrderBy(pt => pt.CreatedAt)
            .ToListAsync();

        return transactions.Select(JobsController.MapPortTransactionToDto).ToList();
    }

    [HttpPost("{id:guid}/port-transactions")]
    public async Task<ActionResult<PortTransactionResponseDto>> AddPortTransaction(Guid id, CreatePortTransactionDto dto)
    {
        var se = await _db.StepExecutions
            .Include(s => s.ProcessStep)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (se is null) return NotFound();

        if (se.Status != StepExecutionStatus.InProgress)
            return BadRequest("Can only record port transactions on an InProgress step execution.");

        // Validate port belongs to this step's template
        var port = await _db.Ports
            .FirstOrDefaultAsync(p => p.Id == dto.PortId && p.StepTemplateId == se.ProcessStep.StepTemplateId);

        if (port is null)
            return BadRequest("Port does not belong to this step's template.");

        // Validate item if provided
        Item? item = null;
        if (dto.ItemId.HasValue)
        {
            item = await _db.Items
                .Include(i => i.Kind)
                .Include(i => i.Grade)
                .FirstOrDefaultAsync(i => i.Id == dto.ItemId.Value);

            if (item is null) return BadRequest($"Item '{dto.ItemId}' not found.");

            // For input ports: item Kind must match port Kind
            if (item.KindId != port.KindId)
                return BadRequest($"Item Kind '{item.Kind.Name}' does not match port Kind.");
        }

        // Validate batch if provided
        Batch? batch = null;
        if (dto.BatchId.HasValue)
        {
            batch = await _db.Batches
                .Include(b => b.Kind)
                .FirstOrDefaultAsync(b => b.Id == dto.BatchId.Value);

            if (batch is null) return BadRequest($"Batch '{dto.BatchId}' not found.");

            if (batch.KindId != port.KindId)
                return BadRequest($"Batch Kind '{batch.Kind.Name}' does not match port Kind.");
        }

        var pt = new PortTransaction
        {
            StepExecutionId = id,
            PortId = dto.PortId,
            ItemId = dto.ItemId,
            BatchId = dto.BatchId,
            Quantity = dto.Quantity
        };

        _db.PortTransactions.Add(pt);

        // For output ports: update item/batch grade to match port's declared grade (Material ports only)
        if (port.Direction == PortDirection.Output && port.GradeId.HasValue)
        {
            if (item != null)
            {
                item.GradeId = port.GradeId.Value;
                item.Status = ItemStatus.InProcess;
            }
            if (batch != null)
            {
                batch.GradeId = port.GradeId.Value;
                // Also update all items in the batch
                var batchItems = await _db.Items.Where(i => i.BatchId == batch.Id).ToListAsync();
                foreach (var bi in batchItems)
                    bi.GradeId = port.GradeId.Value;
            }
        }

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        var result = await _db.PortTransactions
            .Include(p => p.Port)
            .Include(p => p.Item)
            .Include(p => p.Batch)
            .FirstAsync(p => p.Id == pt.Id);

        return CreatedAtAction(nameof(GetPortTransactions), new { id }, JobsController.MapPortTransactionToDto(result));
    }

    // ───── Prompt Responses ─────

    [HttpGet("{id:guid}/prompt-responses")]
    public async Task<ActionResult<List<PromptResponseDto>>> GetPromptResponses(Guid id)
    {
        if (!await _db.StepExecutions.AnyAsync(se => se.Id == id)) return NotFound();

        var responses = await _db.PromptResponses
            .Include(r => r.ProcessStepContent)
            .Include(r => r.StepTemplateContent)
            .Where(r => r.StepExecutionId == id)
            .OrderBy(r => r.RespondedAt)
            .ToListAsync();

        return responses.Select(MapPromptResponseToDto).ToList();
    }

    [HttpPost("{id:guid}/prompt-responses")]
    public async Task<IActionResult> SavePromptResponses(Guid id, SavePromptResponsesDto dto)
    {
        if (!await _db.StepExecutions.AnyAsync(se => se.Id == id)) return NotFound();

        foreach (var item in dto.Responses)
        {
            if (item.ProcessStepContentId is null && item.StepTemplateContentId is null)
                return BadRequest("Each response must reference either a ProcessStepContentId or StepTemplateContentId.");

            // Determine IsOutOfRange
            bool isOutOfRange = false;
            if (item.ProcessStepContentId.HasValue)
            {
                var content = await _db.ProcessStepContents.FindAsync(item.ProcessStepContentId);
                if (content?.PromptType == Domain.Enums.PromptType.NumericEntry &&
                    decimal.TryParse(item.ResponseValue, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    isOutOfRange = (content.MinValue.HasValue && val < content.MinValue) ||
                                   (content.MaxValue.HasValue && val > content.MaxValue);
                }
            }
            else if (item.StepTemplateContentId.HasValue)
            {
                var content = await _db.StepTemplateContents.FindAsync(item.StepTemplateContentId);
                if (content?.PromptType == Domain.Enums.PromptType.NumericEntry &&
                    decimal.TryParse(item.ResponseValue, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    isOutOfRange = (content.MinValue.HasValue && val < content.MinValue) ||
                                   (content.MaxValue.HasValue && val > content.MaxValue);
                }
            }

            // Upsert by (StepExecutionId + content block id)
            var existing = await _db.PromptResponses.FirstOrDefaultAsync(r =>
                r.StepExecutionId == id &&
                r.ProcessStepContentId == item.ProcessStepContentId &&
                r.StepTemplateContentId == item.StepTemplateContentId);

            if (existing is not null)
            {
                existing.ResponseValue = item.ResponseValue;
                existing.IsOutOfRange = isOutOfRange;
                existing.OverrideNote = item.OverrideNote;
                existing.RespondedAt = DateTime.UtcNow;
            }
            else
            {
                _db.PromptResponses.Add(new PromptResponse
                {
                    StepExecutionId = id,
                    ProcessStepContentId = item.ProcessStepContentId,
                    StepTemplateContentId = item.StepTemplateContentId,
                    ResponseValue = item.ResponseValue,
                    IsOutOfRange = isOutOfRange,
                    OverrideNote = item.OverrideNote,
                    RespondedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static PromptResponseDto MapPromptResponseToDto(PromptResponse r)
    {
        var label = r.ProcessStepContent?.Label ?? r.StepTemplateContent?.Label ?? "(deleted prompt)";
        var promptType = r.ProcessStepContent?.PromptType?.ToString()
                      ?? r.StepTemplateContent?.PromptType?.ToString()
                      ?? "Unknown";
        return new PromptResponseDto(
            r.Id, r.StepExecutionId,
            r.ProcessStepContentId, r.StepTemplateContentId,
            label, promptType,
            r.ResponseValue, r.IsOutOfRange, r.OverrideNote, r.RespondedAt);
    }

    // ───── Execution Data ─────

    [HttpGet("{id:guid}/data")]
    public async Task<ActionResult<List<ExecutionDataResponseDto>>> GetData(Guid id)
    {
        if (!await _db.StepExecutions.AnyAsync(se => se.Id == id)) return NotFound();

        var data = await _db.ExecutionData
            .Where(ed => ed.StepExecutionId == id)
            .OrderBy(ed => ed.Key)
            .ToListAsync();

        return data.Select(MapExecutionDataToDto).ToList();
    }

    [HttpPost("{id:guid}/data")]
    public async Task<ActionResult<ExecutionDataResponseDto>> AddData(Guid id, CreateExecutionDataDto dto)
    {
        if (!await _db.StepExecutions.AnyAsync(se => se.Id == id)) return NotFound();

        var ed = new ExecutionData
        {
            Key = dto.Key,
            Value = dto.Value,
            DataType = dto.DataType,
            UnitOfMeasure = dto.UnitOfMeasure,
            StepExecutionId = id
        };

        _db.ExecutionData.Add(ed);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetData), new { id }, MapExecutionDataToDto(ed));
    }

    internal static ExecutionDataResponseDto MapExecutionDataToDto(ExecutionData ed)
    {
        return new ExecutionDataResponseDto(
            ed.Id, ed.Key, ed.Value, ed.DataType, ed.UnitOfMeasure,
            ed.StepExecutionId, ed.BatchId, ed.ItemId, ed.CreatedAt);
    }
}
