using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/change-orders")]
public class ChangeOrdersController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ChangeOrdersController(ProcessManagerDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ChangeOrderSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.ChangeOrders
            .Include(c => c.Impacts)
            .Include(c => c.Approvers)
            .Include(c => c.Tasks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ChangeOrderStatus>(status, true, out var st))
            query = query.Where(c => c.Status == st);

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ChangeOrderType>(type, true, out var tp))
            query = query.Where(c => c.Type == tp);

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<ChangeOrderPriority>(priority, true, out var pr))
            query = query.Where(c => c.Priority == pr);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(s)
                                  || c.Title.ToLower().Contains(s)
                                  || c.RequestedByDisplayName.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummaryDto).ToList();

        return new PaginatedResponse<ChangeOrderSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChangeOrderResponseDto>> GetById(Guid id)
    {
        var eco = await _db.ChangeOrders
            .Include(c => c.Impacts)
            .Include(c => c.Approvers)
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (eco is null) return NotFound();

        return MapToDto(eco);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<ChangeOrderResponseDto>> Create(CreateChangeOrderDto dto)
    {
        if (!Enum.TryParse<ChangeOrderType>(dto.Type, true, out var ecoType))
            return BadRequest($"Invalid type '{dto.Type}'. Valid values: {string.Join(", ", Enum.GetNames<ChangeOrderType>())}");

        if (!Enum.TryParse<ChangeOrderPriority>(dto.Priority, true, out var ecoPriority))
            return BadRequest($"Invalid priority '{dto.Priority}'. Valid values: {string.Join(", ", Enum.GetNames<ChangeOrderPriority>())}");

        var year = DateTime.UtcNow.Year;
        var existingCount = await _db.ChangeOrders
            .CountAsync(c => c.Code.StartsWith($"ECO-{year}-"));
        var code = $"ECO-{year}-{(existingCount + 1):D3}";

        var eco = new ChangeOrder
        {
            Code = code,
            Type = ecoType,
            Priority = ecoPriority,
            Status = ChangeOrderStatus.Draft,
            Title = dto.Title,
            Description = dto.Description,
            Justification = dto.Justification,
            RequestedByUserId = dto.RequestedByUserId,
            RequestedByDisplayName = dto.RequestedByDisplayName,
            RequestedAt = DateTime.UtcNow,
            TargetImplementationDate = dto.TargetImplementationDate,
        };

        _db.ChangeOrders.Add(eco);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = eco.Id }, MapToDto(eco));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChangeOrderResponseDto>> Update(Guid id, UpdateChangeOrderDto dto)
    {
        var eco = await _db.ChangeOrders
            .Include(c => c.Impacts)
            .Include(c => c.Approvers)
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot update a closed or rejected change order.");

        if (dto.Title is not null) eco.Title = dto.Title;
        if (dto.Description is not null) eco.Description = dto.Description;
        if (dto.Justification is not null) eco.Justification = dto.Justification;
        if (dto.TargetImplementationDate.HasValue) eco.TargetImplementationDate = dto.TargetImplementationDate;
        if (dto.Priority is not null)
        {
            if (!Enum.TryParse<ChangeOrderPriority>(dto.Priority, true, out var pr))
                return BadRequest($"Invalid priority '{dto.Priority}'.");
            eco.Priority = pr;
        }

        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    // ── Delete (Draft only) ───────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Draft)
            return BadRequest("Only draft change orders can be deleted.");

        _db.ChangeOrders.Remove(eco);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Lifecycle Transitions ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ChangeOrderResponseDto>> Submit(Guid id, TransitionChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Draft)
            return BadRequest($"Cannot submit a change order in status '{eco.Status}'. Must be Draft.");

        if (!eco.Impacts.Any())
            return BadRequest("At least one impact item is required before submitting for analysis.");

        eco.Status = ChangeOrderStatus.ImpactAnalysis;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    [HttpPost("{id:guid}/request-approval")]
    public async Task<ActionResult<ChangeOrderResponseDto>> RequestApproval(Guid id, TransitionChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.ImpactAnalysis)
            return BadRequest($"Cannot request approval from status '{eco.Status}'. Must be ImpactAnalysis.");

        if (!eco.Approvers.Any())
            return BadRequest("At least one approver must be added before requesting approval.");

        eco.Status = ChangeOrderStatus.Approval;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ChangeOrderResponseDto>> Approve(Guid id, TransitionChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Approval)
            return BadRequest($"Cannot approve from status '{eco.Status}'. Must be Approval.");

        var allDecided = eco.Approvers.All(a => a.Decision != ApproverDecision.Pending);
        var anyRejected = eco.Approvers.Any(a => a.Decision == ApproverDecision.Rejected);

        if (!allDecided)
            return BadRequest("All approvers must record a decision before the change order can be approved.");

        if (anyRejected)
            return BadRequest("Cannot approve — one or more approvers rejected the change order.");

        eco.Status = ChangeOrderStatus.Implementation;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ChangeOrderResponseDto>> Reject(Guid id, RejectChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Approval && eco.Status != ChangeOrderStatus.ImpactAnalysis)
            return BadRequest($"Cannot reject from status '{eco.Status}'. Must be ImpactAnalysis or Approval.");

        eco.Status = ChangeOrderStatus.Rejected;
        eco.RejectionReason = dto.Reason;
        eco.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    [HttpPost("{id:guid}/complete-implementation")]
    public async Task<ActionResult<ChangeOrderResponseDto>> CompleteImplementation(Guid id, TransitionChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Implementation)
            return BadRequest($"Cannot complete implementation from status '{eco.Status}'. Must be Implementation.");

        var incompleteTasks = eco.Tasks.Any(t => t.Status == ActionItemStatus.Open);
        if (incompleteTasks)
            return BadRequest("All implementation tasks must be completed before moving to verification.");

        eco.Status = ChangeOrderStatus.Verification;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ChangeOrderResponseDto>> Close(Guid id, TransitionChangeOrderDto dto)
    {
        var eco = await LoadFull(id);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Verification)
            return BadRequest($"Cannot close from status '{eco.Status}'. Must be Verification.");

        eco.Status = ChangeOrderStatus.Closed;
        eco.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(eco);
    }

    // ── Impact Items ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/impacts")]
    public async Task<ActionResult<List<ChangeOrderImpactResponseDto>>> GetImpacts(Guid id)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        var impacts = await _db.ChangeOrderImpacts
            .Where(i => i.ChangeOrderId == id)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        return impacts.Select(MapImpactDto).ToList();
    }

    [HttpPost("{id:guid}/impacts")]
    public async Task<ActionResult<ChangeOrderImpactResponseDto>> AddImpact(Guid id, CreateChangeOrderImpactDto dto)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot modify impacts on a closed or rejected change order.");

        if (!Enum.TryParse<ChangeOrderImpactEntityType>(dto.AffectedEntityType, true, out var entityType))
            return BadRequest($"Invalid entity type '{dto.AffectedEntityType}'. Valid values: {string.Join(", ", Enum.GetNames<ChangeOrderImpactEntityType>())}");

        var impact = new ChangeOrderImpact
        {
            ChangeOrderId = id,
            AffectedEntityType = entityType,
            AffectedEntityId = dto.AffectedEntityId,
            AffectedEntityName = dto.AffectedEntityName,
            ImpactDescription = dto.ImpactDescription,
            MitigationPlan = dto.MitigationPlan,
        };

        _db.ChangeOrderImpacts.Add(impact);
        await _db.SaveChangesAsync();
        return MapImpactDto(impact);
    }

    [HttpDelete("{ecoId:guid}/impacts/{impactId:guid}")]
    public async Task<IActionResult> DeleteImpact(Guid ecoId, Guid impactId)
    {
        var impact = await _db.ChangeOrderImpacts
            .FirstOrDefaultAsync(i => i.Id == impactId && i.ChangeOrderId == ecoId);
        if (impact is null) return NotFound();

        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == ecoId);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot modify impacts on a closed or rejected change order.");

        _db.ChangeOrderImpacts.Remove(impact);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Approvers ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/approvers")]
    public async Task<ActionResult<List<ChangeOrderApproverResponseDto>>> GetApprovers(Guid id)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        var approvers = await _db.ChangeOrderApprovers
            .Where(a => a.ChangeOrderId == id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return approvers.Select(MapApproverDto).ToList();
    }

    [HttpPost("{id:guid}/approvers")]
    public async Task<ActionResult<ChangeOrderApproverResponseDto>> AddApprover(Guid id, AddChangeOrderApproverDto dto)
    {
        var eco = await _db.ChangeOrders
            .Include(c => c.Approvers)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot add approvers to a closed or rejected change order.");

        if (eco.Approvers.Any(a => a.UserId == dto.UserId))
            return Conflict("This user is already an approver on this change order.");

        var approver = new ChangeOrderApprover
        {
            ChangeOrderId = id,
            UserId = dto.UserId,
            DisplayName = dto.DisplayName,
            Role = dto.Role,
            Decision = ApproverDecision.Pending,
        };

        _db.ChangeOrderApprovers.Add(approver);
        await _db.SaveChangesAsync();
        return MapApproverDto(approver);
    }

    [HttpPost("{ecoId:guid}/approvers/{approverId:guid}/decide")]
    public async Task<ActionResult<ChangeOrderApproverResponseDto>> RecordDecision(
        Guid ecoId, Guid approverId, RecordApproverDecisionDto dto)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == ecoId);
        if (eco is null) return NotFound();

        if (eco.Status != ChangeOrderStatus.Approval)
            return BadRequest("Decisions can only be recorded when the change order is in Approval status.");

        var approver = await _db.ChangeOrderApprovers
            .FirstOrDefaultAsync(a => a.Id == approverId && a.ChangeOrderId == ecoId);
        if (approver is null) return NotFound();

        if (!Enum.TryParse<ApproverDecision>(dto.Decision, true, out var decision))
            return BadRequest($"Invalid decision '{dto.Decision}'. Valid values: Approved, Rejected, Abstained");

        if (decision == ApproverDecision.Pending)
            return BadRequest("Cannot set decision back to Pending.");

        approver.Decision = decision;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comments = dto.Comments;
        await _db.SaveChangesAsync();
        return MapApproverDto(approver);
    }

    [HttpDelete("{ecoId:guid}/approvers/{approverId:guid}")]
    public async Task<IActionResult> RemoveApprover(Guid ecoId, Guid approverId)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == ecoId);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot remove approvers from a closed or rejected change order.");

        var approver = await _db.ChangeOrderApprovers
            .FirstOrDefaultAsync(a => a.Id == approverId && a.ChangeOrderId == ecoId);
        if (approver is null) return NotFound();

        _db.ChangeOrderApprovers.Remove(approver);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/tasks")]
    public async Task<ActionResult<List<ChangeOrderTaskResponseDto>>> GetTasks(Guid id)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        var tasks = await _db.ChangeOrderTasks
            .Where(t => t.ChangeOrderId == id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(MapTaskDto).ToList();
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<ActionResult<ChangeOrderTaskResponseDto>> AddTask(Guid id, CreateChangeOrderTaskDto dto)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot add tasks to a closed or rejected change order.");

        var task = new ChangeOrderTask
        {
            ChangeOrderId = id,
            Title = dto.Title,
            Description = dto.Description,
            AssigneeUserId = dto.AssigneeUserId,
            AssigneeDisplayName = dto.AssigneeDisplayName,
            DueDate = dto.DueDate,
            Status = ActionItemStatus.Open,
        };

        _db.ChangeOrderTasks.Add(task);
        await _db.SaveChangesAsync();
        return MapTaskDto(task);
    }

    [HttpPost("{ecoId:guid}/tasks/{taskId:guid}/complete")]
    public async Task<ActionResult<ChangeOrderTaskResponseDto>> CompleteTask(
        Guid ecoId, Guid taskId, CompleteChangeOrderTaskDto dto)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == ecoId);
        if (eco is null) return NotFound();

        var task = await _db.ChangeOrderTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ChangeOrderId == ecoId);
        if (task is null) return NotFound();

        if (task.Status != ActionItemStatus.Open)
            return BadRequest("Task is not open.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        task.Status = ActionItemStatus.Complete;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedByUserId = userId;
        task.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return MapTaskDto(task);
    }

    [HttpDelete("{ecoId:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid ecoId, Guid taskId)
    {
        var eco = await _db.ChangeOrders.FirstOrDefaultAsync(c => c.Id == ecoId);
        if (eco is null) return NotFound();

        if (eco.Status == ChangeOrderStatus.Closed || eco.Status == ChangeOrderStatus.Rejected)
            return BadRequest("Cannot delete tasks from a closed or rejected change order.");

        var task = await _db.ChangeOrderTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ChangeOrderId == ecoId);
        if (task is null) return NotFound();

        _db.ChangeOrderTasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<ChangeOrderDashboardDto>> GetDashboard()
    {
        var all = await _db.ChangeOrders
            .Include(c => c.Impacts)
            .Include(c => c.Approvers)
            .Include(c => c.Tasks)
            .ToListAsync();

        var open = all.Where(c => c.Status != ChangeOrderStatus.Closed && c.Status != ChangeOrderStatus.Rejected).ToList();
        var closed = all.Where(c => c.Status == ChangeOrderStatus.Closed).ToList();
        var rejected = all.Where(c => c.Status == ChangeOrderStatus.Rejected).ToList();

        var avgDaysToClose = closed.Any()
            ? closed.Average(c => (c.ClosedAt!.Value - c.CreatedAt).TotalDays)
            : 0;

        var overdue = open
            .Where(c => c.TargetImplementationDate.HasValue && c.TargetImplementationDate.Value < DateTime.UtcNow)
            .OrderBy(c => c.TargetImplementationDate)
            .Take(10)
            .Select(MapToSummaryDto)
            .ToList();

        return new ChangeOrderDashboardDto(
            TotalOpen: open.Count,
            TotalClosed: closed.Count,
            TotalRejected: rejected.Count,
            AvgDaysToClose: Math.Round(avgDaysToClose, 1),
            ByStatus: all.GroupBy(c => c.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByType: all.GroupBy(c => c.Type.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByPriority: all.GroupBy(c => c.Priority.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            OverdueEcos: overdue);
    }

    // ── Mapping Helpers ───────────────────────────────────────────────────────

    private async Task<ChangeOrder?> LoadFull(Guid id) =>
        await _db.ChangeOrders
            .Include(c => c.Impacts)
            .Include(c => c.Approvers)
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id);

    private static ChangeOrderResponseDto MapToDto(ChangeOrder c) => new(
        c.Id, c.Code, c.Type.ToString(), c.Priority.ToString(), c.Status.ToString(),
        c.Title, c.Description, c.Justification,
        c.RequestedByUserId, c.RequestedByDisplayName, c.RequestedAt,
        c.TargetImplementationDate, c.ClosedAt, c.RejectionReason,
        c.Impacts.Count, c.Approvers.Count, c.Tasks.Count,
        c.Tasks.Count(t => t.Status == ActionItemStatus.Complete),
        c.CreatedAt, c.UpdatedAt);

    private static ChangeOrderSummaryDto MapToSummaryDto(ChangeOrder c) => new(
        c.Id, c.Code, c.Type.ToString(), c.Priority.ToString(), c.Status.ToString(),
        c.Title, c.RequestedByDisplayName, c.RequestedAt,
        c.TargetImplementationDate,
        c.Impacts.Count, c.Approvers.Count, c.Tasks.Count,
        c.Tasks.Count(t => t.Status == ActionItemStatus.Complete),
        c.CreatedAt);

    private static ChangeOrderImpactResponseDto MapImpactDto(ChangeOrderImpact i) => new(
        i.Id, i.ChangeOrderId, i.AffectedEntityType.ToString(),
        i.AffectedEntityId, i.AffectedEntityName,
        i.ImpactDescription, i.MitigationPlan, i.CreatedAt);

    private static ChangeOrderApproverResponseDto MapApproverDto(ChangeOrderApprover a) => new(
        a.Id, a.ChangeOrderId, a.UserId, a.DisplayName, a.Role,
        a.Decision.ToString(), a.DecidedAt, a.Comments, a.CreatedAt);

    private static ChangeOrderTaskResponseDto MapTaskDto(ChangeOrderTask t) => new(
        t.Id, t.ChangeOrderId, t.Title, t.Description,
        t.AssigneeUserId, t.AssigneeDisplayName, t.DueDate,
        t.Status.ToString(), t.CompletedAt, t.CompletedByUserId, t.Notes, t.CreatedAt);
}
