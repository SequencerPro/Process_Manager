using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductionController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public ProductionController(ProcessManagerDbContext db) => _db = db;

    /// <summary>Current WIP state for the production dashboard.</summary>
    [HttpGet("wip")]
    public async Task<ActionResult<ProductionDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;

        // Active jobs
        var activeJobs = await _db.Jobs
            .Include(j => j.Process)
            .Include(j => j.StepExecutions.OrderBy(se => se.Sequence))
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .Where(j => j.Status == JobStatus.InProgress || j.Status == JobStatus.Created)
            .OrderBy(j => j.DueDate.HasValue ? j.DueDate : DateTime.MaxValue)
            .ToListAsync();

        // Current downtime map
        var currentDowntime = (await _db.DowntimeRecords
            .Where(d => d.EndedAt == null)
            .Select(d => d.EquipmentId)
            .ToListAsync()).ToHashSet();

        // Equipment code map
        var equipmentIds = activeJobs
            .SelectMany(j => j.StepExecutions)
            .Where(se => se.EquipmentId.HasValue)
            .Select(se => se.EquipmentId!.Value)
            .Distinct()
            .ToList();

        var equipmentMap = await _db.Equipment
            .Where(e => equipmentIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Code);

        var wipDtos = activeJobs.Select(job =>
        {
            var pendingSteps = job.StepExecutions
                .Where(se => se.Status is StepExecutionStatus.Pending or StepExecutionStatus.InProgress)
                .OrderBy(se => se.Sequence)
                .ToList();

            var currentStep = pendingSteps.FirstOrDefault();
            var currentStepName = currentStep?.ProcessStep?.StepTemplate?.Name ?? "";
            if (!string.IsNullOrEmpty(currentStep?.ProcessStep?.NameOverride))
                currentStepName = currentStep.ProcessStep.NameOverride;

            var currentStepExpected = currentStep?.ProcessStep?.StepTemplate?.ExpectedDurationMinutes;
            double? currentStepRunning = currentStep?.StartedAt is not null
                ? (now - currentStep.StartedAt.Value).TotalMinutes
                : null;

            var currentEquipId = currentStep?.EquipmentId;
            var currentEquipCode = currentEquipId.HasValue ? equipmentMap.GetValueOrDefault(currentEquipId.Value) : null;
            var currentEquipDown = currentEquipId.HasValue && currentDowntime.Contains(currentEquipId.Value);

            // Estimate completion: PlannedStartDate + sum of remaining step expected durations
            DateTime? expectedCompletion = null;
            if (job.PlannedStartDate.HasValue)
            {
                var totalRemaining = pendingSteps
                    .Sum(se => se.ProcessStep?.StepTemplate?.ExpectedDurationMinutes ?? 0);
                expectedCompletion = job.PlannedStartDate.Value.AddMinutes(totalRemaining);
            }

            var isLate = job.DueDate.HasValue && expectedCompletion.HasValue && expectedCompletion > job.DueDate;
            var daysLate = isLate
                ? (int)Math.Max(0, (expectedCompletion!.Value - job.DueDate!.Value).TotalDays)
                : 0;

            return new WipJobDto(
                job.Id, job.Code, job.Name,
                job.Process?.Name ?? "",
                job.Status.ToString(),
                job.DueDate, job.PlannedStartDate, expectedCompletion,
                isLate, daysLate,
                currentStepName, currentStepExpected, currentStepRunning,
                currentEquipId, currentEquipCode, currentEquipDown);
        }).ToList();

        // Bottlenecks: pending step executions grouped by step template
        var pendingByTemplate = await _db.StepExecutions
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Where(se => se.Status == StepExecutionStatus.Pending)
            .GroupBy(se => new
            {
                TemplateId = se.ProcessStep.StepTemplateId,
                TemplateName = se.ProcessStep.StepTemplate.Name,
                Expected = se.ProcessStep.StepTemplate.ExpectedDurationMinutes
            })
            .Select(g => new
            {
                g.Key.TemplateId,
                g.Key.TemplateName,
                g.Key.Expected,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var bottlenecks = pendingByTemplate.Select(x =>
        {
            var backlog = x.Expected.HasValue ? x.Count * x.Expected.Value : x.Count * 60.0;
            return new BottleneckStepDto(x.TemplateId, x.TemplateName, x.Count, x.Expected, backlog);
        }).OrderByDescending(b => b.BacklogMinutes).ToList();

        // Maintenance due/overdue
        var maintenanceDue = await _db.MaintenanceTasks
            .Include(t => t.Equipment)
            .Where(t => t.Status == MaintenanceTaskStatus.Due || t.Status == MaintenanceTaskStatus.Overdue)
            .OrderBy(t => t.DueDate)
            .Take(20)
            .ToListAsync();

        var lateJobs = wipDtos.Where(j => j.IsLate).OrderByDescending(j => j.DaysLate).ToList();
        var equipmentDown = currentDowntime.Count;

        return new ProductionDashboardDto(
            lateJobs,
            wipDtos,
            bottlenecks,
            maintenanceDue.Select(t => EquipmentController.MapTaskToDto(t)).ToList(),
            wipDtos.Count,
            lateJobs.Count,
            equipmentDown);
    }

    /// <summary>Ranked bottleneck step list.</summary>
    [HttpGet("bottlenecks")]
    public async Task<ActionResult<List<BottleneckStepDto>>> GetBottlenecks()
    {
        var pending = await _db.StepExecutions
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Where(se => se.Status == StepExecutionStatus.Pending)
            .GroupBy(se => new
            {
                TemplateId = se.ProcessStep.StepTemplateId,
                TemplateName = se.ProcessStep.StepTemplate.Name,
                Expected = se.ProcessStep.StepTemplate.ExpectedDurationMinutes
            })
            .Select(g => new
            {
                g.Key.TemplateId,
                g.Key.TemplateName,
                g.Key.Expected,
                Count = g.Count()
            })
            .ToListAsync();

        return pending.Select(x =>
        {
            var backlog = x.Expected.HasValue ? (double)(x.Count * x.Expected.Value) : x.Count * 60.0;
            return new BottleneckStepDto(x.TemplateId, x.TemplateName, x.Count, x.Expected, backlog);
        }).OrderByDescending(b => b.BacklogMinutes).ToList();
    }
}
