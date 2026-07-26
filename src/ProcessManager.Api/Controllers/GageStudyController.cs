using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/gage-studies")]
public class GageStudyController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public GageStudyController(ProcessManagerDbContext db) => _db = db;

    // ── List ────────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GageStudySummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.GageStudies
            .Include(g => g.Equipment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GageStudyStatus>(status, true, out var s))
            query = query.Where(g => g.Status == s);

        if (equipmentId.HasValue)
            query = query.Where(g => g.EquipmentId == equipmentId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(g => new GageStudySummaryDto(
            g.Id,
            g.Name,
            g.StudyType.ToString(),
            g.Equipment?.Code,
            g.CharacteristicName,
            g.Status.ToString(),
            g.GrrPercent,
            g.Ndc,
            g.AcceptanceDecision,
            g.Measurements.Count
        )).ToList();

        // Measurement count needs separate query since lazy loading isn't used
        foreach (var dto in dtos)
        {
            // Already 0 from Include — load count separately
        }

        // Re-query with measurement counts
        var ids = items.Select(i => i.Id).ToList();
        var counts = await _db.GageStudyMeasurements
            .Where(m => ids.Contains(m.GageStudyId))
            .GroupBy(m => m.GageStudyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var finalDtos = items.Select(g => new GageStudySummaryDto(
            g.Id,
            g.Name,
            g.StudyType.ToString(),
            g.Equipment?.Code,
            g.CharacteristicName,
            g.Status.ToString(),
            g.GrrPercent,
            g.Ndc,
            g.AcceptanceDecision,
            counts.GetValueOrDefault(g.Id, 0)
        )).ToList();

        return new PaginatedResponse<GageStudySummaryDto>(finalDtos, totalCount, page, pageSize);
    }

    // ── Get by ID ───────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GageStudyResponseDto>> GetById(Guid id)
    {
        var study = await _db.GageStudies
            .Include(g => g.Equipment)
            .Include(g => g.Process)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (study is null) return NotFound();

        var measurementCount = await _db.GageStudyMeasurements.CountAsync(m => m.GageStudyId == id);
        return MapToDto(study, measurementCount);
    }

    // ── Create ──────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<GageStudyResponseDto>> Create([FromBody] CreateGageStudyDto dto)
    {
        if (!Enum.TryParse<GageStudyType>(dto.StudyType, true, out var studyType))
            return BadRequest("Invalid StudyType.");

        if (dto.EquipmentId.HasValue)
        {
            var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == dto.EquipmentId.Value);
            if (equipment is null) return BadRequest("Equipment not found.");
        }

        if (dto.ProcessId.HasValue)
        {
            var process = await _db.Processes.FirstOrDefaultAsync(p => p.Id == dto.ProcessId.Value);
            if (process is null) return BadRequest("Process not found.");
        }

        var study = new GageStudy
        {
            Name = dto.Name,
            StudyType = studyType,
            EquipmentId = dto.EquipmentId,
            ProcessId = dto.ProcessId,
            CharacteristicName = dto.CharacteristicName,
            Tolerance = dto.Tolerance,
            LSL = dto.LSL,
            USL = dto.USL,
            NumberOfParts = dto.NumberOfParts,
            NumberOfOperators = dto.NumberOfOperators,
            NumberOfTrials = dto.NumberOfTrials,
            Status = GageStudyStatus.Draft,
        };

        _db.GageStudies.Add(study);
        await _db.SaveChangesAsync();

        // Reload with navigation
        study = await _db.GageStudies
            .Include(g => g.Equipment)
            .Include(g => g.Process)
            .FirstAsync(g => g.Id == study.Id);

        return CreatedAtAction(nameof(GetById), new { id = study.Id }, MapToDto(study, 0));
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GageStudyResponseDto>> Update(Guid id, [FromBody] UpdateGageStudyDto dto)
    {
        var study = await _db.GageStudies
            .Include(g => g.Equipment)
            .Include(g => g.Process)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (study is null) return NotFound();

        if (study.Status == GageStudyStatus.Complete)
            return BadRequest("Cannot update a completed study.");

        study.Name = dto.Name;
        study.CharacteristicName = dto.CharacteristicName;
        study.Tolerance = dto.Tolerance;
        study.LSL = dto.LSL;
        study.USL = dto.USL;

        await _db.SaveChangesAsync();

        var measurementCount = await _db.GageStudyMeasurements.CountAsync(m => m.GageStudyId == id);
        return MapToDto(study, measurementCount);
    }

    // ── Delete ──────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var study = await _db.GageStudies.FirstOrDefaultAsync(g => g.Id == id);
        if (study is null) return NotFound();

        _db.GageStudies.Remove(study);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Measurements: List ──────────────────────────────────────────────────────

    [HttpGet("{id:guid}/measurements")]
    public async Task<ActionResult<List<GageStudyMeasurementDto>>> GetMeasurements(Guid id)
    {
        var study = await _db.GageStudies.FirstOrDefaultAsync(g => g.Id == id);
        if (study is null) return NotFound();

        var measurements = await _db.GageStudyMeasurements
            .Where(m => m.GageStudyId == id)
            .OrderBy(m => m.PartNumber).ThenBy(m => m.OperatorId).ThenBy(m => m.TrialNumber)
            .ToListAsync();

        return measurements.Select(m => new GageStudyMeasurementDto(
            m.Id, m.GageStudyId, m.PartNumber, m.OperatorId, m.TrialNumber, m.MeasuredValue
        )).ToList();
    }

    // ── Measurements: Add Bulk ──────────────────────────────────────────────────

    [HttpPost("{id:guid}/measurements")]
    public async Task<ActionResult<List<GageStudyMeasurementDto>>> AddMeasurements(Guid id, [FromBody] AddGageStudyMeasurementsDto dto)
    {
        var study = await _db.GageStudies.FirstOrDefaultAsync(g => g.Id == id);
        if (study is null) return NotFound();

        if (study.Status == GageStudyStatus.Complete)
            return BadRequest("Cannot add measurements to a completed study.");

        if (!dto.Measurements.Any())
            return BadRequest("At least one measurement is required.");

        // Validate bounds
        foreach (var m in dto.Measurements)
        {
            if (m.PartNumber > study.NumberOfParts)
                return BadRequest($"PartNumber {m.PartNumber} exceeds study's NumberOfParts ({study.NumberOfParts}).");
            if (m.TrialNumber > study.NumberOfTrials)
                return BadRequest($"TrialNumber {m.TrialNumber} exceeds study's NumberOfTrials ({study.NumberOfTrials}).");
        }

        // Transition to InProgress if Draft
        if (study.Status == GageStudyStatus.Draft)
            study.Status = GageStudyStatus.InProgress;

        var entities = dto.Measurements.Select(m => new GageStudyMeasurement
        {
            GageStudyId = id,
            PartNumber = m.PartNumber,
            OperatorId = m.OperatorId,
            TrialNumber = m.TrialNumber,
            MeasuredValue = m.MeasuredValue,
        }).ToList();

        _db.GageStudyMeasurements.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(m => new GageStudyMeasurementDto(
            m.Id, m.GageStudyId, m.PartNumber, m.OperatorId, m.TrialNumber, m.MeasuredValue
        )).ToList();
    }

    // ── Calculate GRR ───────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/calculate")]
    public async Task<ActionResult<GrrCalculationResultDto>> Calculate(Guid id)
    {
        var study = await _db.GageStudies.FirstOrDefaultAsync(g => g.Id == id);
        if (study is null) return NotFound();

        var measurements = await _db.GageStudyMeasurements
            .Where(m => m.GageStudyId == id)
            .ToListAsync();

        var expected = study.NumberOfParts * study.NumberOfOperators * study.NumberOfTrials;
        if (measurements.Count < expected)
            return BadRequest($"Insufficient data: have {measurements.Count} measurements, need {expected}.");

        var data = measurements.Select(m => (m.PartNumber, m.OperatorId, m.TrialNumber, m.MeasuredValue)).ToList();

        var result = GrrCalculationService.Calculate(
            data, study.NumberOfParts, study.NumberOfOperators, study.NumberOfTrials, study.Tolerance);

        if (result is null)
            return BadRequest("Calculation failed — check study parameters.");

        // Store results on the study
        study.GrrPercent = result.PercentGRR;
        study.Ndc = result.Ndc;
        study.AcceptanceDecision = result.Assessment;
        study.Status = GageStudyStatus.Complete;

        await _db.SaveChangesAsync();

        return result;
    }

    // ── Dashboard ───────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<GageStudyDashboardDto>> GetDashboard()
    {
        var studies = await _db.GageStudies
            .Include(g => g.Equipment)
            .ToListAsync();

        var total = studies.Count;
        var complete = studies.Count(s => s.Status == GageStudyStatus.Complete);
        var inProgress = studies.Count(s => s.Status == GageStudyStatus.InProgress);
        var draft = studies.Count(s => s.Status == GageStudyStatus.Draft);

        var completedStudies = studies.Where(s => s.Status == GageStudyStatus.Complete && s.GrrPercent.HasValue).ToList();

        var acceptable = completedStudies.Count(s => s.GrrPercent < 10);
        var marginal = completedStudies.Count(s => s.GrrPercent >= 10 && s.GrrPercent < 30);
        var unacceptable = completedStudies.Count(s => s.GrrPercent >= 30);

        var worstStudies = completedStudies
            .OrderByDescending(s => s.GrrPercent)
            .Take(5)
            .Select(s => new GageStudySummaryDto(
                s.Id, s.Name, s.StudyType.ToString(), s.Equipment?.Code,
                s.CharacteristicName, s.Status.ToString(), s.GrrPercent, s.Ndc, s.AcceptanceDecision, 0))
            .ToList();

        return new GageStudyDashboardDto(
            total, complete, inProgress, draft,
            acceptable, marginal, unacceptable,
            worstStudies);
    }

    // ── Mapping helper ──────────────────────────────────────────────────────────

    private static GageStudyResponseDto MapToDto(GageStudy g, int measurementCount) => new(
        g.Id,
        g.Name,
        g.StudyType.ToString(),
        g.EquipmentId,
        g.Equipment?.Code,
        g.Equipment?.Name,
        g.ProcessId,
        g.Process?.Name,
        g.CharacteristicName,
        g.Tolerance,
        g.LSL,
        g.USL,
        g.NumberOfParts,
        g.NumberOfOperators,
        g.NumberOfTrials,
        g.Status.ToString(),
        g.GrrPercent,
        g.Ndc,
        g.AcceptanceDecision,
        measurementCount,
        g.CreatedAt,
        g.UpdatedAt);
}

