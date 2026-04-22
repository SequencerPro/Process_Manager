using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class ExecutionWizardUxTests : IntegrationTestBase
{
    public ExecutionWizardUxTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid StepExecutionId, Guid ContentBlockId)> SetupStepWithNumericPrompt(
        decimal? min = null, decimal? max = null)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var promptDto = new AddPromptBlockDto("Torque", "NumericEntry", true, "Nm", min, max);
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            promptDto, JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var contentBlock = await promptResp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions);

        return (step1.Id, contentBlock!.Id);
    }

    // ───── Batch Prompt Responses — Idempotency ─────

    [Fact]
    public async Task BatchPromptResponses_IdempotentOnRetry()
    {
        var (seId, blockId) = await SetupStepWithNumericPrompt(20, 30);

        var clientId = Guid.NewGuid().ToString();
        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(clientId, blockId, null, "25.0")
        });

        var resp1 = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        resp1.EnsureSuccessStatusCode();

        var resp2 = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        resp2.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.Where(r => r.ProcessStepContentId == blockId).ToList();
        Assert.Single(matching);
        Assert.Equal("25.0", matching[0].ResponseValue);
    }

    [Fact]
    public async Task BatchPromptResponses_ValidatesAgainstLimits()
    {
        var (seId, blockId) = await SetupStepWithNumericPrompt(20, 30);

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), blockId, null, "35.0")
        });

        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.First(r => r.ProcessStepContentId == blockId);
        Assert.True(matching.IsOutOfRange);
    }

    [Fact]
    public async Task BatchPromptResponses_MultipleItemsInSingleBatch()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var prompt1 = new AddPromptBlockDto("Torque", "NumericEntry", true, "Nm", 20, 30);
        var resp1 = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            prompt1, JsonOptions);
        var block1 = (await resp1.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var prompt2 = new AddPromptBlockDto("Angle", "NumericEntry", true, "deg", 0, 360);
        var resp2 = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            prompt2, JsonOptions);
        var block2 = (await resp2.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), block1.Id, null, "25.0"),
            new(Guid.NewGuid().ToString(), block2.Id, null, "180.0")
        });

        var batchResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/prompt-responses/batch", batch, JsonOptions);
        batchResp.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{step1.Id}/prompt-responses", JsonOptions);

        Assert.Equal(2, responses!.Count);
    }

    [Fact]
    public async Task BatchPromptResponses_RequiresContentBlockReference()
    {
        var (seId, _) = await SetupStepWithNumericPrompt();

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), null, null, "25.0")
        });

        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task BatchPromptResponses_NonexistentStepExecution_Returns404()
    {
        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), Guid.NewGuid(), null, "25.0")
        });

        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{Guid.NewGuid()}/prompt-responses/batch", batch, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task BatchPromptResponses_EmptyBatch_ReturnsNoContent()
    {
        var (seId, _) = await SetupStepWithNumericPrompt();

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>());
        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task BatchPromptResponses_DuplicateClientIdInSameBatch_OnlyWritesOnce()
    {
        var (seId, blockId) = await SetupStepWithNumericPrompt(20, 30);

        var sharedClientId = Guid.NewGuid().ToString();

        var firstBatch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(sharedClientId, blockId, null, "22.0")
        });
        await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", firstBatch, JsonOptions);

        var secondBatch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(sharedClientId, blockId, null, "28.0")
        });
        await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", secondBatch, JsonOptions);

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);
        var matching = responses!.Where(r => r.ProcessStepContentId == blockId).ToList();
        Assert.Single(matching);
        Assert.Equal("22.0", matching[0].ResponseValue);
    }

    [Fact]
    public async Task BatchPromptResponses_InRangeValue_NotFlaggedOutOfRange()
    {
        var (seId, blockId) = await SetupStepWithNumericPrompt(20, 30);

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), blockId, null, "25.0")
        });

        await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);
        var matching = responses!.First(r => r.ProcessStepContentId == blockId);
        Assert.False(matching.IsOutOfRange);
    }
}
