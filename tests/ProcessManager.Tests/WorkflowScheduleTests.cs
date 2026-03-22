using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ProcessManager.Api.Controllers;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Integration and unit tests for Phase 12 Step 3: WorkflowSchedule entity + background scheduler.
/// Covers CRUD endpoints, NextRunAt computation, and scheduler firing logic.
/// </summary>
public class WorkflowScheduleTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public WorkflowScheduleTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    // ──────────── Helpers ────────────

    private async Task<WorkflowResponseDto> CreateWorkflowForSchedule(string? suffix = null)
    {
        var pfx = suffix ?? Guid.NewGuid().ToString()[..6];
        var kind = await CreateKind($"K-{pfx}", "Part");
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);
        var step = await CreateTransformStep($"ST-{pfx}", "Work", kind.Id, grade.Id, kind.Id, grade.Id);
        var proc = await CreateProcess($"PA-{pfx}", "Process A");
        await AddProcessStep(proc.Id, step.Id, 1);
        await ReleaseProcess(proc.Id);

        var dto = new CreateWorkflowDto($"WF-{pfx}", $"Workflow {pfx}");
        var resp = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var wf = (await resp.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;

        var wpEntry = new AddWorkflowProcessDto(proc.Id, true, 1);
        var resp2 = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/processes", wpEntry, JsonOptions);
        resp2.EnsureSuccessStatusCode();

        var wpEnd = new AddWorkflowProcessDto(null, false, 2, IsTerminalNode: true);
        var resp3 = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/processes", wpEnd, JsonOptions);
        resp3.EnsureSuccessStatusCode();

        return wf;
    }

    private async Task<WorkflowResponseDto> CreateWorkflowWithNoEntryPoints(string? suffix = null)
    {
        var pfx = suffix ?? Guid.NewGuid().ToString()[..6];
        var dto = new CreateWorkflowDto($"WF-NOEP-{pfx}", $"No Entry Point {pfx}");
        var resp = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowScheduleResponseDto> CreateSchedule(
        Guid workflowId,
        ScheduleRecurrenceType recurrenceType = ScheduleRecurrenceType.Daily,
        int interval = 1,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? dayOfWeek = null,
        int? dayOfMonth = null,
        string? name = null,
        bool isActive = true,
        string? subjectTemplate = null)
    {
        var dto = new CreateWorkflowScheduleDto(
            workflowId,
            name ?? $"Test Schedule {Guid.NewGuid().ToString()[..4]}",
            recurrenceType,
            interval,
            dayOfWeek,
            dayOfMonth,
            startDate ?? DateTime.UtcNow,
            endDate,
            subjectTemplate ?? "{Month} {Year}",
            isActive);

        var resp = await Client.PostAsJsonAsync("/api/workflowschedules", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowScheduleResponseDto>(JsonOptions))!;
    }

    // ──────────── CRUD Happy Paths ────────────

    [Fact]
    public async Task Create_Daily_SetsNextRunAtToStartDate()
    {
        var wf = await CreateWorkflowForSchedule();
        var start = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc);
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily, startDate: start);

        Assert.NotNull(schedule.NextRunAt);
        // Daily initial NextRunAt should be at StartDate
        Assert.Equal(start, schedule.NextRunAt!.Value, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Create_Weekly_SnapsToDayOfWeek()
    {
        var wf = await CreateWorkflowForSchedule();
        // Start on a Thursday (2026-03-19), ask for Monday (1)
        var start = new DateTime(2026, 3, 19, 9, 0, 0, DateTimeKind.Utc);
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Weekly,
            interval: 1, startDate: start, dayOfWeek: 1 /* Monday */);

        Assert.NotNull(schedule.NextRunAt);
        // Should be Monday 2026-03-23
        Assert.Equal(DayOfWeek.Monday, schedule.NextRunAt!.Value.DayOfWeek);
    }

    [Fact]
    public async Task Create_Monthly_SnapsToDayOfMonth()
    {
        var wf = await CreateWorkflowForSchedule();
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Monthly,
            interval: 1, startDate: start, dayOfMonth: 15);

        Assert.NotNull(schedule.NextRunAt);
        Assert.Equal(15, schedule.NextRunAt!.Value.Day);
        Assert.Equal(3, schedule.NextRunAt!.Value.Month);
    }

    [Fact]
    public async Task Create_WithEndDateInPast_NextRunAtIsNull()
    {
        var wf = await CreateWorkflowForSchedule();
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily,
            startDate: start, endDate: end);

        // NextRunAt should be null because EndDate is in the past
        Assert.Null(schedule.NextRunAt);
    }

    [Fact]
    public async Task GetById_ReturnsCorrectWorkflowName()
    {
        var wf = await CreateWorkflowForSchedule();
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily);

        var resp = await Client.GetFromJsonAsync<WorkflowScheduleResponseDto>(
            $"/api/workflowschedules/{created.Id}", JsonOptions);

        Assert.NotNull(resp);
        Assert.Equal(wf.Name, resp!.WorkflowName);
    }

    [Fact]
    public async Task Update_ChangesRecurrenceAndRecomputesNextRunAt()
    {
        var wf = await CreateWorkflowForSchedule();
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily, interval: 1);

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var updateDto = new UpdateWorkflowScheduleDto(
            created.Name,
            ScheduleRecurrenceType.Hourly,
            4,
            null,
            null,
            start,
            null,
            null,
            true);

        var resp = await Client.PutAsJsonAsync($"/api/workflowschedules/{created.Id}", updateDto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var updated = (await resp.Content.ReadFromJsonAsync<WorkflowScheduleResponseDto>(JsonOptions))!;

        Assert.Equal("Hourly", updated.RecurrenceType);
        Assert.Equal(4, updated.RecurrenceInterval);
        // NextRunAt should be start + 4 hours (since ComputeInitialNextRunAt for Hourly calls ComputeNextRunAt)
        Assert.NotNull(updated.NextRunAt);
    }

    [Fact]
    public async Task Delete_WithNoWorkorders_Succeeds()
    {
        var wf = await CreateWorkflowForSchedule();
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily);

        var resp = await Client.DeleteAsync($"/api/workflowschedules/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingWorkorders_Returns400()
    {
        var wf = await CreateWorkflowForSchedule();
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily);

        // Manually insert a workorder linked to this schedule
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var wo = new Workorder
            {
                Code = $"WO-SCHED-{Guid.NewGuid().ToString()[..6]}",
                Name = "Scheduled WO",
                WorkflowId = wf.Id,
                WorkflowVersion = 1,
                Status = WorkorderStatus.Created,
                ScheduleId = schedule.Id
            };
            db.Workorders.Add(wo);
            db.SaveChanges();
        }

        var resp = await Client.DeleteAsync($"/api/workflowschedules/{schedule.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Activate_SetsIsActiveTrue()
    {
        var wf = await CreateWorkflowForSchedule();
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily, isActive: false);
        Assert.False(created.IsActive);

        var resp = await Client.PostAsync($"/api/workflowschedules/{created.Id}/activate", null);
        resp.EnsureSuccessStatusCode();
        var result = (await resp.Content.ReadFromJsonAsync<WorkflowScheduleResponseDto>(JsonOptions))!;
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Deactivate_SetsIsActiveFalse()
    {
        var wf = await CreateWorkflowForSchedule();
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily, isActive: true);
        Assert.True(created.IsActive);

        var resp = await Client.PostAsync($"/api/workflowschedules/{created.Id}/deactivate", null);
        resp.EnsureSuccessStatusCode();
        var result = (await resp.Content.ReadFromJsonAsync<WorkflowScheduleResponseDto>(JsonOptions))!;
        Assert.False(result.IsActive);
    }

    // ──────────── NextRunAt Edge Cases ────────────

    [Fact]
    public async Task MonthlySchedule_DayOfMonth31_InFebruary_SnapsToLastDay()
    {
        // Create a monthly schedule that would land in February
        var wf = await CreateWorkflowForSchedule();
        // Start in January 2026, day 31 — next run will be in January itself
        // Then after firing, next run would be Feb (day 31 → day 28)
        var start = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var schedule = new WorkflowSchedule
        {
            WorkflowId = wf.Id,
            Name = "Feb Test",
            RecurrenceType = ScheduleRecurrenceType.Monthly,
            RecurrenceInterval = 1,
            DayOfMonth = 31,
            StartDate = start,
            SubjectTemplate = "Test"
        };

        // Simulate firing from Jan 31 → should give Feb 28 (2026 is not a leap year)
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, start);
        Assert.NotNull(next);
        Assert.Equal(2, next!.Value.Month);
        Assert.Equal(28, next.Value.Day);
    }

    [Fact]
    public async Task Create_WorkflowNotFound_Returns400()
    {
        var dto = new CreateWorkflowScheduleDto(
            Guid.NewGuid(),
            "Bad Schedule",
            ScheduleRecurrenceType.Daily,
            1, null, null,
            DateTime.UtcNow, null, null, true);

        var resp = await Client.PostAsJsonAsync("/api/workflowschedules", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ──────────── Hourly-specific CRUD ────────────

    [Fact]
    public async Task Create_Hourly_Interval4_SetsNextRunAtToStartDatePlusFourHours()
    {
        var wf = await CreateWorkflowForSchedule();
        var start = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc);
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Hourly,
            interval: 4, startDate: start);

        // Initial NextRunAt for Hourly = ComputeNextRunAt(schedule, startDate) = start + 4h
        Assert.NotNull(schedule.NextRunAt);
        Assert.Equal(start.AddHours(4), schedule.NextRunAt!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Create_Hourly_Interval1_Succeeds()
    {
        var wf = await CreateWorkflowForSchedule();
        var resp = await Client.PostAsJsonAsync("/api/workflowschedules",
            new CreateWorkflowScheduleDto(wf.Id, "H1", ScheduleRecurrenceType.Hourly, 1,
                null, null, DateTime.UtcNow, null, null, true),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Create_Hourly_Interval168_Succeeds()
    {
        var wf = await CreateWorkflowForSchedule();
        var resp = await Client.PostAsJsonAsync("/api/workflowschedules",
            new CreateWorkflowScheduleDto(wf.Id, "H168", ScheduleRecurrenceType.Hourly, 168,
                null, null, DateTime.UtcNow, null, null, true),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Create_Hourly_Interval0_Returns400()
    {
        var wf = await CreateWorkflowForSchedule();
        var resp = await Client.PostAsJsonAsync("/api/workflowschedules",
            new CreateWorkflowScheduleDto(wf.Id, "H0", ScheduleRecurrenceType.Hourly, 0,
                null, null, DateTime.UtcNow, null, null, true),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Create_Hourly_Interval169_Returns400()
    {
        var wf = await CreateWorkflowForSchedule();
        var resp = await Client.PostAsJsonAsync("/api/workflowschedules",
            new CreateWorkflowScheduleDto(wf.Id, "H169", ScheduleRecurrenceType.Hourly, 169,
                null, null, DateTime.UtcNow, null, null, true),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Create_Hourly_DayOfWeekIgnored_StoredAsNull()
    {
        var wf = await CreateWorkflowForSchedule();
        var schedule = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Hourly,
            interval: 2, dayOfWeek: 3 /* Wednesday — should be nulled out server-side */);

        Assert.Null(schedule.DayOfWeek);
        Assert.Null(schedule.DayOfMonth);
    }

    [Fact]
    public async Task Update_ChangeFromDailyToHourly_RecomputesNextRunAt()
    {
        var wf = await CreateWorkflowForSchedule();
        var start = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc);
        var created = await CreateSchedule(wf.Id, ScheduleRecurrenceType.Daily, interval: 1, startDate: start);
        Assert.NotNull(created.NextRunAt);
        var oldNext = created.NextRunAt!.Value;

        var newStart = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc);
        var updateDto = new UpdateWorkflowScheduleDto(
            created.Name,
            ScheduleRecurrenceType.Hourly,
            6,
            null, null,
            newStart,
            null, null, true);
        var resp = await Client.PutAsJsonAsync($"/api/workflowschedules/{created.Id}", updateDto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var updated = (await resp.Content.ReadFromJsonAsync<WorkflowScheduleResponseDto>(JsonOptions))!;

        Assert.Equal("Hourly", updated.RecurrenceType);
        Assert.NotNull(updated.NextRunAt);
        // New next run should differ from old next run
        Assert.NotEqual(oldNext, updated.NextRunAt!.Value);
    }

    // ──────────── ComputeNextRunAt Unit Tests (static, no HTTP) ────────────

    private static WorkflowSchedule MakeSchedule(
        ScheduleRecurrenceType type,
        int interval = 1,
        int? dayOfWeek = null,
        int? dayOfMonth = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return new WorkflowSchedule
        {
            WorkflowId = Guid.NewGuid(),
            Name = "Test",
            RecurrenceType = type,
            RecurrenceInterval = interval,
            DayOfWeek = dayOfWeek,
            DayOfMonth = dayOfMonth,
            StartDate = startDate ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = endDate,
            SubjectTemplate = "Test"
        };
    }

    [Fact]
    public void ComputeNextRunAt_Daily_Interval2_AddsCorrectDays()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Daily, interval: 2);
        var from = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc), next!.Value);
    }

    [Fact]
    public void ComputeNextRunAt_Weekly_FindsCorrectDayOfWeek()
    {
        // DayOfWeek=5 (Friday), from a Wednesday
        var schedule = MakeSchedule(ScheduleRecurrenceType.Weekly, interval: 1, dayOfWeek: 5);
        var from = new DateTime(2026, 3, 18, 12, 0, 0, DateTimeKind.Utc); // Wednesday
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(DayOfWeek.Friday, next!.Value.DayOfWeek);
        Assert.Equal(2026, next.Value.Year);
    }

    [Fact]
    public void ComputeNextRunAt_Monthly_DayOfMonth15_CorrectMonth()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Monthly, interval: 1, dayOfMonth: 15);
        var from = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(4, next!.Value.Month); // April
        Assert.Equal(15, next.Value.Day);
    }

    [Fact]
    public void ComputeNextRunAt_Quarterly_AdvancesThreeMonths()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Quarterly, interval: 1, dayOfMonth: 1);
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(4, next!.Value.Month); // April
        Assert.Equal(1, next.Value.Day);
    }

    [Fact]
    public void ComputeNextRunAt_PastEndDate_ReturnsNull()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Daily, interval: 1,
            endDate: new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        var from = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.Null(next);
    }

    [Fact]
    public void ComputeNextRunAt_Hourly_Interval1_AddsExactlyOneHour()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Hourly, interval: 1);
        var from = new DateTime(2026, 3, 21, 14, 30, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(from.AddHours(1), next!.Value);
    }

    [Fact]
    public void ComputeNextRunAt_Hourly_Interval4_AddsFourHours()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Hourly, interval: 4);
        var from = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        Assert.Equal(from.AddHours(4), next!.Value);
    }

    [Fact]
    public void ComputeNextRunAt_Hourly_DoesNotSnapToDay()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Hourly, interval: 3);
        var from = new DateTime(2026, 3, 21, 10, 45, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        Assert.NotNull(next);
        // Result should NOT be midnight — it should be 13:45
        Assert.NotEqual(TimeSpan.Zero, next!.Value.TimeOfDay);
        Assert.Equal(from.AddHours(3), next.Value);
    }

    [Fact]
    public void ComputeNextRunAt_Hourly_PastEndDate_ReturnsNull()
    {
        var schedule = MakeSchedule(ScheduleRecurrenceType.Hourly, interval: 1,
            endDate: new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc));
        var from = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc);
        var next = WorkflowSchedulesController.ComputeNextRunAt(schedule, from);
        // next = 11:00 which is > endDate 10:00 → null
        Assert.Null(next);
    }

    [Fact]
    public void ComputeNextRunAt_Hourly_MissedThreeWindows_OnlyOneWorkorderCreated()
    {
        // Demonstrates that ComputeNextRunAt(schedule, now) always uses 'now' as base,
        // so missed windows don't backfill — only one advancement happens per tick.
        var schedule = MakeSchedule(ScheduleRecurrenceType.Hourly, interval: 1);
        var originalNextRunAt = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 3, 21, 11, 0, 0, DateTimeKind.Utc); // 3 hours late

        // The scheduler fires once and uses 'now' as the base for the next run
        var nextAfterFire = WorkflowSchedulesController.ComputeNextRunAt(schedule, now);
        Assert.NotNull(nextAfterFire);
        // Should be now + 1h, not originalNextRunAt + 3h
        Assert.Equal(now.AddHours(1), nextAfterFire!.Value);
        // Verify this is not 3 hours ahead of original (which would be backfilling)
        Assert.NotEqual(originalNextRunAt.AddHours(3), nextAfterFire.Value);
    }

    // ──────────── Scheduler Integration Tests ────────────

    private WorkflowSchedulerService CreateSchedulerService()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var logger = _factory.Services.GetRequiredService<ILogger<WorkflowSchedulerService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Scheduler:IntervalSeconds"] = "60" })
            .Build();
        return new WorkflowSchedulerService(scopeFactory, logger, config);
    }

    private async Task<WorkflowSchedule> InsertDueSchedule(
        Guid workflowId,
        ScheduleRecurrenceType type = ScheduleRecurrenceType.Daily,
        int interval = 1,
        DateTime? nextRunAt = null,
        DateTime? endDate = null,
        bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var now = DateTime.UtcNow;
        var schedule = new WorkflowSchedule
        {
            WorkflowId = workflowId,
            Name = $"Sched-{Guid.NewGuid().ToString()[..6]}",
            RecurrenceType = type,
            RecurrenceInterval = interval,
            DayOfMonth = type is ScheduleRecurrenceType.Monthly or ScheduleRecurrenceType.Quarterly or ScheduleRecurrenceType.Annually ? 1 : null,
            StartDate = now.AddDays(-1),
            EndDate = endDate,
            SubjectTemplate = "Auto {Month} {Year}",
            IsActive = isActive,
            NextRunAt = nextRunAt ?? now.AddHours(-1) // overdue
        };
        db.WorkflowSchedules.Add(schedule);
        db.SaveChanges();
        return schedule;
    }

    [Fact]
    public async Task Scheduler_FiresDueSchedule_CreatesWorkorder()
    {
        var wf = await CreateWorkflowForSchedule();
        var sched = await InsertDueSchedule(wf.Id);
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var count = db.Workorders.Count(w => w.ScheduleId == sched.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Scheduler_FiresDueSchedule_AdvancesNextRunAt()
    {
        var wf = await CreateWorkflowForSchedule();
        var oldNextRun = DateTime.UtcNow.AddHours(-1);
        var sched = await InsertDueSchedule(wf.Id, nextRunAt: oldNextRun);
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var updated = db.WorkflowSchedules.Find(sched.Id)!;
        Assert.NotNull(updated.NextRunAt);
        // NextRunAt should be in the future (based on now, not old NextRunAt)
        Assert.True(updated.NextRunAt!.Value > oldNextRun);
        Assert.NotNull(updated.LastRunAt);
    }

    [Fact]
    public async Task Scheduler_MissedWindow_FiresOnceAndAdvances()
    {
        var wf = await CreateWorkflowForSchedule();
        // Schedule was supposed to fire 3 days ago but wasn't picked up
        var sched = await InsertDueSchedule(wf.Id, type: ScheduleRecurrenceType.Daily,
            interval: 1, nextRunAt: DateTime.UtcNow.AddDays(-3));
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        // Only ONE workorder created (not 3)
        var count = db.Workorders.Count(w => w.ScheduleId == sched.Id);
        Assert.Equal(1, count);
        var updated = db.WorkflowSchedules.Find(sched.Id)!;
        // NextRunAt should be ~1 day from now (not backfilled)
        Assert.True(updated.NextRunAt!.Value > DateTime.UtcNow.AddHours(12));
    }

    [Fact]
    public async Task Scheduler_ExpiredSchedule_DeactivatesAfterFire()
    {
        var wf = await CreateWorkflowForSchedule();
        // EndDate is in the past, so after firing once, schedule should deactivate
        var endDate = DateTime.UtcNow.AddHours(-30); // expired 30h ago
        var sched = await InsertDueSchedule(wf.Id, endDate: endDate,
            nextRunAt: DateTime.UtcNow.AddHours(-31));
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var updated = db.WorkflowSchedules.Find(sched.Id)!;
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Scheduler_InactiveSchedule_NotFired()
    {
        var wf = await CreateWorkflowForSchedule();
        var sched = await InsertDueSchedule(wf.Id, isActive: false,
            nextRunAt: DateTime.UtcNow.AddHours(-1));
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var count = db.Workorders.Count(w => w.ScheduleId == sched.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Scheduler_Hourly_FiresDueSchedule_CreatesWorkorder()
    {
        var wf = await CreateWorkflowForSchedule();
        var sched = await InsertDueSchedule(wf.Id,
            type: ScheduleRecurrenceType.Hourly, interval: 1,
            nextRunAt: DateTime.UtcNow.AddMinutes(-5));
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var count = db.Workorders.Count(w => w.ScheduleId == sched.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Scheduler_Hourly_AdvancesNextRunAtByInterval()
    {
        var wf = await CreateWorkflowForSchedule();
        var sched = await InsertDueSchedule(wf.Id,
            type: ScheduleRecurrenceType.Hourly, interval: 4,
            nextRunAt: DateTime.UtcNow.AddMinutes(-5));
        var scheduler = CreateSchedulerService();

        var beforeFire = DateTime.UtcNow;
        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var updated = db.WorkflowSchedules.Find(sched.Id)!;
        Assert.NotNull(updated.NextRunAt);
        // NextRunAt should be approximately 4 hours from now
        var expectedNext = beforeFire.AddHours(4);
        Assert.True(updated.NextRunAt!.Value >= expectedNext.AddMinutes(-1));
        Assert.True(updated.NextRunAt.Value <= expectedNext.AddMinutes(1));
    }

    [Fact]
    public async Task Scheduler_Hourly_MissedThreeWindows_OnlyOneWorkorderCreated()
    {
        var wf = await CreateWorkflowForSchedule();
        // Schedule was supposed to fire every hour, but missed 3 firings
        var sched = await InsertDueSchedule(wf.Id,
            type: ScheduleRecurrenceType.Hourly, interval: 1,
            nextRunAt: DateTime.UtcNow.AddHours(-3));
        var scheduler = CreateSchedulerService();

        await scheduler.ProcessDueSchedulesAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        // Only 1 workorder created, not 3
        var count = db.Workorders.Count(w => w.ScheduleId == sched.Id);
        Assert.Equal(1, count);

        // NextRunAt should be ~1 hour from now (not 3 hours backfilled)
        var updated = db.WorkflowSchedules.Find(sched.Id)!;
        Assert.True(updated.NextRunAt!.Value > DateTime.UtcNow.AddMinutes(30));
        Assert.True(updated.NextRunAt.Value < DateTime.UtcNow.AddHours(2));
    }
}
