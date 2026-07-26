using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 36.4 (T4.2, T4.4, T4.5) — integration tests for the phase audit
/// trail, cross-device resume, and time-on-phase telemetry endpoints, plus a
/// full end-to-end "example execution" that exercises all of them together.
/// </summary>
public class Phase36_ExecutionHardeningTests : IntegrationTestBase
{
    public Phase36_ExecutionHardeningTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── Helpers ────────────

    private async Task<Guid> StartFirstStep()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);
        return step1.Id;
    }

    private async Task<StepExecutionPhaseEventDto> RecordPhase(Guid seId, ExecutionPhase phase)
    {
        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/phase",
            new RecordPhaseDto(phase), JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<StepExecutionPhaseEventDto>(JsonOptions))!;
    }

    // ──────────── T4.2 — Phase audit & revisit ────────────

    [Fact]
    public async Task RecordPhase_OpensNewEvent_AndClosesPrevious()
    {
        var seId = await StartFirstStep();

        await RecordPhase(seId, ExecutionPhase.Setup);
        await RecordPhase(seId, ExecutionPhase.Safety);

        var history = await Client.GetFromJsonAsync<List<StepExecutionPhaseEventDto>>(
            $"/api/step-executions/{seId}/phase-history", JsonOptions);

        Assert.Equal(2, history!.Count);
        // First (Setup) is now closed; second (Safety) is open.
        Assert.NotNull(history[0].ExitedAt);
        Assert.Equal(ExecutionPhase.Setup, history[0].Phase);
        Assert.Null(history[1].ExitedAt);
        Assert.Equal(ExecutionPhase.Safety, history[1].Phase);
    }

    [Fact]
    public async Task RecordPhase_AllowsRevisitingEarlierPhase()
    {
        var seId = await StartFirstStep();

        await RecordPhase(seId, ExecutionPhase.Setup);
        await RecordPhase(seId, ExecutionPhase.Safety);
        await RecordPhase(seId, ExecutionPhase.Execution);
        // Operator goes BACK to Setup to fix something — allowed pre-signoff.
        await RecordPhase(seId, ExecutionPhase.Setup);

        var history = await Client.GetFromJsonAsync<List<StepExecutionPhaseEventDto>>(
            $"/api/step-executions/{seId}/phase-history", JsonOptions);

        Assert.Equal(4, history!.Count);
        // Setup appears twice — the revisit is recorded, not overwritten.
        Assert.Equal(2, history.Count(h => h.Phase == ExecutionPhase.Setup));
        // The last event is the Setup revisit and is still open.
        Assert.Equal(ExecutionPhase.Setup, history[^1].Phase);
        Assert.Null(history[^1].ExitedAt);
    }

    [Fact]
    public async Task RecordPhase_OnCompletedStep_Returns400()
    {
        var seId = await StartFirstStep();
        await RecordPhase(seId, ExecutionPhase.Setup);

        // Complete the step.
        (await Client.PostAsync($"/api/step-executions/{seId}/complete", null)).EnsureSuccessStatusCode();

        // Now phase history should be frozen.
        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/phase",
            new RecordPhaseDto(ExecutionPhase.Setup), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PhaseHistory_UnknownStep_Returns404()
    {
        var resp = await Client.GetAsync($"/api/step-executions/{Guid.NewGuid()}/phase-history");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ──────────── T4.4 — Resume ────────────

    [Fact]
    public async Task Resume_ReturnsCurrentPhaseAndCounts()
    {
        var seId = await StartFirstStep();
        await RecordPhase(seId, ExecutionPhase.Setup);
        await RecordPhase(seId, ExecutionPhase.Execution);

        var resume = await Client.GetFromJsonAsync<StepExecutionResumeDto>(
            $"/api/step-executions/{seId}/resume", JsonOptions);

        Assert.NotNull(resume);
        Assert.Equal(seId, resume!.StepExecutionId);
        Assert.Equal(ExecutionPhase.Execution, resume.CurrentPhase);
        Assert.True(resume.IsResumable);
        Assert.Equal("InProgress", resume.Status);
    }

    [Fact]
    public async Task Resume_BeforeAnyPhaseRecorded_DefaultsToSetup()
    {
        var seId = await StartFirstStep();

        var resume = await Client.GetFromJsonAsync<StepExecutionResumeDto>(
            $"/api/step-executions/{seId}/resume", JsonOptions);

        Assert.Equal(ExecutionPhase.Setup, resume!.CurrentPhase);
    }

    [Fact]
    public async Task Resume_UnknownStep_Returns404()
    {
        var resp = await Client.GetAsync($"/api/step-executions/{Guid.NewGuid()}/resume");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ──────────── T4.5 — Phase timings ────────────

    [Fact]
    public async Task PhaseTimings_AggregatesAcrossSteps()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Walk through several phases so closed events accumulate.
        await RecordPhase(step1.Id, ExecutionPhase.Setup);
        await RecordPhase(step1.Id, ExecutionPhase.Safety);
        await RecordPhase(step1.Id, ExecutionPhase.Execution);
        await RecordPhase(step1.Id, ExecutionPhase.SignOff);

        var report = await Client.GetFromJsonAsync<PhaseTimingReportDto>(
            $"/api/step-executions/phase-timings?jobId={job.Id}", JsonOptions);

        Assert.NotNull(report);
        // Setup, Safety, Execution are closed (SignOff is still open). Each
        // should appear with one sample.
        Assert.Contains(report!.PerPhase, p => p.Phase == ExecutionPhase.Setup);
        Assert.Contains(report.PerPhase, p => p.Phase == ExecutionPhase.Safety);
        Assert.Contains(report.PerPhase, p => p.Phase == ExecutionPhase.Execution);
        Assert.DoesNotContain(report.PerPhase, p => p.Phase == ExecutionPhase.SignOff);
    }

    [Fact]
    public async Task PhaseTimings_UnknownJob_Returns404()
    {
        var resp = await Client.GetAsync($"/api/step-executions/phase-timings?jobId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ──────────── Example execution — full end-to-end ────────────

    /// <summary>
    /// Builds a complete job and drives a step execution through every phase,
    /// records prompt data, simulates a device switch via the resume endpoint,
    /// revisits an earlier phase, signs off, and verifies the resulting audit
    /// trail and timing telemetry. This is the "example execution" sanity check
    /// that the hardened wizard flow functions end to end.
    /// </summary>
    [Fact]
    public async Task ExampleExecution_FullWizardFlow_FunctionsEndToEnd()
    {
        // 1. Build scenario + job, start the job.
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, name: "Example Execution Job");
        var startJob = await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        startJob.EnsureSuccessStatusCode();

        // 2. Grab the first step execution and start it.
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        (await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null)).EnsureSuccessStatusCode();

        // 3. Add a numeric prompt to the step and walk the phases.
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            new AddPromptBlockDto("Torque", "NumericEntry", true, "Nm", 20, 30), JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var promptBlock = (await promptResp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        await RecordPhase(step1.Id, ExecutionPhase.Setup);
        await RecordPhase(step1.Id, ExecutionPhase.Safety);
        await RecordPhase(step1.Id, ExecutionPhase.Reference);
        await RecordPhase(step1.Id, ExecutionPhase.Execution);

        // 4. Capture data in the Execution phase (offline-style batch).
        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), promptBlock.Id, null, "25.0")
        });
        (await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/prompt-responses/batch", batch, JsonOptions)).EnsureSuccessStatusCode();

        // 5. Simulate device switch — resume should report Execution + 1 saved response.
        var resume = await Client.GetFromJsonAsync<StepExecutionResumeDto>(
            $"/api/step-executions/{step1.Id}/resume", JsonOptions);
        Assert.Equal(ExecutionPhase.Execution, resume!.CurrentPhase);
        Assert.Equal(1, resume.SavedPromptResponseCount);
        Assert.True(resume.IsResumable);

        // 6. Operator revisits Setup to correct something, then returns to Execution.
        await RecordPhase(step1.Id, ExecutionPhase.Setup);
        await RecordPhase(step1.Id, ExecutionPhase.Execution);

        // 7. Sign off and complete.
        await RecordPhase(step1.Id, ExecutionPhase.SignOff);
        (await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null)).EnsureSuccessStatusCode();

        // 8. History is now frozen.
        var frozen = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/phase",
            new RecordPhaseDto(ExecutionPhase.Setup), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, frozen.StatusCode);

        // 9. Verify the full audit trail: 7 phase visits, Setup recorded twice.
        var history = await Client.GetFromJsonAsync<List<StepExecutionPhaseEventDto>>(
            $"/api/step-executions/{step1.Id}/phase-history", JsonOptions);
        Assert.Equal(7, history!.Count);
        Assert.Equal(2, history.Count(h => h.Phase == ExecutionPhase.Setup));
        // Every event except possibly the last should be closed; after Complete,
        // the SignOff event remains open (completion doesn't auto-close it), so
        // at least the first six are closed with measurable durations.
        Assert.True(history.Count(h => h.ExitedAt is not null) >= 6);

        // 10. Verify the saved response persisted and is correct.
        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{step1.Id}/prompt-responses", JsonOptions);
        var saved = Assert.Single(responses!);
        Assert.Equal("25.0", saved.ResponseValue);

        // 11. Telemetry aggregates the closed phases for this job.
        var timings = await Client.GetFromJsonAsync<PhaseTimingReportDto>(
            $"/api/step-executions/phase-timings?jobId={job.Id}", JsonOptions);
        Assert.NotNull(timings);
        Assert.Contains(timings!.PerPhase, p => p.Phase == ExecutionPhase.Setup && p.SampleCount == 2);

        // 12. Final state: step execution is Completed.
        var finalSe = await Client.GetFromJsonAsync<StepExecutionResponseDto>(
            $"/api/step-executions/{step1.Id}", JsonOptions);
        Assert.Equal("Completed", finalSe!.Status);
    }
}
