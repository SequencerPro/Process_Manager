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
[Route("api/spc")]
public class SpcController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly ISpcCalculationService _calc;

    public SpcController(ProcessManagerDbContext db, ISpcCalculationService calc)
    {
        _db = db;
        _calc = calc;
    }

    // ── Chart CRUD ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SpcChartSummaryDto>>> GetAll(
        [FromQuery] Guid? processId = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .AsQueryable();

        if (processId.HasValue)
            query = query.Where(c => c.ProcessId == processId.Value);
        if (active.HasValue)
            query = query.Where(c => c.IsActive == active.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var summaries = items.Select(c =>
        {
            var values = c.DataPoints.OrderBy(d => d.SubgroupIndex).Select(d => d.Value).ToList();
            SpcCalculationResultDto? calc = values.Count >= c.SubgroupSize
                ? _calc.Calculate(values, c.SubgroupSize, c.LSL, c.USL)
                : null;

            return new SpcChartSummaryDto(
                c.Id, c.ProcessId, c.Process.Name,
                null, c.Name, c.ChartType.ToString(),
                c.IsActive, c.DataPoints.Count,
                calc?.Cpk, calc?.Cpk,
                calc?.OutOfControlPoints.Count ?? 0);
        }).ToList();

        return new PaginatedResponse<SpcChartSummaryDto>(summaries, totalCount, page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpcChartDto>> GetById(Guid id)
    {
        var chart = await _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chart is null) return NotFound();
        return MapToDto(chart);
    }

    [HttpPost]
    public async Task<ActionResult<SpcChartDto>> Create([FromBody] CreateSpcChartDto dto)
    {
        var process = await _db.Processes.FindAsync(dto.ProcessId);
        if (process is null) return NotFound("Process not found.");

        if (!Enum.TryParse<SpcChartType>(dto.ChartType, true, out var chartType))
            return BadRequest($"Invalid chart type: {dto.ChartType}");
        if (!Enum.TryParse<ControlLimitSource>(dto.ControlLimitSource, true, out var limitSource))
            return BadRequest($"Invalid control limit source: {dto.ControlLimitSource}");

        if (dto.SubgroupSize < 2 || dto.SubgroupSize > 10)
            return BadRequest("Subgroup size must be between 2 and 10.");

        var chart = new SpcChart
        {
            ProcessId = dto.ProcessId,
            ContentBlockId = dto.ContentBlockId,
            Name = dto.Name,
            ChartType = chartType,
            SubgroupSize = dto.SubgroupSize,
            ControlLimitSource = limitSource,
            UCL = dto.UCL,
            LCL = dto.LCL,
            CL = dto.CL,
            RangeUCL = dto.RangeUCL,
            RangeLCL = dto.RangeLCL,
            RangeCL = dto.RangeCL,
            TargetCpk = dto.TargetCpk,
            LSL = dto.LSL,
            USL = dto.USL
        };

        _db.SpcCharts.Add(chart);
        await _db.SaveChangesAsync();

        chart.Process = process;
        return Created($"api/spc/{chart.Id}", MapToDto(chart));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SpcChartDto>> Update(Guid id, [FromBody] UpdateSpcChartDto dto)
    {
        var chart = await _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chart is null) return NotFound();

        if (!Enum.TryParse<SpcChartType>(dto.ChartType, true, out var chartType))
            return BadRequest($"Invalid chart type: {dto.ChartType}");
        if (!Enum.TryParse<ControlLimitSource>(dto.ControlLimitSource, true, out var limitSource))
            return BadRequest($"Invalid control limit source: {dto.ControlLimitSource}");

        chart.Name = dto.Name;
        chart.ChartType = chartType;
        chart.SubgroupSize = dto.SubgroupSize;
        chart.ControlLimitSource = limitSource;
        chart.UCL = dto.UCL;
        chart.LCL = dto.LCL;
        chart.CL = dto.CL;
        chart.RangeUCL = dto.RangeUCL;
        chart.RangeLCL = dto.RangeLCL;
        chart.RangeCL = dto.RangeCL;
        chart.TargetCpk = dto.TargetCpk;
        chart.LSL = dto.LSL;
        chart.USL = dto.USL;
        chart.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return MapToDto(chart);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var chart = await _db.SpcCharts
            .Include(c => c.DataPoints)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chart is null) return NotFound();

        _db.SpcCharts.Remove(chart);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Data Points ─────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/data-points")]
    public async Task<ActionResult<List<SpcDataPointDto>>> GetDataPoints(Guid id)
    {
        var chart = await _db.SpcCharts.FindAsync(id);
        if (chart is null) return NotFound();

        var points = await _db.SpcDataPoints
            .Where(d => d.SpcChartId == id)
            .OrderBy(d => d.SubgroupIndex)
            .ThenBy(d => d.CapturedAt)
            .Select(d => new SpcDataPointDto(d.Id, d.SpcChartId, d.StepExecutionId, d.Value, d.SubgroupIndex, d.CapturedAt))
            .ToListAsync();

        return points;
    }

    [HttpPost("{id:guid}/data-points")]
    [Authorize]
    public async Task<ActionResult<SpcDataPointDto>> AddDataPoint(Guid id, [FromBody] AddSpcDataPointDto dto)
    {
        var chart = await _db.SpcCharts.FindAsync(id);
        if (chart is null) return NotFound();

        var stepExecution = await _db.StepExecutions.FindAsync(dto.StepExecutionId);
        if (stepExecution is null) return BadRequest("Step execution not found.");

        var nextIndex = await _db.SpcDataPoints
            .Where(d => d.SpcChartId == id)
            .Select(d => (int?)d.SubgroupIndex)
            .MaxAsync() ?? -1;

        var point = new SpcDataPoint
        {
            SpcChartId = id,
            StepExecutionId = dto.StepExecutionId,
            Value = dto.Value,
            SubgroupIndex = nextIndex + 1,
            CapturedAt = DateTime.UtcNow
        };

        _db.SpcDataPoints.Add(point);
        await _db.SaveChangesAsync();

        return Created($"api/spc/{id}/data-points/{point.Id}",
            new SpcDataPointDto(point.Id, point.SpcChartId, point.StepExecutionId, point.Value, point.SubgroupIndex, point.CapturedAt));
    }

    // ── Calculate ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/calculate")]
    public async Task<ActionResult<SpcCalculationResultDto>> Calculate(Guid id)
    {
        var chart = await _db.SpcCharts
            .Include(c => c.DataPoints)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chart is null) return NotFound();

        var values = chart.DataPoints
            .OrderBy(d => d.SubgroupIndex)
            .Select(d => d.Value)
            .ToList();

        if (values.Count < chart.SubgroupSize)
            return BadRequest($"Need at least {chart.SubgroupSize} data points (have {values.Count}).");

        var result = _calc.Calculate(values, chart.SubgroupSize, chart.LSL, chart.USL);
        return result;
    }

    // ── Dashboard ───────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<ActionResult<List<SpcChartSummaryDto>>> GetDashboard()
    {
        var charts = await _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var summaries = charts.Select(c =>
        {
            var values = c.DataPoints.OrderBy(d => d.SubgroupIndex).Select(d => d.Value).ToList();
            SpcCalculationResultDto? calc = values.Count >= c.SubgroupSize
                ? _calc.Calculate(values, c.SubgroupSize, c.LSL, c.USL)
                : null;

            return new SpcChartSummaryDto(
                c.Id, c.ProcessId, c.Process.Name,
                null, c.Name, c.ChartType.ToString(),
                c.IsActive, c.DataPoints.Count,
                calc?.Cp, calc?.Cpk,
                calc?.OutOfControlPoints.Count ?? 0);
        }).ToList();

        return summaries;
    }

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static SpcChartDto MapToDto(SpcChart c) => new(
        c.Id, c.ProcessId, c.Process.Name,
        c.ContentBlockId, null, c.Name,
        c.ChartType.ToString(), c.SubgroupSize,
        c.ControlLimitSource.ToString(),
        c.UCL, c.LCL, c.CL,
        c.RangeUCL, c.RangeLCL, c.RangeCL,
        c.TargetCpk, c.LSL, c.USL,
        c.IsActive, c.DataPoints.Count,
        c.CreatedAt);
}
