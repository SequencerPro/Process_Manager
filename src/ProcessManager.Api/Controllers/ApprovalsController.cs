using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ApprovalsController(ProcessManagerDbContext db) => _db = db;

    /// <summary>
    /// Returns approval records, optionally filtered by entity type and/or decision status.
    /// Use decision=Pending to get the approval queue.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ApprovalRecordResponseDto>>> GetAll(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? decision = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.ApprovalRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        if (!string.IsNullOrWhiteSpace(decision))
            query = query.Where(a => a.Decision == decision);

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<ApprovalRecordResponseDto>(
            records.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalRecordResponseDto>> GetById(Guid id)
    {
        var record = await _db.ApprovalRecords.FindAsync(id);
        if (record is null) return NotFound();
        return MapToDto(record);
    }

    private static ApprovalRecordResponseDto MapToDto(Domain.Entities.ApprovalRecord a) => new(
        a.Id, a.EntityType, a.EntityId, a.EntityVersion,
        a.SubmittedBy, a.SubmittedAt,
        a.ReviewedBy, a.ReviewedAt,
        a.Decision, a.Notes,
        a.CreatedAt, a.UpdatedAt
    );
}
