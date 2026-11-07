using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Characterization tests for <c>/api/reports</c>: they lock in what the
/// job-status-breakdown, step-performance, recent-completions and summary
/// counters return today. Tests in this class share one database, so they
/// compare before/after values or use uniquely named data rather than
/// asserting absolute totals.
/// </summary>
public class ReportsCharacterizationTests : IntegrationTestBase
{
    public ReportsCharacterizationTests(TestWebApplicationFactory factory) : base(factory) { }

    private static string Pfx() => Guid.NewGuid().ToString()[..6];

    private async Task<JobResponseDto> CreateJobOnEmptyProcess(string? name = null)
    {
        var pfx = Pfx();
        var process = await CreateProcess($"RPC-{pfx}", $"Report Proc {pfx}");
        return await CreateJob(process.Id, name: name ?? $"Report Job {pfx}");
    }

    private async Task Post(string url) =>
        (await Client.PostAsync(url, null)).EnsureSuccessStatusCode();

    private async Task<List<StepExecutionResponseDto>> GetSteps(Guid jobId) =>
        (await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{jobId}/step-executions", JsonOptions))!
        .OrderBy(s => s.Sequence).ToList();

    private async Task<Dictionary<string, int>> GetBreakdown() =>
        (await Client.GetFromJsonAsync<List<JobStatusBreakdownDto>>(
            "/api/reports/job-status-breakdown", JsonOptions))!
        .ToDictionary(r => r.Status, r => r.Count);

    private Task<ReportSummaryDto?> GetSummary() =>
        Client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary", JsonOptions);

    private Task<List<RecentCompletionDto>?> GetRecent(int? count = null) =>
        Client.GetFromJsonAsync<List<RecentCompletionDto>>(
            count is null ? "/api/reports/recent-completions" : $"/api/reports/recent-completions?count={count}",
            JsonOptions);

    // ───── job-status-breakdown ─────

    [Fact]
    public async Task JobStatusBreakdown_CountsJobsPerStatusName()
    {
        var before = await GetBreakdown();

        await CreateJobOnEmptyProcess();                       // Created
        var started = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{started.Id}/start");           // InProgress
        var held = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{held.Id}/start");
        await Post($"/api/jobs/{held.Id}/hold");               // OnHold

        var after = await GetBreakdown();

        Assert.Equal(before.GetValueOrDefault("Created") + 1, after["Created"]);
        Assert.Equal(before.GetValueOrDefault("InProgress") + 1, after["InProgress"]);
        Assert.Equal(before.GetValueOrDefault("OnHold") + 1, after["OnHold"]);
    }

    [Fact]
    public async Task JobStatusBreakdown_IsOrderedByStatusName()
    {
        var started = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{started.Id}/start");
        await CreateJobOnEmptyProcess();

        var rows = (await Client.GetFromJsonAsync<List<JobStatusBreakdownDto>>(
            "/api/reports/job-status-breakdown", JsonOptions))!;

        Assert.True(rows.Count >= 2);
        Assert.Equal(rows.Select(r => r.Status).OrderBy(s => s), rows.Select(r => r.Status));
        Assert.Equal(rows.Count, rows.Select(r => r.Status).Distinct().Count());
    }

    // ───── step-performance ─────

    [Fact]
    public async Task StepPerformance_CountsExecutionsPerStepTemplateName()
    {
        var pfx = Pfx();
        var (kind, grade) = await CreateKindWithGrade($"RPK-{pfx}", "Report Kind");
        var doneName = $"Done Step {pfx}";
        var failName = $"Fail Step {pfx}";
        var skipName = $"Skip Step {pfx}";
        var done = await CreateTransformStep($"RPD-{pfx}", doneName, kind.Id, grade.Id, kind.Id, grade.Id);
        var fail = await CreateTransformStep($"RPF-{pfx}", failName, kind.Id, grade.Id, kind.Id, grade.Id);
        var skip = await CreateTransformStep($"RPS-{pfx}", skipName, kind.Id, grade.Id, kind.Id, grade.Id);

        var process = await CreateProcess($"RPP-{pfx}", "Report Step Process");
        await AddProcessStep(process.Id, done.Id, 1);
        await AddProcessStep(process.Id, fail.Id, 2);
        await AddProcessStep(process.Id, skip.Id, 3);

        // Job 1: step 1 completed, step 2 failed, step 3 skipped.
        var job1 = await CreateJob(process.Id);
        await Post($"/api/jobs/{job1.Id}/start");
        var steps1 = await GetSteps(job1.Id);
        await Post($"/api/step-executions/{steps1[0].Id}/start");
        await Post($"/api/step-executions/{steps1[0].Id}/complete");
        await Post($"/api/step-executions/{steps1[1].Id}/start");
        await Post($"/api/step-executions/{steps1[1].Id}/fail");
        await Post($"/api/step-executions/{steps1[2].Id}/skip");

        // Job 2: step 1 in progress, steps 2 and 3 pending.
        var job2 = await CreateJob(process.Id);
        await Post($"/api/jobs/{job2.Id}/start");
        var steps2 = await GetSteps(job2.Id);
        await Post($"/api/step-executions/{steps2[0].Id}/start");

        var rows = (await Client.GetFromJsonAsync<List<StepPerformanceDto>>(
            "/api/reports/step-performance", JsonOptions))!;

        // Every execution counts towards Total, whatever its status.
        var doneRow = Assert.Single(rows, r => r.StepName == doneName);
        Assert.Equal(2, doneRow.Total);
        Assert.Equal(1, doneRow.Completed);
        Assert.Equal(0, doneRow.Failed);
        // Only executions with both StartedAt and CompletedAt are timed;
        // the in-progress one is left out.
        Assert.NotNull(doneRow.AvgDurationMinutes);
        Assert.True(doneRow.AvgDurationMinutes >= 0);

        // Fail sets CompletedAt, so a failed execution is timed too.
        var failRow = Assert.Single(rows, r => r.StepName == failName);
        Assert.Equal(2, failRow.Total);
        Assert.Equal(0, failRow.Completed);
        Assert.Equal(1, failRow.Failed);
        Assert.NotNull(failRow.AvgDurationMinutes);

        // Skipped and pending executions are neither Completed nor Failed,
        // and have no timestamps, so there is no average.
        var skipRow = Assert.Single(rows, r => r.StepName == skipName);
        Assert.Equal(2, skipRow.Total);
        Assert.Equal(0, skipRow.Completed);
        Assert.Equal(0, skipRow.Failed);
        Assert.Null(skipRow.AvgDurationMinutes);

        Assert.Equal(rows.Select(r => r.Total).OrderByDescending(t => t), rows.Select(r => r.Total));
    }

    // ───── recent-completions ─────

    [Fact]
    public async Task RecentCompletions_ReturnsCompletedJobsNewestFirst()
    {
        var first = await CreateJobOnEmptyProcess($"First {Pfx()}");
        await Post($"/api/jobs/{first.Id}/start");
        await Post($"/api/jobs/{first.Id}/complete");

        var second = await CreateJobOnEmptyProcess($"Second {Pfx()}");
        await Post($"/api/jobs/{second.Id}/start");
        await Post($"/api/jobs/{second.Id}/complete");

        var rows = (await GetRecent(2))!;

        Assert.Equal(new[] { second.Id, first.Id }, rows.Select(r => r.JobId));

        var top = rows[0];
        Assert.Equal(second.Code, top.Code);
        Assert.Equal(second.Name, top.Name);
        Assert.Equal(second.ProcessName, top.ProcessName);
        Assert.NotNull(top.StartedAt);
        Assert.True(top.CompletedAt >= top.StartedAt);
        Assert.NotNull(top.DurationHours);
        Assert.Equal((top.CompletedAt - top.StartedAt!.Value).TotalHours, top.DurationHours!.Value, 6);
    }

    [Fact]
    public async Task RecentCompletions_ExcludesJobsThatAreNotCompleted()
    {
        var cancelled = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{cancelled.Id}/start");
        await Post($"/api/jobs/{cancelled.Id}/cancel");    // also sets CompletedAt

        var inProgress = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{inProgress.Id}/start");

        var rows = (await GetRecent(1000))!;

        Assert.DoesNotContain(rows, r => r.JobId == cancelled.Id);
        Assert.DoesNotContain(rows, r => r.JobId == inProgress.Id);
    }

    [Fact]
    public async Task RecentCompletions_DefaultsToTenRows()
    {
        for (var i = 0; i < 11; i++)
        {
            var job = await CreateJobOnEmptyProcess();
            await Post($"/api/jobs/{job.Id}/start");
            await Post($"/api/jobs/{job.Id}/complete");
        }

        var rows = (await GetRecent())!;

        Assert.Equal(10, rows.Count);
    }

    // ───── summary counters ─────
    // AvgJobDurationHours is left to ReportTests (PR #32), which changes it.

    [Fact]
    public async Task Summary_CountsTotalActiveCompletedAndFailedSteps()
    {
        var pfx = Pfx();
        var (kind, grade) = await CreateKindWithGrade($"RSK-{pfx}", "Summary Kind");
        var tmpl = await CreateTransformStep($"RST-{pfx}", $"Summary Step {pfx}", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"RSP-{pfx}", "Summary Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var before = (await GetSummary())!;

        // Active (InProgress) job with one failed step.
        var active = await CreateJob(process.Id);
        await Post($"/api/jobs/{active.Id}/start");
        var step = (await GetSteps(active.Id))[0];
        await Post($"/api/step-executions/{step.Id}/start");
        await Post($"/api/step-executions/{step.Id}/fail");

        // Held job: counts towards Total but not Active.
        var held = await CreateJob(process.Id);
        await Post($"/api/jobs/{held.Id}/start");
        await Post($"/api/jobs/{held.Id}/hold");

        // Completed job (no steps, so it can complete straight away).
        var completed = await CreateJobOnEmptyProcess();
        await Post($"/api/jobs/{completed.Id}/start");
        await Post($"/api/jobs/{completed.Id}/complete");

        var after = (await GetSummary())!;

        Assert.Equal(before.TotalJobs + 3, after.TotalJobs);
        Assert.Equal(before.ActiveJobs + 1, after.ActiveJobs);
        Assert.Equal(before.CompletedThisMonth + 1, after.CompletedThisMonth);
        Assert.Equal(before.FailedStepsAllTime + 1, after.FailedStepsAllTime);
    }
}
