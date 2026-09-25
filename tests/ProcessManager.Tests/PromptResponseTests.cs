using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class PromptResponseTests : IntegrationTestBase
{
    public PromptResponseTests(TestWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// Builds a running job whose first step has a NumericEntry prompt (range 1–10)
    /// and returns the first step execution and the prompt block.
    /// </summary>
    private async Task<(StepExecutionResponseDto Execution, ProcessStepContentResponseDto Prompt)> SetupStepWithPrompt()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var promptResponse = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            new AddPromptBlockDto("Torque", "NumericEntry", MinValue: 1m, MaxValue: 10m), JsonOptions);
        promptResponse.EnsureSuccessStatusCode();
        var prompt = (await promptResponse.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        return (executions!.First(se => se.Sequence == 1), prompt);
    }

    private Task<HttpResponseMessage> Save(Guid executionId, params PromptResponseItemDto[] items) =>
        Client.PostAsJsonAsync($"/api/step-executions/{executionId}/prompt-responses",
            new SavePromptResponsesDto(items.ToList()), JsonOptions);

    private async Task<List<PromptResponseDto>> GetResponses(Guid executionId) =>
        (await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{executionId}/prompt-responses", JsonOptions))!;

    [Fact]
    public async Task Save_NewResponse_IsReturnedWithPromptLabel()
    {
        var (se, prompt) = await SetupStepWithPrompt();

        var response = await Save(se.Id, new PromptResponseItemDto(prompt.Id, null, "5"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal("5", saved.ResponseValue);
        Assert.Equal("Torque", saved.Label);
        Assert.Equal("NumericEntry", saved.PromptType);
        Assert.False(saved.IsOutOfRange);
    }

    [Theory]
    [InlineData("0.5", true)]
    [InlineData("1", false)]
    [InlineData("10", false)]
    [InlineData("10.01", true)]
    [InlineData("not a number", false)]
    public async Task Save_NumericEntry_FlagsOutOfRange(string value, bool expectedOutOfRange)
    {
        var (se, prompt) = await SetupStepWithPrompt();

        (await Save(se.Id, new PromptResponseItemDto(prompt.Id, null, value))).EnsureSuccessStatusCode();

        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal(expectedOutOfRange, saved.IsOutOfRange);
    }

    [Fact]
    public async Task Save_SamePromptInSeparateRequests_UpdatesExistingResponse()
    {
        var (se, prompt) = await SetupStepWithPrompt();

        (await Save(se.Id, new PromptResponseItemDto(prompt.Id, null, "5"))).EnsureSuccessStatusCode();
        (await Save(se.Id, new PromptResponseItemDto(prompt.Id, null, "50", "Operator override"))).EnsureSuccessStatusCode();

        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal("50", saved.ResponseValue);
        Assert.True(saved.IsOutOfRange);
        Assert.Equal("Operator override", saved.OverrideNote);
    }

    [Fact]
    public async Task Save_SamePromptTwiceInOneRequest_KeepsSingleResponseWithLastValue()
    {
        var (se, prompt) = await SetupStepWithPrompt();

        (await Save(se.Id,
            new PromptResponseItemDto(prompt.Id, null, "5"),
            new PromptResponseItemDto(prompt.Id, null, "7"))).EnsureSuccessStatusCode();

        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal("7", saved.ResponseValue);
    }

    [Fact]
    public async Task Save_ResponseWithoutContentReference_ReturnsBadRequest()
    {
        var (se, _) = await SetupStepWithPrompt();

        var response = await Save(se.Id, new PromptResponseItemDto(null, null, "5"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetResponses(se.Id));
    }

    [Fact]
    public async Task Save_UnknownStepExecution_ReturnsNotFound()
    {
        var response = await Save(Guid.NewGuid(), new PromptResponseItemDto(Guid.NewGuid(), null, "5"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
