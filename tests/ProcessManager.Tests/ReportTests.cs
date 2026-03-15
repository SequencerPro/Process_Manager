using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class ReportTests : IntegrationTestBase
{
    public ReportTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────────────── helpers ────────────────────

    /// <summary>
    /// Creates a job, runs it to completion (all steps started + completed, then job completed),
    /// and returns the completed JobResponseDto.
    /// </summary>
    private async Task<JobResponseDto> CreateAndCompleteJob(Guid processId, string? code = null)
    {
        code ??= $"JOB-{Guid.NewGuid().ToString()[..6]}";
        var job = await CreateJob(processId, code);

        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        foreach (var se in executions!.OrderBy(s => s.Sequence))
        {
            await Client.PostAsync($"/api/step-executions/{se.Id}/start", null);
            await Client.PostAsync($"/api/step-executions/{se.Id}/complete", null);
        }

        var completeResp = await Client.PostAsync($"/api/jobs/{job.Id}/complete", null);
        completeResp.EnsureSuccessStatusCode();
        return (await completeResp.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
    }

    // ──────────────────── process-timing ────────────────────

    [Fact]
    public async Task GetProcessTiming_CompletedJobExists_ReturnsProcessEntry()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing", JsonOptions);

        Assert.NotNull(result);
        var entry = result!.FirstOrDefault(r => r.ProcessId == scenario.Process.Id);
        Assert.NotNull(entry);
        Assert.Equal(scenario.Process.Code, entry!.Code);
        Assert.Equal(1, entry.CompletedJobs);
        Assert.NotNull(entry.MinHours);
        Assert.NotNull(entry.AvgHours);
        Assert.NotNull(entry.MaxHours);
    }

    [Fact]
    public async Task GetProcessTiming_CompletedJobExists_ReturnsStepBreakdown()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing", JsonOptions);

        var entry = result!.First(r => r.ProcessId == scenario.Process.Id);
        // Widget Finishing Scenario has 2 steps
        Assert.Equal(2, entry.Steps.Count);
        // Steps ordered by sequence
        Assert.Equal(1, entry.Steps[0].Sequence);
        Assert.Equal(2, entry.Steps[1].Sequence);
        // Each step should have timing stats
        Assert.All(entry.Steps, s =>
        {
            Assert.Equal(1, s.CompletedExecutions);
            Assert.NotNull(s.MinMinutes);
            Assert.NotNull(s.AvgMinutes);
            Assert.NotNull(s.MaxMinutes);
        });
    }

    [Fact]
    public async Task GetProcessTiming_JobNotCompleted_ProcessNotInResult()
    {
        var scenario = await BuildWidgetFinishingScenario();
        // Create a job but do NOT complete it — only start it
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing", JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(result!, r => r.ProcessId == scenario.Process.Id);
    }

    [Fact]
    public async Task GetProcessTiming_MultipleCompletedJobs_AggregatesCorrectly()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);
        await CreateAndCompleteJob(scenario.Process.Id);
        await CreateAndCompleteJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing", JsonOptions);

        var entry = result!.First(r => r.ProcessId == scenario.Process.Id);
        Assert.Equal(3, entry.CompletedJobs);
        // Min should be ≤ Avg ≤ Max
        Assert.True(entry.MinHours <= entry.AvgHours);
        Assert.True(entry.AvgHours <= entry.MaxHours);
    }

    [Fact]
    public async Task GetProcessTiming_ProcessRoleFilter_ReturnsOnlyMatchingRole()
    {
        // Create a standard Manufacturing process and complete a job
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);

        // Training role filter should NOT include our ManufacturingProcess
        var trainingResult = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing?processRole=Training", JsonOptions);

        Assert.NotNull(trainingResult);
        Assert.DoesNotContain(trainingResult!, r => r.ProcessId == scenario.Process.Id);
    }

    [Fact]
    public async Task GetProcessTiming_ManufacturingRoleFilter_IncludesManufacturingProcess()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing?processRole=ManufacturingProcess", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!, r => r.ProcessId == scenario.Process.Id);
    }

    [Fact]
    public async Task GetProcessTiming_InvalidRoleFilter_ReturnsAllProcesses()
    {
        // An unrecognised processRole value is silently ignored — returns all roles
        var scenario = await BuildWidgetFinishingScenario();
        await CreateAndCompleteJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<List<ProcessTimingDto>>(
            "/api/reports/process-timing?processRole=NotARealRole", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!, r => r.ProcessId == scenario.Process.Id);
    }

    // ──────────────────── documentRolesOnly filter ────────────────────

    [Fact]
    public async Task GetProcesses_DocumentRolesOnly_IncludesTrainingProcesses()
    {
        var code = $"TRN-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var createDto = new ProcessCreateDto(code, "Test Training Doc", null, "Training");
        var createResp = await Client.PostAsJsonAsync("/api/processes", createDto, JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var process = (await createResp.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions))!;

        // Use processRole filter + large page to ensure our specific process is visible
        var result = await Client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            $"/api/processes?documentRolesOnly=true&processRole=Training&pageSize=100", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Id == process.Id);
    }

    [Fact]
    public async Task GetProcesses_DocumentRolesOnly_IncludesQmsDocumentProcesses()
    {
        var code = $"QMS-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var createDto = new ProcessCreateDto(code, "Test QMS Doc", null, "QmsDocument");
        var createResp = await Client.PostAsJsonAsync("/api/processes", createDto, JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var process = (await createResp.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions))!;

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            $"/api/processes?processRole=QmsDocument&pageSize=100", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Id == process.Id);
    }

    [Fact]
    public async Task GetProcesses_DocumentRolesOnly_ExcludesApprovalProcessRole()
    {
        var code = $"APR-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var createDto = new ProcessCreateDto(code, "Test Approval Process", null, "ApprovalProcess");
        var createResp = await Client.PostAsJsonAsync("/api/processes", createDto, JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var process = (await createResp.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions))!;

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            "/api/processes?documentRolesOnly=true&pageSize=200", JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(result!.Items, p => p.Id == process.Id);
    }
}
