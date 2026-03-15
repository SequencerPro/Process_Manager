using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using System.Security.Claims;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/document-approvals")]
public class DocumentApprovalsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public DocumentApprovalsController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<DocumentApprovalRequestDto>>> GetAll(
        [FromQuery] Guid? processId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.DocumentApprovalRequests
            .Include(d => d.Process)
            .Include(d => d.ApprovalJob)
            .AsQueryable();

        if (processId.HasValue)
            query = query.Where(d => d.ProcessId == processId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentApprovalStatus>(status, true, out var ds))
            query = query.Where(d => d.Status == ds);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<DocumentApprovalRequestDto>(
            items.Select(MapToDto).ToList(),
            totalCount, page, pageSize);
    }

    // ───── Get by ID ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentApprovalRequestDto>> GetById(Guid id)
    {
        var dar = await Load(id);
        if (dar is null) return NotFound();
        return MapToDto(dar);
    }

    // ───── Submit for Approval ─────

    [HttpPost("submit")]
    public async Task<ActionResult<DocumentApprovalRequestDto>> Submit(DocumentSubmitForApprovalDto dto)
    {
        // Load and validate the document
        var process = await _db.Processes
            .Include(p => p.ProcessSteps)
            .FirstOrDefaultAsync(p => p.Id == dto.ProcessId);

        if (process is null) return BadRequest("Process not found.");

        if (process.Status != ProcessStatus.Draft)
            return BadRequest($"Only Draft processes can be submitted for approval. Current status: {process.Status}.");

        if (process.ProcessRole == ProcessRole.ManufacturingProcess)
            return BadRequest("ManufacturingProcess-role processes are not subject to formal document approval routing. Use the Approval Queue for ad-hoc approvals.");

        if (process.ProcessRole == ProcessRole.ApprovalProcess)
            return BadRequest("ApprovalProcess-role templates cannot be submitted for approval via this flow.");

        if (string.IsNullOrWhiteSpace(dto.ChangeDescription))
            return BadRequest("ChangeDescription is required before submission.");

        // Load the approval process template
        Guid approvalProcessId = process.ApprovalProcessId
            ?? await GetDefaultApprovalProcessIdAsync();

        if (approvalProcessId == Guid.Empty)
            return BadRequest("No ApprovalProcess template is configured for this document. Assign an ApprovalProcessId or create a seeded Standard Document Approval process.");

        var approvalProcess = await _db.Processes
            .Include(p => p.ProcessSteps).ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(p => p.Id == approvalProcessId);

        if (approvalProcess is null)
            return BadRequest("Configured ApprovalProcess template not found.");

        if (approvalProcess.ProcessRole != ProcessRole.ApprovalProcess)
            return BadRequest("The linked ApprovalProcessId does not point to an ApprovalProcess-role process.");

        // Validate step assignments — every step needs an assignee
        foreach (var step in approvalProcess.ProcessSteps)
        {
            if (!dto.StepAssignments.ContainsKey(step.Id) || string.IsNullOrWhiteSpace(dto.StepAssignments[step.Id]))
                return BadRequest($"Missing user assignment for approval step '{step.StepTemplate?.Name ?? step.Id.ToString()}'.");
        }

        var submittedBy = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Unknown";

        // Update process fields
        process.ChangeDescription = dto.ChangeDescription;
        if (!string.IsNullOrWhiteSpace(dto.RevisionCode))
            process.RevisionCode = dto.RevisionCode;
        if (dto.EffectiveDate.HasValue)
            process.EffectiveDate = dto.EffectiveDate;

        // Create the approval job
        var jobCode = $"APR-{process.Code}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var job = new Job
        {
            Code = jobCode,
            Name = $"Approval: {process.Code} v{process.Version}",
            Description = $"Document approval for {process.Name} (Rev {process.RevisionCode ?? process.Version.ToString()})",
            ProcessId = approvalProcessId,
            ProcessVersion = approvalProcess.Version,
            Status = JobStatus.InProgress,
            Priority = 10,
            StartedAt = DateTime.UtcNow,
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(); // Flush job to get its Id

        // Create the DocumentApprovalRequest
        var dar = new DocumentApprovalRequest
        {
            ProcessId = dto.ProcessId,
            ProcessVersion = process.Version,
            ApprovalJobId = job.Id,
            Status = DocumentApprovalStatus.Pending,
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow,
        };

        _db.DocumentApprovalRequests.Add(dar);
        await _db.SaveChangesAsync(); // Flush DAR to get its Id

        // Link job back to the DAR
        job.DocumentApprovalRequestId = dar.Id;

        // Create parallel step executions (all ParallelGroup = 1)
        foreach (var step in approvalProcess.ProcessSteps.OrderBy(ps => ps.Sequence))
        {
            var assignedUserId = dto.StepAssignments.TryGetValue(step.Id, out var uid) ? uid : null;
            var stepExecution = new StepExecution
            {
                JobId = job.Id,
                ProcessStepId = step.Id,
                Sequence = step.Sequence,
                Status = StepExecutionStatus.Pending,
                ParallelGroup = 1,
                AssignedToUserId = assignedUserId,
            };
            _db.StepExecutions.Add(stepExecution);
        }

        // Mark document as PendingApproval
        process.Status = ProcessStatus.PendingApproval;

        await _db.SaveChangesAsync();

        var result = await Load(dar.Id);
        return CreatedAtAction(nameof(GetById), new { id = dar.Id }, MapToDto(result!));
    }

    // ───── Withdraw ─────

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<DocumentApprovalRequestDto>> Withdraw(Guid id)
    {
        var dar = await Load(id);
        if (dar is null) return NotFound();

        if (dar.Status != DocumentApprovalStatus.Pending)
            return BadRequest($"Only Pending approval requests can be withdrawn. Current status: {dar.Status}.");

        var submittedBy = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Unknown";

        // Cancel open step executions
        var openSteps = await _db.StepExecutions
            .Where(s => s.JobId == dar.ApprovalJobId
                && (s.Status == StepExecutionStatus.Pending || s.Status == StepExecutionStatus.InProgress))
            .ToListAsync();

        foreach (var step in openSteps)
            step.Status = StepExecutionStatus.Skipped;

        // Close the job
        var job = await _db.Jobs.FindAsync(dar.ApprovalJobId);
        if (job is not null)
        {
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
        }

        dar.Status = DocumentApprovalStatus.Withdrawn;

        // Revert document to Draft
        var process = await _db.Processes.FindAsync(dar.ProcessId);
        if (process is not null)
            process.Status = ProcessStatus.Draft;

        await _db.SaveChangesAsync();

        return MapToDto(dar);
    }

    // ───── Helpers ─────

    private async Task<DocumentApprovalRequest?> Load(Guid id) =>
        await _db.DocumentApprovalRequests
            .Include(d => d.Process)
            .Include(d => d.ApprovalJob)
            .FirstOrDefaultAsync(d => d.Id == id);

    /// <summary>Returns the Id of the first active ApprovalProcess-role process (fallback when no explicit link).</summary>
    private async Task<Guid> GetDefaultApprovalProcessIdAsync()
    {
        var ap = await _db.Processes
            .Where(p => p.ProcessRole == ProcessRole.ApprovalProcess && p.IsActive && p.Status == ProcessStatus.Released)
            .OrderBy(p => p.Name)
            .FirstOrDefaultAsync();

        return ap?.Id ?? Guid.Empty;
    }

    private static DocumentApprovalRequestDto MapToDto(DocumentApprovalRequest d) => new(
        d.Id,
        d.ProcessId,
        d.Process?.Code ?? "",
        d.Process?.Name ?? "",
        d.ProcessVersion,
        d.ApprovalJobId,
        d.ApprovalJob?.Code ?? "",
        d.Status.ToString(),
        d.SubmittedBy,
        d.SubmittedAt,
        d.CreatedAt,
        d.UpdatedAt
    );
}
