using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class ReportTests : IntegrationTestBase
{
    public ReportTests(TestWebApplicationFactory factory) : base(factory) { }

    // Cancel also sets Job.CompletedAt (docs/data-model.md), so the reports
    // must filter on Status, not on CompletedAt alone.

    private async Task<JobResponseDto> CreateStartedJob()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var process = await CreateProcess($"RPT-{pfx}", "Report Process");
        var job = await CreateJob(process.Id);
        (await Client.PostAsync($"/api/jobs/{job.Id}/start", null)).EnsureSuccessStatusCode();
        return job;
    }

    private Task<List<ThroughputPointDto>?> GetThroughput() =>
        Client.GetFromJsonAsync<List<ThroughputPointDto>>("/api/reports/throughput?days=7", JsonOptions);

    [Fact]
    public async Task Throughput_DoesNotCountCancelledJobsAsCompleted()
    {
        var before = await GetThroughput();

        var job = await CreateStartedJob();
        (await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null)).EnsureSuccessStatusCode();

        var after = await GetThroughput();

        Assert.Equal(before!.Sum(p => p.Created) + 1, after!.Sum(p => p.Created));
        Assert.Equal(before.Sum(p => p.Completed), after.Sum(p => p.Completed));
    }

    [Fact]
    public async Task Throughput_CountsCompletedJobs()
    {
        var before = await GetThroughput();

        var job = await CreateStartedJob();
        (await Client.PostAsync($"/api/jobs/{job.Id}/complete", null)).EnsureSuccessStatusCode();

        var after = await GetThroughput();

        Assert.Equal(before!.Sum(p => p.Completed) + 1, after!.Sum(p => p.Completed));
    }

    [Fact]
    public async Task Summary_AvgJobDuration_IgnoresCancelledJobs()
    {
        var before = await Client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary", JsonOptions);

        var job = await CreateStartedJob();
        (await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null)).EnsureSuccessStatusCode();

        var after = await Client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary", JsonOptions);

        Assert.Equal(before!.AvgJobDurationHours, after!.AvgJobDurationHours);
    }
}
