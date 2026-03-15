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

    // GET /api/reports/process-timing?processRole=ManufacturingProcess
    [HttpGet("process-timing")]
    public async Task<List<ProcessTimingDto>> GetProcessTiming([FromQuery] string? processRole = null)
    {
        Domain.Enums.ProcessRole? roleFilter = null;
        if (processRole != null && Enum.TryParse<Domain.Enums.ProcessRole>(processRole, out var parsed))
            roleFilter = parsed;

        // ── Step executions: all completed with both timestamps ───────────────
        var stepQuery = _db.StepExecutions
            .Where(se => se.Status == StepExecutionStatus.Completed
                      && se.StartedAt  != null
                      && se.CompletedAt != null);

        if (roleFilter.HasValue)
            stepQuery = stepQuery.Where(se => se.ProcessStep.Process.ProcessRole == roleFilter.Value);

        var stepRaw = await stepQuery
            .Select(se => new
            {
                ProcessId   = se.ProcessStep.ProcessId,
                ProcessCode = se.ProcessStep.Process.Code,
                ProcessName = se.ProcessStep.Process.Name,
                RoleValue   = se.ProcessStep.Process.ProcessRole,
                Sequence    = se.ProcessStep.Sequence,
                StepCode    = se.ProcessStep.StepTemplate.Code,
                StepName    = se.ProcessStep.NameOverride ?? se.ProcessStep.StepTemplate.Name,
                StartedAt   = se.StartedAt!.Value,
                CompletedAt = se.CompletedAt!.Value
            })
            .ToListAsync();

        // ── Jobs: all completed with both timestamps ──────────────────────────
        var jobQuery = _db.Jobs
            .Where(j => j.Status == JobStatus.Completed
                     && j.StartedAt  != null
                     && j.CompletedAt != null);

        if (roleFilter.HasValue)
            jobQuery = jobQuery.Where(j => j.Process.ProcessRole == roleFilter.Value);

        var jobRaw = await jobQuery
            .Select(j => new
            {
                j.ProcessId,
                StartedAt   = j.StartedAt!.Value,
                CompletedAt = j.CompletedAt!.Value
            })
            .ToListAsync();

        // ── Aggregate job durations per process ───────────────────────────────
        var jobsByProcess = jobRaw
            .GroupBy(j => j.ProcessId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(j => (j.CompletedAt - j.StartedAt).TotalHours)
                       .OrderBy(h => h).ToList());

        // ── Aggregate step durations per (process, sequence) ──────────────────
        var stepsByProcess = stepRaw
            .GroupBy(s => s.ProcessId)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(s => s.Sequence)
                .OrderBy(sg => sg.Key)
                .Select(sg =>
                {
                    var first  = sg.First();
                    var sorted = sg.Select(s => (s.CompletedAt - s.StartedAt).TotalMinutes)
                                   .OrderBy(m => m).ToList();
                    return new StepTimingDto(
                        Sequence:           first.Sequence,
                        StepCode:           first.StepCode,
                        StepName:           first.StepName,
                        CompletedExecutions: sorted.Count,
                        MinMinutes:         sorted.Count > 0 ? sorted.First()    : null,
                        AvgMinutes:         sorted.Count > 0 ? sorted.Average()  : null,
                        MedianMinutes:      Percentile(sorted, 0.50),
                        P95Minutes:         Percentile(sorted, 0.95),
                        MaxMinutes:         sorted.Count > 0 ? sorted.Last()     : null);
                })
                .ToList());

        // ── Build process metadata lookup (from step data, supplement from DB) ─
        var processMetaById = stepRaw
            .GroupBy(s => s.ProcessId)
            .ToDictionary(
                g => g.Key,
                g => new { g.First().ProcessCode, g.First().ProcessName, g.First().RoleValue });

        var missingIds = jobsByProcess.Keys.Except(processMetaById.Keys).ToList();
        if (missingIds.Count > 0)
        {
            var extra = await _db.Processes
                .Where(p => missingIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Name, p.ProcessRole })
                .ToListAsync();
            foreach (var e in extra)
                processMetaById[e.Id] = new { ProcessCode = e.Code, ProcessName = e.Name, RoleValue = e.ProcessRole };
        }

        // ── Combine into result list ───────────────────────────────────────────
        return jobsByProcess.Keys.Union(stepsByProcess.Keys)
            .Where(id => processMetaById.ContainsKey(id))
            .Select(id =>
            {
                var meta  = processMetaById[id];
                var jobs  = jobsByProcess.GetValueOrDefault(id) ?? new List<double>();
                var steps = stepsByProcess.GetValueOrDefault(id) ?? new List<StepTimingDto>();
                return new ProcessTimingDto(
                    ProcessId:    id,
                    Code:         meta.ProcessCode,
                    Name:         meta.ProcessName,
                    ProcessRole:  meta.RoleValue.ToString(),
                    CompletedJobs: jobs.Count,
                    MinHours:    jobs.Count > 0 ? jobs.First()   : null,
                    AvgHours:    jobs.Count > 0 ? jobs.Average() : null,
                    MedianHours: Percentile(jobs, 0.50),
                    P95Hours:    Percentile(jobs, 0.95),
                    MaxHours:    jobs.Count > 0 ? jobs.Last()    : null,
                    Steps:       steps);
            })
            .OrderByDescending(p => p.CompletedJobs)
            .ThenBy(p => p.Name)
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Returns the value at percentile p (0–1) of a pre-sorted ascending list.</summary>
    private static double? Percentile(List<double> sortedAsc, double p)
    {
        if (sortedAsc.Count == 0) return null;
        var idx = (int)Math.Ceiling(sortedAsc.Count * p) - 1;
        return sortedAsc[Math.Clamp(idx, 0, sortedAsc.Count - 1)];
    }
}
