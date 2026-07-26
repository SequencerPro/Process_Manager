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
[Route("api/complaints")]
public class CustomerComplaintsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public CustomerComplaintsController(ProcessManagerDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerComplaintSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.CustomerComplaints
            .Include(c => c.Investigations)
            .Include(c => c.Responses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ComplaintStatus>(status, true, out var st))
            query = query.Where(c => c.Status == st);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ComplaintCategory>(category, true, out var cat))
            query = query.Where(c => c.Category == cat);

        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<ComplaintSeverity>(severity, true, out var sev))
            query = query.Where(c => c.Severity == sev);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(s)
                                  || c.CustomerName.ToLower().Contains(s)
                                  || c.Description.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummaryDto).ToList();

        return new PaginatedResponse<CustomerComplaintSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerComplaintResponseDto>> GetById(Guid id)
    {
        var complaint = await _db.CustomerComplaints
            .Include(c => c.Investigations)
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint is null) return NotFound();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.CustomerComplaint && a.SourceEntityId == id);

        return MapToDto(complaint, aiCount);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<CustomerComplaintResponseDto>> Create(CreateCustomerComplaintDto dto)
    {
        if (!Enum.TryParse<ComplaintCategory>(dto.Category, true, out var category))
            return BadRequest($"Invalid category '{dto.Category}'. Valid values: {string.Join(", ", Enum.GetNames<ComplaintCategory>())}");

        if (!Enum.TryParse<ComplaintSeverity>(dto.Severity, true, out var severity))
            return BadRequest($"Invalid severity '{dto.Severity}'. Valid values: {string.Join(", ", Enum.GetNames<ComplaintSeverity>())}");

        var year = DateTime.UtcNow.Year;
        var existingCount = await _db.CustomerComplaints
            .CountAsync(c => c.Code.StartsWith($"CC-{year}-"));
        var code = $"CC-{year}-{(existingCount + 1):D3}";

        var complaint = new CustomerComplaint
        {
            Code = code,
            CustomerName = dto.CustomerName,
            CustomerReference = dto.CustomerReference,
            ProductKindId = dto.ProductKindId,
            LotNumber = dto.LotNumber,
            ComplaintDate = dto.ComplaintDate ?? DateTime.UtcNow,
            ReceivedDate = DateTime.UtcNow,
            Category = category,
            Severity = severity,
            Description = dto.Description,
            QuantityAffected = dto.QuantityAffected,
            Status = ComplaintStatus.New,
            OwnerUserId = dto.OwnerUserId,
            OwnerDisplayName = dto.OwnerDisplayName,
            ResponseDueDate = dto.ResponseDueDate
        };

        _db.CustomerComplaints.Add(complaint);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, MapToDto(complaint, 0));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CustomerComplaintResponseDto>> Update(Guid id, UpdateCustomerComplaintDto dto)
    {
        var complaint = await _db.CustomerComplaints
            .Include(c => c.Investigations)
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint is null) return NotFound();

        if (dto.CustomerName is not null) complaint.CustomerName = dto.CustomerName;
        if (dto.CustomerReference is not null) complaint.CustomerReference = dto.CustomerReference;
        if (dto.ProductKindId.HasValue) complaint.ProductKindId = dto.ProductKindId;
        if (dto.LotNumber is not null) complaint.LotNumber = dto.LotNumber;
        if (dto.Description is not null) complaint.Description = dto.Description;
        if (dto.QuantityAffected.HasValue) complaint.QuantityAffected = dto.QuantityAffected.Value;
        if (dto.OwnerUserId is not null) complaint.OwnerUserId = dto.OwnerUserId;
        if (dto.OwnerDisplayName is not null) complaint.OwnerDisplayName = dto.OwnerDisplayName;
        if (dto.ResponseDueDate.HasValue) complaint.ResponseDueDate = dto.ResponseDueDate;
        if (dto.CustomerSatisfied.HasValue) complaint.CustomerSatisfied = dto.CustomerSatisfied;
        if (dto.LinkedNonConformanceId.HasValue) complaint.LinkedNonConformanceId = dto.LinkedNonConformanceId;
        if (dto.LinkedCapaId.HasValue) complaint.LinkedCapaId = dto.LinkedCapaId;
        if (dto.LinkedSupplierId.HasValue) complaint.LinkedSupplierId = dto.LinkedSupplierId;

        if (!string.IsNullOrWhiteSpace(dto.Category))
        {
            if (Enum.TryParse<ComplaintCategory>(dto.Category, true, out var cat))
                complaint.Category = cat;
            else
                return BadRequest($"Invalid category '{dto.Category}'.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Severity))
        {
            if (Enum.TryParse<ComplaintSeverity>(dto.Severity, true, out var sev))
                complaint.Severity = sev;
            else
                return BadRequest($"Invalid severity '{dto.Severity}'.");
        }

        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.CustomerComplaint && a.SourceEntityId == id);

        return MapToDto(complaint, aiCount);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var complaint = await _db.CustomerComplaints.FindAsync(id);
        if (complaint is null) return NotFound();

        _db.CustomerComplaints.Remove(complaint);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Status Transition ─────────────────────────────────────────────────────

    [HttpPost("{id:guid}/transition")]
    public async Task<ActionResult<CustomerComplaintResponseDto>> TransitionStatus(Guid id, TransitionComplaintStatusDto dto)
    {
        var complaint = await _db.CustomerComplaints
            .Include(c => c.Investigations)
            .Include(c => c.Responses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint is null) return NotFound();

        if (!Enum.TryParse<ComplaintStatus>(dto.TargetStatus, true, out var target))
            return BadRequest($"Invalid target status '{dto.TargetStatus}'. Valid values: {string.Join(", ", Enum.GetNames<ComplaintStatus>())}");

        var valid = IsValidTransition(complaint.Status, target);
        if (!valid)
            return BadRequest($"Cannot transition from '{complaint.Status}' to '{target}'.");

        // Gate checks
        if (target == ComplaintStatus.UnderInvestigation && !complaint.Investigations.Any())
            return BadRequest("At least one investigation must be recorded before moving to UnderInvestigation.");

        if (target == ComplaintStatus.RootCauseIdentified && complaint.LinkedCapaId is null && complaint.LinkedNonConformanceId is null)
            return BadRequest("A linked CAPA or NonConformance is required before marking root cause as identified.");

        if (target == ComplaintStatus.ResponseSent && !complaint.Responses.Any())
            return BadRequest("At least one response must be sent before transitioning to ResponseSent.");

        if (target == ComplaintStatus.Closed)
        {
            complaint.ClosedAt = DateTime.UtcNow;
        }

        if (target == ComplaintStatus.ResponseSent)
        {
            complaint.ResponseSentAt = DateTime.UtcNow;
        }

        complaint.Status = target;
        await _db.SaveChangesAsync();

        var aiCount = await _db.ActionItems
            .CountAsync(a => a.SourceType == ActionItemSourceType.CustomerComplaint && a.SourceEntityId == id);

        return MapToDto(complaint, aiCount);
    }

    // ── Investigations ────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/investigations")]
    public async Task<ActionResult<List<ComplaintInvestigationResponseDto>>> GetInvestigations(Guid id)
    {
        var exists = await _db.CustomerComplaints.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        var investigations = await _db.ComplaintInvestigations
            .Where(i => i.CustomerComplaintId == id)
            .OrderByDescending(i => i.InvestigatedAt)
            .ToListAsync();

        return investigations.Select(MapInvestigationToDto).ToList();
    }

    [HttpPost("{id:guid}/investigations")]
    public async Task<ActionResult<ComplaintInvestigationResponseDto>> AddInvestigation(Guid id, CreateComplaintInvestigationDto dto)
    {
        var exists = await _db.CustomerComplaints.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        if (!Enum.TryParse<InvestigationType>(dto.InvestigationType, true, out var investType))
            return BadRequest($"Invalid investigation type '{dto.InvestigationType}'. Valid values: {string.Join(", ", Enum.GetNames<InvestigationType>())}");

        var investigation = new ComplaintInvestigation
        {
            CustomerComplaintId = id,
            InvestigationType = investType,
            Findings = dto.Findings,
            InvestigatedByUserId = dto.InvestigatedByUserId,
            InvestigatedByDisplayName = dto.InvestigatedByDisplayName,
            InvestigatedAt = DateTime.UtcNow
        };

        _db.ComplaintInvestigations.Add(investigation);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInvestigations), new { id }, MapInvestigationToDto(investigation));
    }

    // ── Responses ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/responses")]
    public async Task<ActionResult<List<ComplaintResponseResponseDto>>> GetResponses(Guid id)
    {
        var exists = await _db.CustomerComplaints.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        var responses = await _db.ComplaintResponses
            .Where(r => r.CustomerComplaintId == id)
            .OrderByDescending(r => r.SentAt)
            .ToListAsync();

        return responses.Select(MapResponseToDto).ToList();
    }

    [HttpPost("{id:guid}/responses")]
    public async Task<ActionResult<ComplaintResponseResponseDto>> AddResponse(Guid id, CreateComplaintResponseDto dto)
    {
        var exists = await _db.CustomerComplaints.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        if (!Enum.TryParse<ComplaintResponseType>(dto.ResponseType, true, out var respType))
            return BadRequest($"Invalid response type '{dto.ResponseType}'. Valid values: {string.Join(", ", Enum.GetNames<ComplaintResponseType>())}");

        var response = new ComplaintResponse
        {
            CustomerComplaintId = id,
            ResponseType = respType,
            Content = dto.Content,
            SentByUserId = dto.SentByUserId,
            SentByDisplayName = dto.SentByDisplayName,
            SentAt = DateTime.UtcNow
        };

        _db.ComplaintResponses.Add(response);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResponses), new { id }, MapResponseToDto(response));
    }

    // ── Action Items ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/action-items")]
    public async Task<ActionResult<List<object>>> GetActionItems(Guid id)
    {
        var exists = await _db.CustomerComplaints.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound();

        var items = await _db.ActionItems
            .Where(a => a.SourceType == ActionItemSourceType.CustomerComplaint && a.SourceEntityId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                a.AssignedToUserId,
                a.DueDate,
                a.CreatedAt
            })
            .ToListAsync();

        return items.Cast<object>().ToList();
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<ComplaintDashboardDto>> GetDashboard()
    {
        var all = await _db.CustomerComplaints
            .Include(c => c.Investigations)
            .Include(c => c.Responses)
            .ToListAsync();

        var open = all.Where(c => c.Status != ComplaintStatus.Closed).ToList();
        var overdue = open.Where(c => c.ResponseDueDate.HasValue && c.ResponseDueDate < DateTime.UtcNow).ToList();
        var closed = all.Where(c => c.Status == ComplaintStatus.Closed && c.ClosedAt.HasValue).ToList();

        var avgDaysToClose = closed.Any()
            ? (int)closed.Average(c => (c.ClosedAt!.Value - c.CreatedAt).TotalDays)
            : 0;

        var withSatisfaction = all.Where(c => c.CustomerSatisfied.HasValue).ToList();
        var satisfactionRate = withSatisfaction.Any()
            ? Math.Round((decimal)withSatisfaction.Count(c => c.CustomerSatisfied == true) / withSatisfaction.Count * 100, 1)
            : 0m;

        var byStatus = all.GroupBy(c => c.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byCategory = all.GroupBy(c => c.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var bySeverity = all.GroupBy(c => c.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var recent = all
            .OrderByDescending(c => c.CreatedAt)
            .Take(10)
            .Select(MapToSummaryDto)
            .ToList();

        return new ComplaintDashboardDto(
            TotalOpen: open.Count,
            TotalOverdue: overdue.Count,
            AvgDaysToClose: avgDaysToClose,
            CustomerSatisfactionRate: satisfactionRate,
            ByStatus: byStatus,
            ByCategory: byCategory,
            BySeverity: bySeverity,
            RecentComplaints: recent);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValidTransition(ComplaintStatus current, ComplaintStatus target)
    {
        return (current, target) switch
        {
            (ComplaintStatus.New, ComplaintStatus.UnderInvestigation) => true,
            (ComplaintStatus.UnderInvestigation, ComplaintStatus.ContainmentInPlace) => true,
            (ComplaintStatus.UnderInvestigation, ComplaintStatus.RootCauseIdentified) => true,
            (ComplaintStatus.ContainmentInPlace, ComplaintStatus.RootCauseIdentified) => true,
            (ComplaintStatus.RootCauseIdentified, ComplaintStatus.CorrectiveActionImplemented) => true,
            (ComplaintStatus.CorrectiveActionImplemented, ComplaintStatus.ResponseSent) => true,
            (ComplaintStatus.ResponseSent, ComplaintStatus.Closed) => true,
            // Allow closing from any open state for administrative closure
            (not ComplaintStatus.Closed, ComplaintStatus.Closed) => true,
            _ => false
        };
    }

    private static CustomerComplaintResponseDto MapToDto(CustomerComplaint c, int actionItemCount) => new(
        c.Id, c.Code, c.CustomerName, c.CustomerReference, c.ProductKindId,
        c.LotNumber, c.ComplaintDate, c.ReceivedDate,
        c.Category.ToString(), c.Severity.ToString(), c.Description,
        c.QuantityAffected, c.Status.ToString(), c.OwnerUserId, c.OwnerDisplayName,
        c.ResponseDueDate, c.ResponseSentAt, c.CustomerSatisfied,
        c.LinkedNonConformanceId, c.LinkedCapaId, c.LinkedSupplierId,
        c.ClosedAt, c.Investigations.Count, c.Responses.Count, actionItemCount,
        c.CreatedAt, c.UpdatedAt);

    private static CustomerComplaintSummaryDto MapToSummaryDto(CustomerComplaint c) => new(
        c.Id, c.Code, c.CustomerName, c.Category.ToString(), c.Severity.ToString(),
        c.Status.ToString(), c.Description, c.OwnerDisplayName,
        c.ResponseDueDate, c.ClosedAt, c.Investigations.Count, c.Responses.Count,
        c.CreatedAt);

    private static ComplaintInvestigationResponseDto MapInvestigationToDto(ComplaintInvestigation i) => new(
        i.Id, i.CustomerComplaintId, i.InvestigationType.ToString(),
        i.Findings, i.InvestigatedByUserId, i.InvestigatedByDisplayName,
        i.InvestigatedAt, i.CreatedAt);

    private static ComplaintResponseResponseDto MapResponseToDto(ComplaintResponse r) => new(
        r.Id, r.CustomerComplaintId, r.ResponseType.ToString(),
        r.Content, r.SentByUserId, r.SentByDisplayName,
        r.SentAt, r.CreatedAt);
}
