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
[Route("api/competency")]
public class CompetencyController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public CompetencyController(ProcessManagerDbContext db) => _db = db;

    // ───── List ─────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CompetencyRecordSummaryDto>>> GetAll(
        [FromQuery] string? userId = null,
        [FromQuery] Guid? trainingProcessId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var now = DateTime.UtcNow;

        var query = _db.CompetencyRecords
            .Include(c => c.TrainingProcess)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(c => c.UserId == userId);

        if (trainingProcessId.HasValue)
            query = query.Where(c => c.TrainingProcessId == trainingProcessId.Value);

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<CompetencyStatus>(status, true, out var s))
            query = query.Where(c => c.Status == s);

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(c => c.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<CompetencyRecordSummaryDto>(
            records.Select(c => MapToSummary(c, now)).ToList(),
            totalCount, page, pageSize);
    }

    // ───── My competencies ─────

    [HttpGet("my")]
    public async Task<ActionResult<List<CompetencyRecordSummaryDto>>> GetMy()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        var records = await _db.CompetencyRecords
            .Include(c => c.TrainingProcess)
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync();

        return records.Select(c => MapToSummary(c, now)).ToList();
    }

    // ───── Single record ─────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompetencyRecordDto>> GetById(Guid id)
    {
        var rec = await _db.CompetencyRecords
            .Include(c => c.TrainingProcess)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (rec is null) return NotFound();
        return MapToDto(rec);
    }

    // ───── Create (manual record — Admin/Engineer) ─────

    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<CompetencyRecordDto>> Create(CreateCompetencyRecordDto dto)
    {
        var trainingProcess = await _db.Processes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.TrainingProcessId);

        if (trainingProcess is null)
            return BadRequest($"Training process '{dto.TrainingProcessId}' not found.");

        if (trainingProcess.ProcessRole != ProcessRole.Training)
            return BadRequest($"Process '{trainingProcess.Code}' is not a Training-role process.");

        // Supersede any existing Current records for the same user + training process
        var existing = await _db.CompetencyRecords
            .Where(c => c.UserId == dto.UserId
                     && c.TrainingProcessId == dto.TrainingProcessId
                     && c.Status == CompetencyStatus.Current)
            .ToListAsync();

        foreach (var old in existing)
            old.Status = CompetencyStatus.Superseded;

        var expiresAt = trainingProcess.CompetencyExpiryDays.HasValue
            ? dto.CompletedAt.AddDays(trainingProcess.CompetencyExpiryDays.Value)
            : (DateTime?)null;

        var record = new CompetencyRecord
        {
            UserId                   = dto.UserId,
            UserDisplayName          = dto.UserDisplayName,
            TrainingProcessId        = dto.TrainingProcessId,
            TrainingProcessVersion   = trainingProcess.Version,
            JobId                    = dto.JobId,
            InstructorUserId         = dto.InstructorUserId,
            InstructorDisplayName    = dto.InstructorDisplayName,
            CompletedAt              = dto.CompletedAt,
            ExpiresAt                = expiresAt,
            Status                   = CompetencyStatus.Current,
            Notes                    = dto.Notes
        };

        _db.CompetencyRecords.Add(record);
        await _db.SaveChangesAsync();

        var full = await _db.CompetencyRecords
            .Include(c => c.TrainingProcess)
            .FirstAsync(c => c.Id == record.Id);

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, MapToDto(full));
    }

    // ───── Delete (Admin only) ─────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rec = await _db.CompetencyRecords.FindAsync(id);
        if (rec is null) return NotFound();
        _db.CompetencyRecords.Remove(rec);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Competency Matrix ─────

    [HttpGet("matrix")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<List<CompetencyMatrixRowDto>>> GetMatrix(
        [FromQuery] Guid? subjectProcessId = null)
    {
        var now = DateTime.UtcNow;
        var soon = now.AddDays(30);

        // Determine which training processes to show as columns
        List<Guid> trainingProcessIds;

        if (subjectProcessId.HasValue)
        {
            trainingProcessIds = await _db.ProcessTrainingRequirements
                .Where(r => r.SubjectType == TrainingRequirementSubjectType.Process
                         && r.SubjectEntityId == subjectProcessId.Value)
                .Select(r => r.RequiredTrainingProcessId)
                .Distinct()
                .ToListAsync();
        }
        else
        {
            trainingProcessIds = await _db.Processes
                .Where(p => p.ProcessRole == ProcessRole.Training && p.IsActive)
                .Select(p => p.Id)
                .ToListAsync();
        }

        if (!trainingProcessIds.Any())
            return new List<CompetencyMatrixRowDto>();

        // Get all relevant competency records
        var records = await _db.CompetencyRecords
            .AsNoTracking()
            .Where(c => trainingProcessIds.Contains(c.TrainingProcessId))
            .ToListAsync();

        // Get all users who have any record
        var userIds = records.Select(c => c.UserId).Distinct().ToList();

        var rows = userIds.Select(uid =>
        {
            var displayName = records.First(r => r.UserId == uid).UserDisplayName;
            var cells = trainingProcessIds.Select(tpId =>
            {
                var best = records
                    .Where(r => r.UserId == uid && r.TrainingProcessId == tpId)
                    .OrderBy(r => r.Status == CompetencyStatus.Current ? 0
                                : r.Status == CompetencyStatus.Expired  ? 1 : 2)
                    .ThenByDescending(r => r.CompletedAt)
                    .FirstOrDefault();

                return new CompetencyMatrixCellDto(
                    tpId,
                    best?.Status.ToString(),
                    best?.CompletedAt,
                    best?.ExpiresAt,
                    best?.Status == CompetencyStatus.Current && best.ExpiresAt.HasValue && best.ExpiresAt.Value <= soon
                );
            }).ToList();

            return new CompetencyMatrixRowDto(uid, displayName, cells);
        }).ToList();

        return rows;
    }

    // ───── Training Compliance Aggregate ─────

    [HttpGet("compliance")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<TrainingComplianceSummaryDto>> GetCompliance()
    {
        var now  = DateTime.UtcNow;
        var soon = now.AddDays(30);

        var records = await _db.CompetencyRecords.AsNoTracking().ToListAsync();

        var total   = records.Count(r => r.Status != CompetencyStatus.Superseded);
        var current = records.Count(r => r.Status == CompetencyStatus.Current);
        var expired = records.Count(r => r.Status == CompetencyStatus.Expired);
        var missing = 0; // not tracked without enumerated user list — surfaced via matrix
        var expiringSoon = records.Count(r =>
            r.Status == CompetencyStatus.Current &&
            r.ExpiresAt.HasValue && r.ExpiresAt.Value <= soon);

        var pct = total > 0 ? Math.Round((double)current / total * 100, 1) : 0.0;

        return new TrainingComplianceSummaryDto(total, current, expired, missing, expiringSoon, pct);
    }

    // ───── Training Requirements ─────

    [HttpGet("requirements")]
    public async Task<ActionResult<List<ProcessTrainingRequirementDto>>> GetRequirements(
        [FromQuery] string subjectType,
        [FromQuery] Guid subjectEntityId)
    {
        if (!Enum.TryParse<TrainingRequirementSubjectType>(subjectType, true, out var st))
            return BadRequest($"Invalid subjectType: {subjectType}");

        var reqs = await _db.ProcessTrainingRequirements
            .Include(r => r.RequiredTrainingProcess)
            .AsNoTracking()
            .Where(r => r.SubjectType == st && r.SubjectEntityId == subjectEntityId)
            .OrderBy(r => r.RequiredTrainingProcess!.Code)
            .ToListAsync();

        return reqs.Select(MapRequirementToDto).ToList();
    }

    [HttpPost("requirements")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<ActionResult<ProcessTrainingRequirementDto>> AddRequirement(AddTrainingRequirementDto dto)
    {
        if (!Enum.TryParse<TrainingRequirementSubjectType>(dto.SubjectType, true, out var st))
            return BadRequest($"Invalid subjectType: {dto.SubjectType}");

        var trainingProcess = await _db.Processes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.RequiredTrainingProcessId);

        if (trainingProcess is null)
            return BadRequest($"Training process '{dto.RequiredTrainingProcessId}' not found.");

        if (trainingProcess.ProcessRole != ProcessRole.Training)
            return BadRequest($"Process '{trainingProcess.Code}' is not a Training-role process.");

        var already = await _db.ProcessTrainingRequirements.AnyAsync(
            r => r.SubjectType == st
              && r.SubjectEntityId == dto.SubjectEntityId
              && r.RequiredTrainingProcessId == dto.RequiredTrainingProcessId);

        if (already)
            return Conflict("This training requirement already exists for the subject.");

        var req = new ProcessTrainingRequirement
        {
            SubjectType                = st,
            SubjectEntityId            = dto.SubjectEntityId,
            RequiredTrainingProcessId  = dto.RequiredTrainingProcessId,
            IsEnforced                 = dto.IsEnforced
        };

        _db.ProcessTrainingRequirements.Add(req);
        await _db.SaveChangesAsync();

        var full = await _db.ProcessTrainingRequirements
            .Include(r => r.RequiredTrainingProcess)
            .FirstAsync(r => r.Id == req.Id);

        return CreatedAtAction(nameof(GetRequirements),
            new { subjectType = dto.SubjectType, subjectEntityId = dto.SubjectEntityId },
            MapRequirementToDto(full));
    }

    [HttpDelete("requirements/{id:guid}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> DeleteRequirement(Guid id)
    {
        var req = await _db.ProcessTrainingRequirements.FindAsync(id);
        if (req is null) return NotFound();
        _db.ProcessTrainingRequirements.Remove(req);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ───── Mappers ─────

    private static CompetencyRecordDto MapToDto(CompetencyRecord c) => new(
        c.Id,
        c.UserId,
        c.UserDisplayName,
        c.TrainingProcessId,
        c.TrainingProcess?.Code ?? "",
        c.TrainingProcess?.Name ?? "",
        c.TrainingProcess?.CompetencyTitle,
        c.TrainingProcessVersion,
        c.JobId,
        c.InstructorUserId,
        c.InstructorDisplayName,
        c.CompletedAt,
        c.ExpiresAt,
        c.Status.ToString(),
        c.Notes,
        c.CreatedAt,
        c.CreatedBy
    );

    private static CompetencyRecordSummaryDto MapToSummary(CompetencyRecord c, DateTime now)
    {
        var soon = now.AddDays(30);
        return new(
            c.Id,
            c.UserId,
            c.UserDisplayName,
            c.TrainingProcessId,
            c.TrainingProcess?.Code ?? "",
            c.TrainingProcess?.Name ?? "",
            c.TrainingProcess?.CompetencyTitle,
            c.TrainingProcessVersion,
            c.CompletedAt,
            c.ExpiresAt,
            c.Status.ToString(),
            c.Status == CompetencyStatus.Current && c.ExpiresAt.HasValue && c.ExpiresAt.Value <= soon
        );
    }

    private static ProcessTrainingRequirementDto MapRequirementToDto(ProcessTrainingRequirement r) => new(
        r.Id,
        r.SubjectType.ToString(),
        r.SubjectEntityId,
        r.RequiredTrainingProcessId,
        r.RequiredTrainingProcess?.Code ?? "",
        r.RequiredTrainingProcess?.Name ?? "",
        r.RequiredTrainingProcess?.CompetencyTitle,
        r.IsEnforced,
        r.CreatedAt,
        r.CreatedBy
    );
}
