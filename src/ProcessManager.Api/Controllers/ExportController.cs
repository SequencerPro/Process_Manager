using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public ExportController(ProcessManagerDbContext db) => _db = db;

    // ──────────────────── Step Executions ────────────────────

    /// <summary>
    /// Exports step-execution history as a CSV file.
    /// </summary>
    /// <param name="jobId">Optional — filter to a single job.</param>
    /// <param name="startDate">Optional UTC start date filter.</param>
    /// <param name="endDate">Optional UTC end date filter.</param>
    [HttpGet("step-executions")]
    public async Task<IActionResult> ExportStepExecutions(
        [FromQuery] Guid? jobId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _db.StepExecutions
            .AsNoTracking()
            .Include(se => se.Job)
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.Process)
            .AsQueryable();

        if (jobId.HasValue)
            query = query.Where(se => se.JobId == jobId.Value);

        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(se => se.CreatedAt >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            query = query.Where(se => se.CreatedAt <= endUtc);
        }

        var rows = await query
            .OrderByDescending(se => se.CreatedAt)
            .Take(10000)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("JobCode,JobName,ProcessName,StepName,Sequence,Status,StartedAt,CompletedAt,DurationMinutes,Notes,CreatedBy");

        foreach (var se in rows)
        {
            string? durationMinutes = null;
            if (se.StartedAt.HasValue && se.CompletedAt.HasValue)
                durationMinutes = ((se.CompletedAt.Value - se.StartedAt.Value).TotalMinutes).ToString("F2");

            sb.AppendLine(string.Join(",",
                CsvEscape(se.Job?.Code),
                CsvEscape(se.Job?.Name),
                CsvEscape(se.ProcessStep?.Process?.Name),
                CsvEscape(se.ProcessStep?.NameOverride ?? se.ProcessStep?.StepTemplate?.Name),
                se.Sequence.ToString(),
                CsvEscape(se.Status.ToString()),
                se.StartedAt?.ToString("o"),
                se.CompletedAt?.ToString("o"),
                durationMinutes ?? string.Empty,
                CsvEscape(se.Notes),
                CsvEscape(se.CreatedBy)
            ));
        }

        var filename = jobId.HasValue
            ? $"step-executions-job-{jobId:N}.csv"
            : $"step-executions-{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", filename);
    }

    // ──────────────────── Alerts ────────────────────

    /// <summary>
    /// Exports out-of-range alert history as a CSV file.
    /// </summary>
    /// <param name="days">Rolling window in days (default 7, use 0 for all time).</param>
    [HttpGet("alerts")]
    public async Task<IActionResult> ExportAlerts([FromQuery] int days = 7)
    {
        var since = days > 0
            ? DateTime.UtcNow.AddDays(-days)
            : DateTime.MinValue;

        var rows = await _db.PromptResponses
            .AsNoTracking()
            .Include(r => r.StepExecution)
                .ThenInclude(se => se.Job)
            .Include(r => r.StepExecution)
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .Include(r => r.StepExecution)
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.Process)
            .Include(r => r.StepTemplateContent)
            .Include(r => r.ProcessStepContent)
            .Where(r => r.IsOutOfRange && (days == 0 || r.RespondedAt >= since))
            .OrderByDescending(r => r.RespondedAt)
            .Take(10000)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("RespondedAt,JobCode,JobName,ProcessName,StepName,PromptLabel,Value,OverrideNote,Operator");

        foreach (var r in rows)
        {
            string promptLabel = r.StepTemplateContent?.Label
                ?? r.ProcessStepContent?.Label
                ?? "(unknown prompt)";
            string stepName = r.StepExecution?.ProcessStep?.NameOverride
                ?? r.StepExecution?.ProcessStep?.StepTemplate?.Name
                ?? "(unknown step)";

            sb.AppendLine(string.Join(",",
                r.RespondedAt.ToString("o"),
                CsvEscape(r.StepExecution?.Job?.Code),
                CsvEscape(r.StepExecution?.Job?.Name),
                CsvEscape(r.StepExecution?.ProcessStep?.Process?.Name),
                CsvEscape(stepName),
                CsvEscape(promptLabel),
                CsvEscape(r.ResponseValue),
                CsvEscape(r.OverrideNote),
                CsvEscape(r.CreatedBy)
            ));
        }

        string filename = $"alerts-{(days == 0 ? "all" : $"{days}d")}-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", filename);
    }

    // ──────────────────── helpers ────────────────────

    /// <summary>
    /// Escapes a CSV field: wraps in double quotes if it contains commas, quotes, or newlines.
    /// </summary>
    private static string CsvEscape(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
