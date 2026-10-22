using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// <c>POST /api/step-executions/{id}/prompt-responses</c> may only answer prompts that exist
/// and belong to the execution's ProcessStep or to that step's StepTemplate.
/// </summary>
public class PromptResponseContentOwnershipTests : IntegrationTestBase
{
    public PromptResponseContentOwnershipTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<ProcessStepContentResponseDto> AddProcessStepPrompt(Guid processId, Guid stepId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{processId}/steps/{stepId}/content/prompt",
            new AddPromptBlockDto("Torque", "NumericEntry"), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;
    }

    private async Task<StepTemplateContentResponseDto> AddTemplatePrompt(Guid stepTemplateId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{stepTemplateId}/content/prompt",
            new AddPromptBlockDto("Operator initials", "TextEntry"), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions))!;
    }

    /// <summary>Starts a job for the scenario and returns the execution of process step 1 (Deburr).</summary>
    private async Task<StepExecutionResponseDto> StartFirstStepExecution(WidgetFinishingScenario scenario)
    {
        var job = await CreateJob(scenario.Process.Id);
        (await Client.PostAsync($"/api/jobs/{job.Id}/start", null)).EnsureSuccessStatusCode();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        return executions!.Single(se => se.Sequence == 1);
    }

    private Task<HttpResponseMessage> Save(Guid executionId, PromptResponseItemDto item) =>
        Client.PostAsJsonAsync($"/api/step-executions/{executionId}/prompt-responses",
            new SavePromptResponsesDto(new List<PromptResponseItemDto> { item }), JsonOptions);

    private async Task<List<PromptResponseDto>> GetResponses(Guid executionId) =>
        (await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{executionId}/prompt-responses", JsonOptions))!;

    [Fact]
    public async Task Save_UnknownProcessStepContentId_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(Guid.NewGuid(), null, "5"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetResponses(se.Id));
    }

    [Fact]
    public async Task Save_UnknownStepTemplateContentId_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(null, Guid.NewGuid(), "5"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetResponses(se.Id));
    }

    [Fact]
    public async Task Save_PromptFromAnotherProcessStep_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var step2Prompt = await AddProcessStepPrompt(scenario.Process.Id, scenario.ProcessStep2.Id);
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(step2Prompt.Id, null, "5"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetResponses(se.Id));
    }

    [Fact]
    public async Task Save_PromptFromAnotherStepTemplate_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var inspectionPrompt = await AddTemplatePrompt(scenario.InspectionStep.Id);
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(null, inspectionPrompt.Id, "JM"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetResponses(se.Id));
    }

    [Fact]
    public async Task Save_PromptOnOwnProcessStep_IsSaved()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var prompt = await AddProcessStepPrompt(scenario.Process.Id, scenario.ProcessStep1.Id);
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(prompt.Id, null, "5"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal(prompt.Id, saved.ProcessStepContentId);
        Assert.Equal("5", saved.ResponseValue);
    }

    [Fact]
    public async Task Save_PromptOnOwnStepTemplate_IsSaved()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var prompt = await AddTemplatePrompt(scenario.DeburrStep.Id);
        var se = await StartFirstStepExecution(scenario);

        var response = await Save(se.Id, new PromptResponseItemDto(null, prompt.Id, "JM"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = Assert.Single(await GetResponses(se.Id));
        Assert.Equal(prompt.Id, saved.StepTemplateContentId);
        Assert.Equal("JM", saved.ResponseValue);
    }
}
