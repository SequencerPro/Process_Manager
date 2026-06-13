using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Phase 50: Strategy Management — Balanced Scorecard. Scorecards hold perspectives,
/// perspectives hold strategic objectives, objectives carry measures (with live data
/// sources) and link to the operational Processes/Workflows that realize them.
/// </summary>
[Authorize(Roles = "Admin,Engineer")]
[ApiController]
[Route("api/scorecards")]
public class ScorecardsController : ControllerBase
{
    /// <summary>The Kaplan-Norton default perspectives seeded on scorecard creation.</summary>
    private static readonly (string Name, string Description)[] DefaultPerspectives =
    [
        ("Financial", "How do we look to shareholders?"),
        ("Customer", "How do customers see us?"),
        ("Internal Business Process", "What must we excel at?"),
        ("Learning & Growth", "Can we continue to improve and create value?"),
    ];

    private readonly ProcessManagerDbContext _db;
    private readonly IMeasureValueResolver _resolver;
    private readonly ITenantContext _tenantContext;

    public ScorecardsController(ProcessManagerDbContext db, IMeasureValueResolver resolver, ITenantContext tenantContext)
    {
        _db = db;
        _resolver = resolver;
        _tenantContext = tenantContext;
    }

    // ── Scorecards ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ScorecardSummaryDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Scorecards
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.Measures)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ScorecardStatus>(status, true, out var st))
            query = query.Where(s => s.Status == st);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(s) || c.Name.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(s => new ScorecardSummaryDto(
            s.Id, s.Code, s.Name, s.Status.ToString(), s.OwnerOrgUnitId,
            s.Perspectives.Count,
            s.Perspectives.Sum(p => p.Objectives.Count),
            s.Perspectives.Sum(p => p.Objectives.Sum(o => o.Measures.Count)),
            s.CreatedAt)).ToList();

        return new PaginatedResponse<ScorecardSummaryDto>(dtos, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScorecardResponseDto>> GetById(Guid id)
    {
        var scorecard = await LoadScorecardTree(id);
        if (scorecard is null) return NotFound();

        return await MapScorecardAsync(scorecard, resolveValues: true);
    }

    [HttpPost]
    public async Task<ActionResult<ScorecardResponseDto>> Create(CreateScorecardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        if (dto.OwnerOrgUnitId.HasValue &&
            !await _db.OrgUnits.AnyAsync(o => o.Id == dto.OwnerOrgUnitId))
            return BadRequest("Owner org unit does not exist.");

        var scorecard = new Scorecard
        {
            Code = await NextCode("BSC", _db.Scorecards.Select(s => s.Code)),
            Name = dto.Name,
            MissionStatement = dto.MissionStatement,
            VisionStatement = dto.VisionStatement,
            StrategyNotes = dto.StrategyNotes,
            OwnerOrgUnitId = dto.OwnerOrgUnitId,
            Status = ScorecardStatus.Draft,
        };

        for (var i = 0; i < DefaultPerspectives.Length; i++)
        {
            scorecard.Perspectives.Add(new ScorecardPerspective
            {
                Name = DefaultPerspectives[i].Name,
                Description = DefaultPerspectives[i].Description,
                SortOrder = i,
            });
        }

        _db.Scorecards.Add(scorecard);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = scorecard.Id },
            await MapScorecardAsync(scorecard, resolveValues: false));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ScorecardResponseDto>> Update(Guid id, UpdateScorecardDto dto)
    {
        var scorecard = await LoadScorecardTree(id);
        if (scorecard is null) return NotFound();

        if (dto.Name is not null) scorecard.Name = dto.Name;
        if (dto.MissionStatement is not null) scorecard.MissionStatement = dto.MissionStatement;
        if (dto.VisionStatement is not null) scorecard.VisionStatement = dto.VisionStatement;
        if (dto.StrategyNotes is not null) scorecard.StrategyNotes = dto.StrategyNotes;
        if (dto.OwnerOrgUnitId.HasValue) scorecard.OwnerOrgUnitId = dto.OwnerOrgUnitId;
        if (dto.Status is not null)
        {
            if (!Enum.TryParse<ScorecardStatus>(dto.Status, true, out var st))
                return BadRequest($"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<ScorecardStatus>())}");
            scorecard.Status = st;
        }

        await _db.SaveChangesAsync();
        return await MapScorecardAsync(scorecard, resolveValues: false);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var scorecard = await _db.Scorecards.FindAsync(id);
        if (scorecard is null) return NotFound();

        if (scorecard.Status == ScorecardStatus.Active)
            return BadRequest("Cannot delete an active scorecard. Archive it first.");

        _db.Scorecards.Remove(scorecard);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Seeds the full-capability example scorecard (demo process with execution data, live and
    /// manual measures, snapshot history, initiatives, process links, cause-effect chain).
    /// Idempotent per tenant — re-running returns the existing demo scorecard.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("seed-demo")]
    public async Task<ActionResult<ScorecardResponseDto>> SeedDemo()
    {
        var scorecardId = await DataSeeder.SeedBalancedScorecardDemoAsync(_db, _tenantContext.CurrentTenantId);

        var scorecard = await LoadScorecardTree(scorecardId);
        return await MapScorecardAsync(scorecard!, resolveValues: false);
    }

    /// <summary>
    /// Numeric data-collection prompts a PromptMetric measure can read from — the measure
    /// averages their responses over a rolling window.
    /// </summary>
    [HttpGet("prompt-metrics")]
    public async Task<ActionResult<List<PromptMetricOptionDto>>> GetPromptMetricOptions()
    {
        return await _db.StepTemplateContents
            .Where(c => c.ContentType == StepContentType.Prompt
                     && c.PromptType == PromptType.NumericEntry
                     && c.Label != null)
            .OrderBy(c => c.StepTemplate.Name).ThenBy(c => c.SortOrder)
            .Select(c => new PromptMetricOptionDto(c.Id, c.Label!, c.StepTemplate.Name, c.Units))
            .ToListAsync();
    }

    // ── Perspectives ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/perspectives")]
    public async Task<ActionResult<PerspectiveResponseDto>> AddPerspective(Guid id, CreatePerspectiveDto dto)
    {
        var scorecard = await _db.Scorecards.FindAsync(id);
        if (scorecard is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var perspective = new ScorecardPerspective
        {
            ScorecardId = id,
            Name = dto.Name,
            Description = dto.Description,
            SortOrder = dto.SortOrder,
        };

        _db.ScorecardPerspectives.Add(perspective);
        await _db.SaveChangesAsync();

        return await MapPerspectiveAsync(perspective, resolveValues: false);
    }

    [HttpPut("perspectives/{perspectiveId:guid}")]
    public async Task<ActionResult<PerspectiveResponseDto>> UpdatePerspective(Guid perspectiveId, UpdatePerspectiveDto dto)
    {
        var perspective = await _db.ScorecardPerspectives
            .Include(p => p.Objectives).ThenInclude(o => o.Measures).ThenInclude(m => m.Snapshots)
            .Include(p => p.Objectives).ThenInclude(o => o.ProcessLinks)
            .FirstOrDefaultAsync(p => p.Id == perspectiveId);
        if (perspective is null) return NotFound();

        if (dto.Name is not null) perspective.Name = dto.Name;
        if (dto.Description is not null) perspective.Description = dto.Description;
        if (dto.SortOrder.HasValue) perspective.SortOrder = dto.SortOrder.Value;

        await _db.SaveChangesAsync();
        return await MapPerspectiveAsync(perspective, resolveValues: false);
    }

    [HttpDelete("perspectives/{perspectiveId:guid}")]
    public async Task<IActionResult> DeletePerspective(Guid perspectiveId)
    {
        var perspective = await _db.ScorecardPerspectives
            .Include(p => p.Objectives)
            .FirstOrDefaultAsync(p => p.Id == perspectiveId);
        if (perspective is null) return NotFound();

        var objectiveIds = perspective.Objectives.Select(o => o.Id).ToList();
        await RemoveCauseLinksReferencing(objectiveIds);

        _db.ScorecardPerspectives.Remove(perspective);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Objectives ────────────────────────────────────────────────────────────

    [HttpPost("perspectives/{perspectiveId:guid}/objectives")]
    public async Task<ActionResult<ObjectiveResponseDto>> AddObjective(Guid perspectiveId, CreateObjectiveDto dto)
    {
        var perspective = await _db.ScorecardPerspectives.FindAsync(perspectiveId);
        if (perspective is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        if (dto.OwnerOrgUnitId.HasValue &&
            !await _db.OrgUnits.AnyAsync(o => o.Id == dto.OwnerOrgUnitId))
            return BadRequest("Owner org unit does not exist.");

        var objective = new StrategicObjective
        {
            PerspectiveId = perspectiveId,
            Code = await NextCode("OBJ", _db.StrategicObjectives.Select(o => o.Code)),
            Name = dto.Name,
            Description = dto.Description,
            OwnerOrgUnitId = dto.OwnerOrgUnitId,
            TargetDate = dto.TargetDate,
            SortOrder = dto.SortOrder,
        };

        _db.StrategicObjectives.Add(objective);
        await _db.SaveChangesAsync();

        return await MapObjectiveAsync(objective, resolveValues: false);
    }

    [HttpPut("objectives/{objectiveId:guid}")]
    public async Task<ActionResult<ObjectiveResponseDto>> UpdateObjective(Guid objectiveId, UpdateObjectiveDto dto)
    {
        var objective = await _db.StrategicObjectives
            .Include(o => o.Measures).ThenInclude(m => m.Snapshots)
            .Include(o => o.ProcessLinks)
            .FirstOrDefaultAsync(o => o.Id == objectiveId);
        if (objective is null) return NotFound();

        if (dto.Name is not null) objective.Name = dto.Name;
        if (dto.Description is not null) objective.Description = dto.Description;
        if (dto.OwnerOrgUnitId.HasValue) objective.OwnerOrgUnitId = dto.OwnerOrgUnitId;
        if (dto.TargetDate.HasValue) objective.TargetDate = dto.TargetDate;
        if (dto.SortOrder.HasValue) objective.SortOrder = dto.SortOrder.Value;
        if (dto.Status is not null)
        {
            if (!Enum.TryParse<StrategicObjectiveStatus>(dto.Status, true, out var st))
                return BadRequest($"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<StrategicObjectiveStatus>())}");
            objective.Status = st;
        }

        await _db.SaveChangesAsync();
        return await MapObjectiveAsync(objective, resolveValues: false);
    }

    [HttpDelete("objectives/{objectiveId:guid}")]
    public async Task<IActionResult> DeleteObjective(Guid objectiveId)
    {
        var objective = await _db.StrategicObjectives.FindAsync(objectiveId);
        if (objective is null) return NotFound();

        await RemoveCauseLinksReferencing([objectiveId]);

        _db.StrategicObjectives.Remove(objective);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Measures ──────────────────────────────────────────────────────────────

    [HttpPost("objectives/{objectiveId:guid}/measures")]
    public async Task<ActionResult<MeasureResponseDto>> AddMeasure(Guid objectiveId, CreateMeasureDto dto)
    {
        var objective = await _db.StrategicObjectives.FindAsync(objectiveId);
        if (objective is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        if (!Enum.TryParse<MeasureDirection>(dto.Direction, true, out var direction))
            return BadRequest($"Invalid direction '{dto.Direction}'. Valid values: {string.Join(", ", Enum.GetNames<MeasureDirection>())}");

        if (!Enum.TryParse<MeasureSourceType>(dto.SourceType, true, out var sourceType))
            return BadRequest($"Invalid source type '{dto.SourceType}'. Valid values: {string.Join(", ", Enum.GetNames<MeasureSourceType>())}");

        var sourceError = await ValidateSource(sourceType, dto.SourceEntityId);
        if (sourceError is not null) return BadRequest(sourceError);

        var measure = new ObjectiveMeasure
        {
            ObjectiveId = objectiveId,
            Name = dto.Name,
            Units = dto.Units,
            Direction = direction,
            BaselineValue = dto.BaselineValue,
            TargetValue = dto.TargetValue,
            GreenThreshold = dto.GreenThreshold,
            RedThreshold = dto.RedThreshold,
            SourceType = sourceType,
            SourceEntityId = dto.SourceEntityId,
            SourceParameter = dto.SourceParameter,
        };

        _db.ObjectiveMeasures.Add(measure);
        await _db.SaveChangesAsync();

        return await MapMeasureAsync(measure, resolveValues: false);
    }

    [HttpPut("measures/{measureId:guid}")]
    public async Task<ActionResult<MeasureResponseDto>> UpdateMeasure(Guid measureId, UpdateMeasureDto dto)
    {
        var measure = await _db.ObjectiveMeasures
            .Include(m => m.Snapshots)
            .FirstOrDefaultAsync(m => m.Id == measureId);
        if (measure is null) return NotFound();

        if (dto.Name is not null) measure.Name = dto.Name;
        if (dto.Units is not null) measure.Units = dto.Units;
        if (dto.BaselineValue.HasValue) measure.BaselineValue = dto.BaselineValue;
        if (dto.TargetValue.HasValue) measure.TargetValue = dto.TargetValue.Value;
        if (dto.GreenThreshold.HasValue) measure.GreenThreshold = dto.GreenThreshold;
        if (dto.RedThreshold.HasValue) measure.RedThreshold = dto.RedThreshold;
        if (dto.SourceParameter is not null) measure.SourceParameter = dto.SourceParameter;

        if (dto.Direction is not null)
        {
            if (!Enum.TryParse<MeasureDirection>(dto.Direction, true, out var direction))
                return BadRequest($"Invalid direction '{dto.Direction}'.");
            measure.Direction = direction;
        }

        if (dto.SourceType is not null)
        {
            if (!Enum.TryParse<MeasureSourceType>(dto.SourceType, true, out var sourceType))
                return BadRequest($"Invalid source type '{dto.SourceType}'.");

            var sourceError = await ValidateSource(sourceType, dto.SourceEntityId ?? measure.SourceEntityId);
            if (sourceError is not null) return BadRequest(sourceError);

            measure.SourceType = sourceType;
        }

        if (dto.SourceEntityId.HasValue) measure.SourceEntityId = dto.SourceEntityId;

        await _db.SaveChangesAsync();
        return await MapMeasureAsync(measure, resolveValues: true);
    }

    [HttpDelete("measures/{measureId:guid}")]
    public async Task<IActionResult> DeleteMeasure(Guid measureId)
    {
        var measure = await _db.ObjectiveMeasures.FindAsync(measureId);
        if (measure is null) return NotFound();

        _db.ObjectiveMeasures.Remove(measure);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Snapshots ─────────────────────────────────────────────────────────────

    [HttpGet("measures/{measureId:guid}/snapshots")]
    public async Task<ActionResult<List<MeasureSnapshotResponseDto>>> GetSnapshots(Guid measureId)
    {
        if (!await _db.ObjectiveMeasures.AnyAsync(m => m.Id == measureId))
            return NotFound();

        return await _db.MeasureSnapshots
            .Where(s => s.MeasureId == measureId)
            .OrderByDescending(s => s.CapturedAt)
            .Select(s => new MeasureSnapshotResponseDto(
                s.Id, s.MeasureId, s.Value, s.CapturedAt, s.CaptureSource.ToString(), s.Note))
            .ToListAsync();
    }

    [HttpPost("measures/{measureId:guid}/snapshots")]
    public async Task<ActionResult<MeasureSnapshotResponseDto>> AddSnapshot(Guid measureId, CreateMeasureSnapshotDto dto)
    {
        var measure = await _db.ObjectiveMeasures.FindAsync(measureId);
        if (measure is null) return NotFound();

        var snapshot = new MeasureSnapshot
        {
            MeasureId = measureId,
            Value = dto.Value,
            CapturedAt = dto.CapturedAt ?? DateTime.UtcNow,
            CaptureSource = MeasureCaptureSource.Manual,
            Note = dto.Note,
        };

        _db.MeasureSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        return new MeasureSnapshotResponseDto(
            snapshot.Id, snapshot.MeasureId, snapshot.Value, snapshot.CapturedAt,
            snapshot.CaptureSource.ToString(), snapshot.Note);
    }

    // ── Cause links (strategy map edges) ─────────────────────────────────────

    [HttpPost("{id:guid}/cause-links")]
    public async Task<ActionResult<CauseLinkResponseDto>> AddCauseLink(Guid id, CreateCauseLinkDto dto)
    {
        var scorecard = await _db.Scorecards.FindAsync(id);
        if (scorecard is null) return NotFound();

        if (dto.SourceObjectiveId == dto.TargetObjectiveId)
            return BadRequest("A cause link cannot connect an objective to itself.");

        var scorecardObjectiveIds = await _db.StrategicObjectives
            .Where(o => o.Perspective.ScorecardId == id)
            .Select(o => o.Id)
            .ToListAsync();

        if (!scorecardObjectiveIds.Contains(dto.SourceObjectiveId) ||
            !scorecardObjectiveIds.Contains(dto.TargetObjectiveId))
            return BadRequest("Both objectives must belong to this scorecard.");

        if (await _db.ObjectiveCauseLinks.AnyAsync(l =>
                l.SourceObjectiveId == dto.SourceObjectiveId &&
                l.TargetObjectiveId == dto.TargetObjectiveId))
            return Conflict("A cause link between these objectives already exists.");

        var link = new ObjectiveCauseLink
        {
            ScorecardId = id,
            SourceObjectiveId = dto.SourceObjectiveId,
            TargetObjectiveId = dto.TargetObjectiveId,
            Description = dto.Description,
        };

        _db.ObjectiveCauseLinks.Add(link);
        await _db.SaveChangesAsync();

        return new CauseLinkResponseDto(link.Id, link.SourceObjectiveId, link.TargetObjectiveId, link.Description);
    }

    [HttpDelete("cause-links/{linkId:guid}")]
    public async Task<IActionResult> DeleteCauseLink(Guid linkId)
    {
        var link = await _db.ObjectiveCauseLinks.FindAsync(linkId);
        if (link is null) return NotFound();

        _db.ObjectiveCauseLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Process links ─────────────────────────────────────────────────────────

    [HttpPost("objectives/{objectiveId:guid}/process-links")]
    public async Task<ActionResult<ProcessLinkResponseDto>> AddProcessLink(Guid objectiveId, CreateProcessLinkDto dto)
    {
        var objective = await _db.StrategicObjectives.FindAsync(objectiveId);
        if (objective is null) return NotFound();

        if (dto.ProcessId.HasValue == dto.WorkflowId.HasValue)
            return BadRequest("Exactly one of ProcessId or WorkflowId must be set.");

        if (dto.ProcessId.HasValue && !await _db.Processes.AnyAsync(p => p.Id == dto.ProcessId))
            return BadRequest("Process does not exist.");

        if (dto.WorkflowId.HasValue && !await _db.Workflows.AnyAsync(w => w.Id == dto.WorkflowId))
            return BadRequest("Workflow does not exist.");

        var duplicate = await _db.ObjectiveProcessLinks.AnyAsync(l =>
            l.ObjectiveId == objectiveId &&
            (dto.ProcessId.HasValue ? l.ProcessId == dto.ProcessId : l.WorkflowId == dto.WorkflowId));
        if (duplicate)
            return Conflict("This objective is already linked to that process/workflow.");

        var link = new ObjectiveProcessLink
        {
            ObjectiveId = objectiveId,
            ProcessId = dto.ProcessId,
            WorkflowId = dto.WorkflowId,
            Note = dto.Note,
        };

        _db.ObjectiveProcessLinks.Add(link);
        await _db.SaveChangesAsync();

        return await MapProcessLinkAsync(link);
    }

    [HttpDelete("process-links/{linkId:guid}")]
    public async Task<IActionResult> DeleteProcessLink(Guid linkId)
    {
        var link = await _db.ObjectiveProcessLinks.FindAsync(linkId);
        if (link is null) return NotFound();

        _db.ObjectiveProcessLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Status (RAG rollup) ───────────────────────────────────────────────────

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<ScorecardStatusDto>> GetStatus(Guid id)
    {
        var scorecard = await LoadScorecardTree(id);
        if (scorecard is null) return NotFound();

        var perspectives = new List<PerspectiveResponseDto>();
        int green = 0, amber = 0, red = 0, noData = 0;

        foreach (var perspective in scorecard.Perspectives.OrderBy(p => p.SortOrder))
        {
            var mapped = await MapPerspectiveAsync(perspective, resolveValues: true);
            perspectives.Add(mapped);

            foreach (var measure in mapped.Objectives.SelectMany(o => o.Measures))
            {
                switch (measure.Rag)
                {
                    case "Green": green++; break;
                    case "Amber": amber++; break;
                    case "Red": red++; break;
                    default: noData++; break;
                }
            }
        }

        return new ScorecardStatusDto(
            scorecard.Id, scorecard.Code, scorecard.Name, scorecard.Status.ToString(),
            green, amber, red, noData, perspectives);
    }

    // ── Strategy map ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/strategy-map")]
    public async Task<ActionResult<StrategyMapDto>> GetStrategyMap(Guid id)
    {
        var scorecard = await LoadScorecardTree(id);
        if (scorecard is null) return NotFound();

        var nodes = new List<StrategyMapNodeDto>();
        foreach (var perspective in scorecard.Perspectives.OrderBy(p => p.SortOrder))
        {
            foreach (var objective in perspective.Objectives.OrderBy(o => o.SortOrder))
            {
                nodes.Add(new StrategyMapNodeDto(
                    objective.Id, objective.Code, objective.Name,
                    perspective.Id, perspective.Name, perspective.SortOrder,
                    objective.Status.ToString(),
                    await RollupObjectiveRag(objective)));
            }
        }

        var edges = scorecard.CauseLinks
            .Select(l => new CauseLinkResponseDto(l.Id, l.SourceObjectiveId, l.TargetObjectiveId, l.Description))
            .ToList();

        return new StrategyMapDto(scorecard.Id, scorecard.Name, nodes, edges);
    }

    // ── Dashboard widget ────────────────────────────────────────────────────

    /// <summary>
    /// Compact strategy snapshot for the home dashboard. Picks the most recently created
    /// Active scorecard, resolves its measures to a RAG rollup, and lists up to five
    /// off-track objectives (Red first, then Amber).
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ScorecardDashboardDto>> GetDashboard()
    {
        var activeCount = await _db.Scorecards.CountAsync(s => s.Status == ScorecardStatus.Active);

        var scorecard = await _db.Scorecards
            .Where(s => s.Status == ScorecardStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.Measures)
                        .ThenInclude(m => m.Snapshots)
            .FirstOrDefaultAsync();

        if (scorecard is null)
            return new ScorecardDashboardDto(null, null, null, 0, 0, 0, 0, 0, []);

        int green = 0, amber = 0, red = 0, noData = 0;
        var atRisk = new List<(AtRiskObjectiveDto Dto, int Severity)>();

        foreach (var perspective in scorecard.Perspectives.OrderBy(p => p.SortOrder))
        foreach (var objective in perspective.Objectives.OrderBy(o => o.SortOrder))
        {
            var worst = "NoData";
            foreach (var measure in objective.Measures)
            {
                var rag = MeasureValueResolver.EvaluateRag(measure, await _resolver.ResolveAsync(measure));
                switch (rag)
                {
                    case "Green": green++; break;
                    case "Amber": amber++; break;
                    case "Red": red++; break;
                    default: noData++; break;
                }
                worst = (rag, worst) switch
                {
                    ("Red", _) => "Red",
                    ("Amber", not "Red") => "Amber",
                    ("Green", "NoData") => "Green",
                    _ => worst,
                };
            }

            if (worst is "Red" or "Amber")
                atRisk.Add((
                    new AtRiskObjectiveDto(objective.Id, objective.Code, objective.Name, perspective.Name, worst),
                    worst == "Red" ? 0 : 1));
        }

        var topAtRisk = atRisk
            .OrderBy(x => x.Severity)
            .Take(5)
            .Select(x => x.Dto)
            .ToList();

        return new ScorecardDashboardDto(
            scorecard.Id, scorecard.Code, scorecard.Name, activeCount,
            green, amber, red, noData, topAtRisk);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<Scorecard?> LoadScorecardTree(Guid id) =>
        _db.Scorecards
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.Measures)
                        .ThenInclude(m => m.Snapshots)
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.ProcessLinks)
            .Include(s => s.CauseLinks)
            .FirstOrDefaultAsync(s => s.Id == id);

    /// <summary>Generates the next sequential code (e.g., "BSC-003"), skipping past any taken values.</summary>
    private async Task<string> NextCode(string prefix, IQueryable<string> existingCodes)
    {
        var taken = await existingCodes.ToListAsync();
        var next = taken.Count + 1;
        string code;
        do { code = $"{prefix}-{next:D3}"; next++; } while (taken.Contains(code));
        return code;
    }

    /// <summary>Validates that a source-bound measure references an existing entity of the right type.</summary>
    private async Task<string?> ValidateSource(MeasureSourceType sourceType, Guid? sourceEntityId)
    {
        switch (sourceType)
        {
            case MeasureSourceType.ProcessYield:
            case MeasureSourceType.ProcessMaturity:
                if (sourceEntityId is null)
                    return $"{sourceType} measures require SourceEntityId (a Process id).";
                if (!await _db.Processes.AnyAsync(p => p.Id == sourceEntityId))
                    return "Source process does not exist.";
                return null;

            case MeasureSourceType.WorkflowThroughput:
                if (sourceEntityId is null)
                    return "WorkflowThroughput measures require SourceEntityId (a Workflow id).";
                if (!await _db.Workflows.AnyAsync(w => w.Id == sourceEntityId))
                    return "Source workflow does not exist.";
                return null;

            case MeasureSourceType.SpcCapability:
                if (sourceEntityId is null)
                    return "SpcCapability measures require SourceEntityId (an SpcChart id).";
                if (!await _db.SpcCharts.AnyAsync(c => c.Id == sourceEntityId))
                    return "Source SPC chart does not exist.";
                return null;

            case MeasureSourceType.PromptMetric:
                if (sourceEntityId is null)
                    return "PromptMetric measures require SourceEntityId (a prompt content block id).";
                if (!await _db.StepTemplateContents.AnyAsync(c => c.Id == sourceEntityId) &&
                    !await _db.ProcessStepContents.AnyAsync(c => c.Id == sourceEntityId))
                    return "Source prompt content block does not exist.";
                return null;

            case MeasureSourceType.Oee:
                if (sourceEntityId is not null &&
                    !await _db.Equipment.AnyAsync(eq => eq.Id == sourceEntityId))
                    return "Source equipment does not exist.";
                return null;

            default:
                return null;
        }
    }

    private async Task RemoveCauseLinksReferencing(List<Guid> objectiveIds)
    {
        var links = await _db.ObjectiveCauseLinks
            .Where(l => objectiveIds.Contains(l.SourceObjectiveId) || objectiveIds.Contains(l.TargetObjectiveId))
            .ToListAsync();
        _db.ObjectiveCauseLinks.RemoveRange(links);
    }

    private async Task<string> RollupObjectiveRag(StrategicObjective objective)
    {
        var worst = "NoData";
        foreach (var measure in objective.Measures)
        {
            var rag = MeasureValueResolver.EvaluateRag(measure, await _resolver.ResolveAsync(measure));
            worst = (rag, worst) switch
            {
                ("Red", _) => "Red",
                ("Amber", not "Red") => "Amber",
                ("Green", "NoData") => "Green",
                _ => worst,
            };
        }
        return worst;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private async Task<ScorecardResponseDto> MapScorecardAsync(Scorecard s, bool resolveValues)
    {
        var perspectives = new List<PerspectiveResponseDto>();
        foreach (var p in s.Perspectives.OrderBy(p => p.SortOrder))
            perspectives.Add(await MapPerspectiveAsync(p, resolveValues));

        return new ScorecardResponseDto(
            s.Id, s.Code, s.Name, s.MissionStatement, s.VisionStatement, s.StrategyNotes,
            s.Status.ToString(), s.OwnerOrgUnitId,
            perspectives,
            s.CauseLinks.Select(l => new CauseLinkResponseDto(
                l.Id, l.SourceObjectiveId, l.TargetObjectiveId, l.Description)).ToList(),
            s.CreatedAt, s.UpdatedAt);
    }

    private async Task<PerspectiveResponseDto> MapPerspectiveAsync(ScorecardPerspective p, bool resolveValues)
    {
        var objectives = new List<ObjectiveResponseDto>();
        foreach (var o in p.Objectives.OrderBy(o => o.SortOrder))
            objectives.Add(await MapObjectiveAsync(o, resolveValues));

        return new PerspectiveResponseDto(p.Id, p.Name, p.Description, p.SortOrder, objectives);
    }

    private async Task<ObjectiveResponseDto> MapObjectiveAsync(StrategicObjective o, bool resolveValues)
    {
        var measures = new List<MeasureResponseDto>();
        foreach (var m in o.Measures)
            measures.Add(await MapMeasureAsync(m, resolveValues));

        var processLinks = new List<ProcessLinkResponseDto>();
        foreach (var l in o.ProcessLinks)
            processLinks.Add(await MapProcessLinkAsync(l));

        return new ObjectiveResponseDto(
            o.Id, o.Code, o.Name, o.Description, o.OwnerOrgUnitId,
            o.Status.ToString(), o.TargetDate, o.SortOrder, measures, processLinks);
    }

    private async Task<MeasureResponseDto> MapMeasureAsync(ObjectiveMeasure m, bool resolveValues)
    {
        decimal? currentValue = resolveValues ? await _resolver.ResolveAsync(m) : null;
        var rag = resolveValues ? MeasureValueResolver.EvaluateRag(m, currentValue) : "NoData";
        var latestSnapshotAt = m.Snapshots.Count > 0 ? m.Snapshots.Max(s => s.CapturedAt) : (DateTime?)null;

        // Last 12 readings, oldest → newest, for trend sparklines.
        var trend = m.Snapshots
            .OrderByDescending(s => s.CapturedAt)
            .Take(12)
            .OrderBy(s => s.CapturedAt)
            .Select(s => s.Value)
            .ToList();

        return new MeasureResponseDto(
            m.Id, m.Name, m.Units, m.Direction.ToString(),
            m.BaselineValue, m.TargetValue, m.GreenThreshold, m.RedThreshold,
            m.SourceType.ToString(), m.SourceEntityId, m.SourceParameter,
            currentValue, rag, latestSnapshotAt, trend);
    }

    private async Task<ProcessLinkResponseDto> MapProcessLinkAsync(ObjectiveProcessLink l)
    {
        string? processName = l.ProcessId.HasValue
            ? await _db.Processes.Where(p => p.Id == l.ProcessId).Select(p => p.Name).FirstOrDefaultAsync()
            : null;
        string? workflowName = l.WorkflowId.HasValue
            ? await _db.Workflows.Where(w => w.Id == l.WorkflowId).Select(w => w.Name).FirstOrDefaultAsync()
            : null;

        return new ProcessLinkResponseDto(l.Id, l.ProcessId, processName, l.WorkflowId, workflowName, l.Note);
    }
}
