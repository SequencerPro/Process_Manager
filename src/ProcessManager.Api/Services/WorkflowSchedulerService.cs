using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcessManager.Api.Controllers;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

/// <summary>
/// Background service that polls for due WorkflowSchedules and auto-creates Workorders.
/// Polling interval is configurable via Scheduler:IntervalSeconds (default 60).
/// </summary>
public class WorkflowSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowSchedulerService> _logger;
    private readonly TimeSpan _interval;

    public WorkflowSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowSchedulerService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue<int>("Scheduler:IntervalSeconds", 60);
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowSchedulerService started. Polling every {Interval}s.", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WorkflowSchedulerService encountered an error.");
            }

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("WorkflowSchedulerService stopped.");
    }

    /// <summary>
    /// Processes all due schedules. Called each polling tick.
    /// Exposed as internal for direct invocation in integration tests.
    /// </summary>
    internal async Task ProcessDueSchedulesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();

        var now = DateTime.UtcNow;

        var dueSchedules = await db.WorkflowSchedules
            .Where(s => s.IsActive && s.NextRunAt != null && s.NextRunAt <= now)
            .Include(s => s.Workflow)
                .ThenInclude(wf => wf.WorkflowProcesses.Where(wp => !wp.IsTerminalNode))
                    .ThenInclude(wp => wp.Process)
                        .ThenInclude(p => p!.ProcessSteps)
            .ToListAsync(ct);

        foreach (var schedule in dueSchedules)
        {
            try
            {
                await FireScheduleAsync(db, schedule, now, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firing schedule {ScheduleId} ({ScheduleName}).", schedule.Id, schedule.Name);
            }
        }
    }

    private async Task FireScheduleAsync(
        ProcessManagerDbContext db,
        WorkflowSchedule schedule,
        DateTime now,
        CancellationToken ct)
    {
        var workflow = schedule.Workflow;

        // Validate workflow has entry points
        var entryPoints = workflow.WorkflowProcesses
            .Where(wp => wp.IsEntryPoint && wp.ProcessId.HasValue)
            .ToList();

        if (entryPoints.Count == 0)
        {
            _logger.LogWarning("Schedule {ScheduleId}: workflow {WorkflowId} has no entry points. Skipping.", schedule.Id, workflow.Id);
            // Advance anyway so we don't loop forever
            schedule.LastRunAt = now;
            schedule.NextRunAt = WorkflowSchedulesController.ComputeNextRunAt(schedule, now);
            if (schedule.EndDate.HasValue && now >= schedule.EndDate.Value)
                schedule.IsActive = false;
            await db.SaveChangesAsync(ct);
            return;
        }

        // Build workorder name from SubjectTemplate, substituting tokens
        var subject = ResolveSubjectTemplate(schedule.SubjectTemplate, now);
        if (string.IsNullOrWhiteSpace(subject))
            subject = $"{workflow.Name} – {now:yyyy-MM-dd}";

        // Generate a unique workorder code
        var dateCode = now.ToString("yyyyMMdd-HHmmss");
        var baseCode = $"SCH-{dateCode}";
        var code = baseCode;
        var suffix = 1;
        while (await db.Workorders.AnyAsync(w => w.Code == code, ct))
        {
            code = $"{baseCode}-{suffix:D2}";
            suffix++;
        }

        var workorder = new Workorder
        {
            Code = code,
            Name = subject,
            Description = $"Auto-created by schedule '{schedule.Name}'",
            WorkflowId = workflow.Id,
            WorkflowVersion = workflow.Version,
            Priority = 0,
            Status = WorkorderStatus.Created,
            ScheduleId = schedule.Id
        };

        db.Workorders.Add(workorder);

        // Create Jobs for each entry-point process
        foreach (var ep in entryPoints.OrderBy(e => e.SortOrder))
        {
            var process = ep.Process!;
            var jobCode = $"{code}-{process.Code}";
            var jobSuffix = 1;
            while (await db.Jobs.AnyAsync(j => j.Code == jobCode, ct)
                   || db.Jobs.Local.Any(j => j.Code == jobCode))
            {
                jobCode = $"{code}-{process.Code}-{jobSuffix:D2}";
                jobSuffix++;
            }

            var job = new Job
            {
                Code = jobCode,
                Name = $"{subject} - {process.Name}",
                Description = $"Auto-created by schedule '{schedule.Name}'",
                ProcessId = process.Id,
                ProcessVersion = process.Version,
                Priority = 0,
                Status = JobStatus.Created,
                WorkorderId = workorder.Id
            };

            db.Jobs.Add(job);

            foreach (var ps in process.ProcessSteps.OrderBy(ps => ps.Sequence))
            {
                db.StepExecutions.Add(new StepExecution
                {
                    JobId = job.Id,
                    ProcessStepId = ps.Id,
                    Sequence = ps.Sequence,
                    Status = StepExecutionStatus.Pending
                });
            }

            db.WorkorderJobs.Add(new WorkorderJob
            {
                WorkorderId = workorder.Id,
                WorkflowProcessId = ep.Id,
                JobId = job.Id
            });
        }

        // Update schedule state — use now as base (no backfill)
        schedule.LastRunAt = now;
        schedule.NextRunAt = WorkflowSchedulesController.ComputeNextRunAt(schedule, now);

        // Deactivate if EndDate has passed
        if (schedule.EndDate.HasValue && now >= schedule.EndDate.Value)
            schedule.IsActive = false;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Schedule {ScheduleId} fired: created workorder {Code}. NextRunAt={NextRunAt}.",
            schedule.Id, code, schedule.NextRunAt);
    }

    private static string ResolveSubjectTemplate(string template, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(template)) return string.Empty;

        return template
            .Replace("{Month}", now.ToString("MMMM"))
            .Replace("{Year}", now.Year.ToString())
            .Replace("{Date}", now.ToString("yyyy-MM-dd"));
    }
}
