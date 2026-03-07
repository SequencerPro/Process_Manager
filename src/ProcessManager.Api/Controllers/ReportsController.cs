using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public ReportsController(ProcessManagerDbContext db) => _db = db;

    // GET /api/reports/summary
    [HttpGet("summary")]
    public async Task<ReportSummaryDto> GetSummary()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total             = await _db.Jobs.CountAsync();
        var active            = await _db.Jobs.CountAsync(j => j.Status == JobStatus.InProgress);
        var completedThisMonth = await _db.Jobs.CountAsync(j =>
            j.Status == JobStatus.Completed && j.CompletedAt >= monthStart);
        var failedSteps = await _db.StepExecutions.CountAsync(se =>
            se.Status == StepExecutionStatus.Failed);

        // Average duration — load timestamps then compute in C# (provider-agnostic)
        var timestamps = await _db.Jobs
            .Where(j => j.StartedAt != null && j.CompletedAt != null)
            .Select(j => new { j.StartedAt, j.CompletedAt })
            .ToListAsync();

        double? avgHours = timestamps.Count > 0
            ? timestamps.Average(t => (t.CompletedAt!.Value - t.StartedAt!.Value).TotalHours)
            : null;

        return new ReportSummaryDto(total, active, completedThisMonth, failedSteps, avgHours);
    }

    // GET /api/reports/job-status-breakdown
    [HttpGet("job-status-breakdown")]
    public async Task<List<JobStatusBreakdownDto>> GetJobStatusBreakdown()
    {
        var rows = await _db.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return rows
            .OrderBy(r => r.Status)
            .Select(r => new JobStatusBreakdownDto(r.Status, r.Count))
            .ToList();
    }

    // GET /api/reports/step-performance
    [HttpGet("step-performance")]
    public async Task<List<StepPerformanceDto>> GetStepPerformance()
    {
        var rows = await _db.StepExecutions
            .Include(se => se.ProcessStep).ThenInclude(ps => ps.StepTemplate)
            .Select(se => new
            {
                StepName  = se.ProcessStep.StepTemplate.Name,
                se.Status,
                se.StartedAt,
                se.CompletedAt
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.StepName)
            .Select(g =>
            {
                var timed = g.Where(r => r.StartedAt != null && r.CompletedAt != null).ToList();
                double? avgMins = timed.Count > 0
                    ? timed.Average(r => (r.CompletedAt!.Value - r.StartedAt!.Value).TotalMinutes)
                    : null;
                return new StepPerformanceDto(
                    g.Key,
                    g.Count(),
                    g.Count(r => r.Status == StepExecutionStatus.Completed),
                    g.Count(r => r.Status == StepExecutionStatus.Failed),
                    avgMins);
            })
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    // GET /api/reports/recent-completions?count=10
    [HttpGet("recent-completions")]
    public async Task<List<RecentCompletionDto>> GetRecentCompletions([FromQuery] int count = 10)
    {
        var jobs = await _db.Jobs
            .Include(j => j.Process)
            .Where(j => j.Status == JobStatus.Completed && j.CompletedAt != null)
            .OrderByDescending(j => j.CompletedAt)
            .Take(count)
            .ToListAsync();

        return jobs.Select(j => new RecentCompletionDto(
            j.Id,
            j.Code,
            j.Name,
            j.Process.Name,
            j.StartedAt,
            j.CompletedAt!.Value,
            j.StartedAt.HasValue
                ? (j.CompletedAt.Value - j.StartedAt.Value).TotalHours
                : null
        )).ToList();
    }

    // GET /api/reports/throughput?days=30
    [HttpGet("throughput")]
    public async Task<List<ThroughputPointDto>> GetThroughput([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);

        var created = await _db.Jobs
            .Where(j => j.CreatedAt >= since)
            .GroupBy(j => j.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var completed = await _db.Jobs
            .Where(j => j.CompletedAt >= since && j.CompletedAt != null)
            .GroupBy(j => j.CompletedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var createdMap   = created.ToDictionary(x => x.Date, x => x.Count);
        var completedMap = completed.ToDictionary(x => x.Date, x => x.Count);

        return Enumerable.Range(0, days)
            .Select(i => since.AddDays(i))
            .Select(d => new ThroughputPointDto(
                DateOnly.FromDateTime(d),
                createdMap.GetValueOrDefault(d, 0),
                completedMap.GetValueOrDefault(d, 0)))
            .ToList();
    }
}
