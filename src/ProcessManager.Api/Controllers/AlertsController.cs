using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    public AlertsController(ProcessManagerDbContext db) => _db = db;

    /// <summary>
    /// Returns recent out-of-range prompt responses.
    /// </summary>
    /// <param name="days">Rolling window in days (default 7, use 0 for all time).</param>
    /// <param name="limit">Maximum rows to return (default 100, max 500).</param>
    [HttpGet("out-of-range")]
    public async Task<ActionResult<List<OutOfRangeAlertDto>>> GetOutOfRange(
        [FromQuery] int days = 7,
        [FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);
        var since = days > 0
            ? DateTime.UtcNow.AddDays(-days)
            : DateTime.MinValue;

        var rows = await _db.PromptResponses
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
            .Take(limit)
            .ToListAsync();

        return rows.Select(r => new OutOfRangeAlertDto(
            r.Id,
            r.StepExecutionId,
            r.StepExecution.Job.Code,
            r.StepExecution.Job.Name,
            r.StepExecution.ProcessStep.Process.Name,
            r.StepExecution.ProcessStep.NameOverride
                ?? r.StepExecution.ProcessStep.StepTemplate.Name,
            r.StepTemplateContent?.Label
                ?? r.ProcessStepContent?.Label
                ?? "(unknown prompt)",
            r.ResponseValue,
            r.OverrideNote,
            r.CreatedBy,
            r.RespondedAt
        )).ToList();
    }

    /// <summary>
    /// Returns only the count — used by the NavMenu badge to avoid fetching full rows.
    /// </summary>
    [HttpGet("out-of-range/count")]
    public async Task<AlertCountDto> GetOutOfRangeCount([FromQuery] int days = 7)
    {
        var since = days > 0
            ? DateTime.UtcNow.AddDays(-days)
            : DateTime.MinValue;

        var count = await _db.PromptResponses
            .CountAsync(r => r.IsOutOfRange && (days == 0 || r.RespondedAt >= since));

        return new AlertCountDto(count);
    }
}
