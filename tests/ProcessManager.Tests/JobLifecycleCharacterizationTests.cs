using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Characterization tests for the Job and StepExecution lifecycle guards.
/// They lock in the current status-transition rules (which transitions are
/// allowed, which return 400, and which timestamps are set) so that changes
/// to them are deliberate. Behaviours that docs/agent/BACKLOG.md flags as open
/// questions (for example, step transitions on a cancelled Job) are not covered.
/// </summary>
public class JobLifecycleCharacterizationTests : IntegrationTestBase
{
    public JobLifecycleCharacterizationTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<JobResponseDto> NewJob()
    {
        var scenario = await BuildWidgetFinishingScenario();
        return await CreateJob(scenario.Process.Id);
    }

    private Task<HttpResponseMessage> JobAction(Guid jobId, string action) =>
        Client.PostAsync($"/api/jobs/{jobId}/{action}", null);

    private Task<HttpResponseMessage> StepAction(Guid stepId, string action) =>
        Client.PostAsync($"/api/step-executions/{stepId}/{action}", null);

    private async Task<JobResponseDto> GetJob(Guid jobId) =>
        (await Client.GetFromJsonAsync<JobResponseDto>($"/api/jobs/{jobId}", JsonOptions))!;

    private async Task<List<StepExecutionResponseDto>> GetSteps(Guid jobId) =>
        (await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{jobId}/step-executions", JsonOptions))!.OrderBy(s => s.Sequence).ToList();

    private async Task Ok(Task<HttpResponseMessage> call) => (await call).EnsureSuccessStatusCode();

    // ───── Job transitions that are rejected ─────

    [Theory]
    [InlineData("hold")]      // must be InProgress
    [InlineData("resume")]    // must be OnHold
    [InlineData("complete")]  // must be InProgress
    public async Task CreatedJob_RejectsTransition(string action)
    {
        var job = await NewJob();

        var response = await JobAction(job.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Created", (await GetJob(job.Id)).Status);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("resume")]
    public async Task InProgressJob_RejectsTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));

        var response = await JobAction(job.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InProgress", (await GetJob(job.Id)).Status);
    }

    [Theory]
    [InlineData("hold")]
    [InlineData("complete")]
    public async Task OnHoldJob_RejectsTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        await Ok(JobAction(job.Id, "hold"));

        var response = await JobAction(job.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("OnHold", (await GetJob(job.Id)).Status);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("hold")]
    [InlineData("resume")]
    [InlineData("complete")]
    [InlineData("cancel")]
    public async Task CancelledJob_RejectsEveryTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "cancel"));

        var response = await JobAction(job.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Cancelled", (await GetJob(job.Id)).Status);
    }

    [Fact]
    public async Task CancelledJob_RejectsMetadataUpdate()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "cancel"));

        var response = await Client.PutAsJsonAsync($"/api/jobs/{job.Id}",
            new UpdateJobDto("Renamed", null, 1), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(job.Name, (await GetJob(job.Id)).Name);
    }

    // ───── Job transitions that are allowed ─────

    [Fact]
    public async Task Cancel_CreatedJob_SetsCompletedAtWithoutStartedAt()
    {
        var job = await NewJob();

        var response = await JobAction(job.Id, "cancel");
        response.EnsureSuccessStatusCode();

        var cancelled = (await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Null(cancelled.StartedAt);
        Assert.NotNull(cancelled.CompletedAt);
    }

    [Fact]
    public async Task Cancel_OnHoldJob_Succeeds()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        await Ok(JobAction(job.Id, "hold"));

        var response = await JobAction(job.Id, "cancel");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Cancelled", (await GetJob(job.Id)).Status);
    }

    [Fact]
    public async Task Start_OnHoldJob_ResumesAndKeepsOriginalStartedAt()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var firstStart = (await GetJob(job.Id)).StartedAt;
        await Ok(JobAction(job.Id, "hold"));

        var response = await JobAction(job.Id, "start");
        response.EnsureSuccessStatusCode();

        var restarted = (await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
        Assert.Equal("InProgress", restarted.Status);
        Assert.NotNull(firstStart);
        Assert.Equal(firstStart, restarted.StartedAt);
    }

    [Fact]
    public async Task Complete_WithSkippedAndCompletedSteps_Succeeds()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var steps = await GetSteps(job.Id);
        await Ok(StepAction(steps[0].Id, "skip"));
        await Ok(StepAction(steps[1].Id, "start"));
        await Ok(StepAction(steps[1].Id, "complete"));

        var response = await JobAction(job.Id, "complete");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Completed", (await GetJob(job.Id)).Status);
    }

    [Fact]
    public async Task Complete_WithFailedStep_ReturnsBadRequestNamingTheStep()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var steps = await GetSteps(job.Id);
        await Ok(StepAction(steps[0].Id, "start"));
        await Ok(StepAction(steps[0].Id, "complete"));
        await Ok(StepAction(steps[1].Id, "start"));
        await Ok(StepAction(steps[1].Id, "fail"));

        var response = await JobAction(job.Id, "complete");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Step 2 (Failed)", await response.Content.ReadAsStringAsync());
        Assert.Equal("InProgress", (await GetJob(job.Id)).Status);
    }

    // ───── StepExecution transitions ─────

    [Theory]
    [InlineData("complete")]  // must be InProgress
    [InlineData("fail")]      // must be InProgress
    public async Task PendingStep_RejectsTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var step = (await GetSteps(job.Id))[0];

        var response = await StepAction(step.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Pending", (await GetSteps(job.Id))[0].Status);
    }

    [Theory]
    [InlineData("start")]     // must be Pending
    [InlineData("skip")]      // must be Pending
    public async Task InProgressStep_RejectsTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var step = (await GetSteps(job.Id))[0];
        await Ok(StepAction(step.Id, "start"));

        var response = await StepAction(step.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InProgress", (await GetSteps(job.Id))[0].Status);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("complete")]
    [InlineData("skip")]
    [InlineData("fail")]
    public async Task FailedStep_RejectsEveryTransition(string action)
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var step = (await GetSteps(job.Id))[0];
        await Ok(StepAction(step.Id, "start"));
        await Ok(StepAction(step.Id, "fail"));

        var response = await StepAction(step.Id, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Failed", (await GetSteps(job.Id))[0].Status);
    }

    [Fact]
    public async Task Start_SecondStep_AfterFirstSkipped_Succeeds()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var steps = await GetSteps(job.Id);
        await Ok(StepAction(steps[0].Id, "skip"));

        var response = await StepAction(steps[1].Id, "start");

        response.EnsureSuccessStatusCode();
        var started = (await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions))!;
        Assert.Equal("InProgress", started.Status);
    }

    [Fact]
    public async Task Start_SecondStep_AfterFirstFailed_ReturnsBadRequest()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var steps = await GetSteps(job.Id);
        await Ok(StepAction(steps[0].Id, "start"));
        await Ok(StepAction(steps[0].Id, "fail"));

        var response = await StepAction(steps[1].Id, "start");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Pending", (await GetSteps(job.Id))[1].Status);
    }

    [Fact]
    public async Task Fail_SetsCompletedAt_AndSkipLeavesTimestampsEmpty()
    {
        var job = await NewJob();
        await Ok(JobAction(job.Id, "start"));
        var steps = await GetSteps(job.Id);
        await Ok(StepAction(steps[0].Id, "start"));
        await Ok(StepAction(steps[0].Id, "fail"));

        var failed = (await GetSteps(job.Id))[0];
        Assert.NotNull(failed.StartedAt);
        Assert.NotNull(failed.CompletedAt);

        // A second job, so the skip isn't affected by the failed step above.
        var job2 = await NewJob();
        await Ok(JobAction(job2.Id, "start"));
        var step = (await GetSteps(job2.Id))[0];
        await Ok(StepAction(step.Id, "skip"));

        var skipped = (await GetSteps(job2.Id))[0];
        Assert.Null(skipped.StartedAt);
        Assert.Null(skipped.CompletedAt);
    }
}
