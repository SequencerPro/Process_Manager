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
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public SuppliersController(ProcessManagerDbContext db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SupplierSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Suppliers
            .Include(s => s.Evaluations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(sup => sup.Code.ToLower().Contains(s)
                                    || sup.Name.ToLower().Contains(s)
                                    || (sup.ContactName != null && sup.ContactName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SupplierStatus>(status, true, out var st))
            query = query.Where(sup => sup.Status == st);

        if (active.HasValue)
            query = query.Where(sup => sup.IsActive == active.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var supplierIds = items.Select(s => s.Id).ToList();
        var mrbCounts = await GetSupplierMrbCounts(supplierIds);

        var dtos = items.Select(s =>
        {
            var latest = s.Evaluations.OrderByDescending(e => e.EvaluationDate).FirstOrDefault();
            mrbCounts.TryGetValue(s.Id, out var ncCount);
            return new SupplierSummaryDto(
                s.Id, s.Code, s.Name, s.Status.ToString(), s.IsActive,
                s.Evaluations.Count, latest?.OverallScore, ncCount);
        }).ToList();

        return new PaginatedResponse<SupplierSummaryDto>(dtos, totalCount, page, pageSize);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierResponseDto>> GetById(Guid id)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.Evaluations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return NotFound();

        return MapToDto(supplier, 0, 0);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<SupplierResponseDto>> Create(CreateSupplierDto dto)
    {
        if (await _db.Suppliers.AnyAsync(s => s.Code == dto.Code.Trim()))
            return Conflict($"A supplier with code '{dto.Code}' already exists.");

        var supplier = new Supplier
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ContactName = dto.ContactName?.Trim(),
            ContactEmail = dto.ContactEmail?.Trim(),
            ContactPhone = dto.ContactPhone?.Trim(),
            Address = dto.Address?.Trim(),
            Notes = dto.Notes?.Trim()
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, MapToDto(supplier, 0, 0));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierResponseDto>> Update(Guid id, UpdateSupplierDto dto)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.Evaluations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return NotFound();

        supplier.Name = dto.Name.Trim();
        supplier.ContactName = dto.ContactName?.Trim();
        supplier.ContactEmail = dto.ContactEmail?.Trim();
        supplier.ContactPhone = dto.ContactPhone?.Trim();
        supplier.Address = dto.Address?.Trim();
        supplier.Notes = dto.Notes?.Trim();
        supplier.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return MapToDto(supplier, 0, 0);
    }

    // ── Status Transition ─────────────────────────────────────────────────────

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<SupplierResponseDto>> UpdateStatus(Guid id, UpdateSupplierStatusDto dto)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.Evaluations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return NotFound();

        if (!Enum.TryParse<SupplierStatus>(dto.Status, true, out var newStatus))
            return BadRequest($"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<SupplierStatus>())}");

        var valid = IsValidTransition(supplier.Status, newStatus);
        if (!valid)
            return BadRequest($"Cannot transition from '{supplier.Status}' to '{newStatus}'.");

        supplier.Status = newStatus;

        if (newStatus == SupplierStatus.Approved && !supplier.ApprovedDate.HasValue)
            supplier.ApprovedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
            supplier.Notes = dto.Notes.Trim();

        await _db.SaveChangesAsync();
        return MapToDto(supplier, 0, 0);
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();

        supplier.IsActive = false;
        supplier.Status = SupplierStatus.Inactive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Evaluations ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/evaluations")]
    public async Task<ActionResult<List<SupplierEvaluationResponseDto>>> GetEvaluations(Guid id)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == id))
            return NotFound();

        var evals = await _db.SupplierEvaluations
            .Where(e => e.SupplierId == id)
            .OrderByDescending(e => e.EvaluationDate)
            .ToListAsync();

        var userIds = evals.Where(e => e.EvaluatedByUserId != null).Select(e => e.EvaluatedByUserId!).Distinct().ToList();
        var userMap = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? u.UserName ?? u.Id);

        return evals.Select(e => MapEvalToDto(e, userMap)).ToList();
    }

    [HttpPost("{id:guid}/evaluations")]
    public async Task<ActionResult<SupplierEvaluationResponseDto>> AddEvaluation(Guid id, CreateSupplierEvaluationDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();

        if (dto.QualityScore < 0 || dto.QualityScore > 100 ||
            dto.DeliveryScore < 0 || dto.DeliveryScore > 100 ||
            dto.ResponsivenessScore < 0 || dto.ResponsivenessScore > 100)
            return BadRequest("Scores must be between 0 and 100.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var overallScore = (dto.QualityScore + dto.DeliveryScore + dto.ResponsivenessScore) / 3;

        var eval = new SupplierEvaluation
        {
            SupplierId = id,
            EvaluationDate = dto.EvaluationDate.ToUniversalTime(),
            QualityScore = dto.QualityScore,
            DeliveryScore = dto.DeliveryScore,
            ResponsivenessScore = dto.ResponsivenessScore,
            OverallScore = overallScore,
            Notes = dto.Notes?.Trim(),
            EvaluatedByUserId = userId
        };

        _db.SupplierEvaluations.Add(eval);

        supplier.LastEvaluationDate = eval.EvaluationDate;

        await _db.SaveChangesAsync();

        var displayName = userId != null
            ? (await _db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName ?? u.UserName).FirstOrDefaultAsync()) ?? userId
            : null;

        return CreatedAtAction(nameof(GetEvaluations), new { id }, MapEvalToDto(eval, displayName != null && userId != null
            ? new Dictionary<string, string> { { userId, displayName } }
            : new Dictionary<string, string>()));
    }

    [HttpDelete("{supplierId:guid}/evaluations/{evalId:guid}")]
    public async Task<IActionResult> DeleteEvaluation(Guid supplierId, Guid evalId)
    {
        var eval = await _db.SupplierEvaluations
            .FirstOrDefaultAsync(e => e.Id == evalId && e.SupplierId == supplierId);

        if (eval is null) return NotFound();

        _db.SupplierEvaluations.Remove(eval);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Quality Dashboard ─────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<SupplierQualityDashboardDto>> GetDashboard()
    {
        var suppliers = await _db.Suppliers
            .Include(s => s.Evaluations)
            .Where(s => s.IsActive)
            .ToListAsync();

        var approved = suppliers.Count(s => s.Status == SupplierStatus.Approved);
        var conditional = suppliers.Count(s => s.Status == SupplierStatus.Conditional);
        var suspended = suppliers.Count(s => s.Status == SupplierStatus.Suspended);

        var supplierMrbCounts = await GetSupplierMrbCounts(suppliers.Select(s => s.Id).ToList());

        var withNcs = supplierMrbCounts.Count(kvp => kvp.Value > 0);
        var openMrbCount = await _db.MrbReviews
            .Where(m => m.SupplierCaused && (m.Status == MrbStatus.Draft || m.Status == MrbStatus.UnderReview))
            .CountAsync();

        var scored = suppliers
            .Where(s => s.Evaluations.Any())
            .Select(s => new
            {
                Supplier = s,
                LatestScore = s.Evaluations.OrderByDescending(e => e.EvaluationDate).First().OverallScore
            })
            .ToList();

        var avgScore = scored.Any() ? scored.Average(s => s.LatestScore) : 0;

        var topPerformers = scored
            .OrderByDescending(s => s.LatestScore)
            .Take(5)
            .Select(s =>
            {
                supplierMrbCounts.TryGetValue(s.Supplier.Id, out var ncCount);
                return new SupplierSummaryDto(
                    s.Supplier.Id, s.Supplier.Code, s.Supplier.Name, s.Supplier.Status.ToString(),
                    s.Supplier.IsActive, s.Supplier.Evaluations.Count, s.LatestScore, ncCount);
            }).ToList();

        var atRisk = scored
            .Where(s => s.LatestScore < 60 || s.Supplier.Status == SupplierStatus.Conditional || s.Supplier.Status == SupplierStatus.Suspended)
            .OrderBy(s => s.LatestScore)
            .Take(5)
            .Select(s =>
            {
                supplierMrbCounts.TryGetValue(s.Supplier.Id, out var ncCount);
                return new SupplierSummaryDto(
                    s.Supplier.Id, s.Supplier.Code, s.Supplier.Name, s.Supplier.Status.ToString(),
                    s.Supplier.IsActive, s.Supplier.Evaluations.Count, s.LatestScore, ncCount);
            }).ToList();

        // Include unscoredConditional/suspended suppliers in at-risk
        var unscoredAtRisk = suppliers
            .Where(s => !s.Evaluations.Any() && (s.Status == SupplierStatus.Conditional || s.Status == SupplierStatus.Suspended))
            .Select(s =>
            {
                supplierMrbCounts.TryGetValue(s.Id, out var ncCount);
                return new SupplierSummaryDto(
                    s.Id, s.Code, s.Name, s.Status.ToString(), s.IsActive, 0, null, ncCount);
            }).ToList();

        atRisk.AddRange(unscoredAtRisk);

        return new SupplierQualityDashboardDto(
            suppliers.Count, approved, conditional, suspended,
            withNcs, openMrbCount, Math.Round(avgScore, 1),
            topPerformers, atRisk.Take(5).ToList());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IsValidTransition(SupplierStatus from, SupplierStatus to)
    {
        return (from, to) switch
        {
            (SupplierStatus.Pending, SupplierStatus.Approved) => true,
            (SupplierStatus.Pending, SupplierStatus.Conditional) => true,
            (SupplierStatus.Pending, SupplierStatus.Inactive) => true,
            (SupplierStatus.Approved, SupplierStatus.Conditional) => true,
            (SupplierStatus.Approved, SupplierStatus.Suspended) => true,
            (SupplierStatus.Approved, SupplierStatus.Inactive) => true,
            (SupplierStatus.Conditional, SupplierStatus.Approved) => true,
            (SupplierStatus.Conditional, SupplierStatus.Suspended) => true,
            (SupplierStatus.Conditional, SupplierStatus.Inactive) => true,
            (SupplierStatus.Suspended, SupplierStatus.Conditional) => true,
            (SupplierStatus.Suspended, SupplierStatus.Approved) => true,
            (SupplierStatus.Suspended, SupplierStatus.Inactive) => true,
            (SupplierStatus.Inactive, SupplierStatus.Pending) => true,
            _ => false
        };
    }

    private async Task<Dictionary<Guid, int>> GetSupplierMrbCounts(List<Guid> supplierIds)
    {
        // No direct FK from NC/MRB to Supplier yet — return empty counts
        // Future: link NonConformance.SupplierId or Kind.SupplierId for proper tracking
        return supplierIds.ToDictionary(id => id, _ => 0);
    }

    private static SupplierResponseDto MapToDto(Supplier s, int ncCount, int mrbCount)
    {
        var latest = s.Evaluations?.OrderByDescending(e => e.EvaluationDate).FirstOrDefault();
        return new SupplierResponseDto(
            s.Id, s.Code, s.Name, s.Status.ToString(),
            s.ContactName, s.ContactEmail, s.ContactPhone,
            s.Address, s.Notes, s.ApprovedDate, s.LastEvaluationDate,
            s.IsActive, s.Evaluations?.Count ?? 0, latest?.OverallScore,
            ncCount, mrbCount,
            s.CreatedAt, s.UpdatedAt);
    }

    private static SupplierEvaluationResponseDto MapEvalToDto(SupplierEvaluation e, Dictionary<string, string> userMap)
    {
        string? displayName = null;
        if (e.EvaluatedByUserId != null)
            userMap.TryGetValue(e.EvaluatedByUserId, out displayName);

        return new SupplierEvaluationResponseDto(
            e.Id, e.SupplierId, e.EvaluationDate,
            e.QualityScore, e.DeliveryScore, e.ResponsivenessScore, e.OverallScore,
            e.Notes, e.EvaluatedByUserId, displayName,
            e.CreatedAt);
    }
}
