using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class JobTests : IntegrationTestBase
{
    public JobTests(TestWebApplicationFactory factory) : base(factory) { }

    // ───── Job CRUD ─────

    [Fact]
    public async Task Create_Job_ReturnsCreatedWithStepExecutions()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var job = await CreateJob(scenario.Process.Id, "JOB-001", "Widget Run #1");

        Assert.Equal("JOB-001", job.Code);
        Assert.Equal("Created", job.Status);
        Assert.Equal(scenario.Process.Id, job.ProcessId);
        Assert.NotNull(job.StepExecutions);
        Assert.Equal(2, job.StepExecutions!.Count);
        Assert.All(job.StepExecutions, se => Assert.Equal("Pending", se.Status));
        Assert.Equal(1, job.StepExecutions![0].Sequence);
        Assert.Equal(2, job.StepExecutions![1].Sequence);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateJob(scenario.Process.Id, "JOB-DUP");

        var dto = new CreateJobDto("JOB-DUP", "Dup", null, scenario.Process.Id);
        var response = await Client.PostAsJsonAsync("/api/jobs", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidProcess_ReturnsBadRequest()
    {
        var dto = new CreateJobDto("JOB-X", "Bad", null, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/jobs", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsJobWithStepExecutions()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var response = await Client.GetFromJsonAsync<JobResponseDto>($"/api/jobs/{job.Id}", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(job.Id, response!.Id);
        Assert.NotNull(response.StepExecutions);
        Assert.Equal(2, response.StepExecutions!.Count);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFiltered()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job1 = await CreateJob(scenario.Process.Id, "JOB-F1");
        var job2 = await CreateJob(scenario.Process.Id, "JOB-F2");

        // Start job2
        await Client.PostAsync($"/api/jobs/{job2.Id}/start", null);

        var created = await Client.GetFromJsonAsync<PaginatedResponse<JobResponseDto>>("/api/jobs?status=Created", JsonOptions);
        var inProgress = await Client.GetFromJsonAsync<PaginatedResponse<JobResponseDto>>("/api/jobs?status=InProgress", JsonOptions);

        Assert.Contains(created!.Items, j => j.Code == "JOB-F1");
        Assert.DoesNotContain(created!.Items, j => j.Code == "JOB-F2");
        Assert.Contains(inProgress!.Items, j => j.Code == "JOB-F2");
        Assert.DoesNotContain(inProgress!.Items, j => j.Code == "JOB-F1");
    }

    [Fact]
    public async Task Update_Job_ChangesMetadata()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var dto = new UpdateJobDto("Updated Name", "New description", 5);
        var response = await Client.PutAsJsonAsync($"/api/jobs/{job.Id}", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(5, updated.Priority);
    }

    [Fact]
    public async Task Delete_CreatedJob_ReturnsNoContent()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var response = await Client.DeleteAsync($"/api/jobs/{job.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await Client.GetAsync($"/api/jobs/{job.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_InProgressJob_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var response = await Client.DeleteAsync($"/api/jobs/{job.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ───── Job Lifecycle ─────

    [Fact]
    public async Task Start_CreatedJob_TransitionsToInProgress()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var response = await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("InProgress", updated!.Status);
        Assert.NotNull(updated.StartedAt);
    }

    [Fact]
    public async Task Complete_AllStepsDone_TransitionsToCompleted()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // Start job
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        // Start and complete both steps
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        foreach (var se in executions!.OrderBy(s => s.Sequence))
        {
            await Client.PostAsync($"/api/step-executions/{se.Id}/start", null);
            await Client.PostAsync($"/api/step-executions/{se.Id}/complete", null);
        }

        // Complete job
        var response = await Client.PostAsync($"/api/jobs/{job.Id}/complete", null);
        response.EnsureSuccessStatusCode();

        var completed = await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("Completed", completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task Complete_IncompleteSteps_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        // Try to complete without finishing steps
        var response = await Client.PostAsync($"/api/jobs/{job.Id}/complete", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_InProgressJob_TransitionsToCancelled()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var response = await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null);
        response.EnsureSuccessStatusCode();

        var cancelled = await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    [Fact]
    public async Task Hold_Resume_Cycle()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        // Hold
        var holdResponse = await Client.PostAsync($"/api/jobs/{job.Id}/hold", null);
        holdResponse.EnsureSuccessStatusCode();
        var held = await holdResponse.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("OnHold", held!.Status);

        // Resume
        var resumeResponse = await Client.PostAsync($"/api/jobs/{job.Id}/resume", null);
        resumeResponse.EnsureSuccessStatusCode();
        var resumed = await resumeResponse.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions);
        Assert.Equal("InProgress", resumed!.Status);
    }
}
