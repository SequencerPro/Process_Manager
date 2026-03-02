using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class AlertTests : IntegrationTestBase
{
    public AlertTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────────────── helpers ────────────────────

    /// <summary>
    /// Adds a NumericEntry prompt block to the given step template with tight min/max bounds,
    /// creates a job, starts it, starts the first step, and saves an out-of-range response.
    /// Returns the step execution id that received the response.
    /// </summary>
    private async Task<Guid> CreateOutOfRangeAlertScenario(decimal minValue = 0m, decimal maxValue = 10m, string outOfRangeValue = "999")
    {
        var scenario = await BuildWidgetFinishingScenario();

        // Add a numeric prompt block to the deburr step template (min=0, max=10)
        var promptDto = new AddStepTemplatePromptBlockDto(
            Label: "Width",
            PromptType: "NumericEntry",
            IsRequired: true,
            Units: "mm",
            MinValue: minValue,
            MaxValue: maxValue
        );
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{scenario.DeburrStep.Id}/content/prompt",
            promptDto,
            JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var promptContent = await promptResp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions);

        // Create job and start it
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        // Get step executions
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);

        // Start step 1
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Submit out-of-range value
        var saveDto = new SavePromptResponsesDto(
            new List<PromptResponseItemDto>
            {
                new PromptResponseItemDto(
                    ProcessStepContentId: null,
                    StepTemplateContentId: promptContent!.Id,
                    ResponseValue: outOfRangeValue,
                    OverrideNote: "test override"
                )
            }
        );
        var saveResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/prompt-responses",
            saveDto,
            JsonOptions);
        saveResp.EnsureSuccessStatusCode();

        return step1.Id;
    }

    // ──────────────────── GET /api/alerts/out-of-range ────────────────────

    [Fact]
    public async Task GetOutOfRange_ReturnsOkAndValidList()
    {
        var response = await Client.GetAsync("/api/alerts/out-of-range");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<OutOfRangeAlertDto>>(JsonOptions);
        Assert.NotNull(result);  // may be empty or non-empty depending on test order
    }

    [Fact]
    public async Task GetOutOfRange_WithOutOfRangeResponse_ReturnsAlert()
    {
        var stepExecutionId = await CreateOutOfRangeAlertScenario();

        var result = await Client.GetFromJsonAsync<List<OutOfRangeAlertDto>>(
            "/api/alerts/out-of-range?days=0", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result, a => a.StepExecutionId == stepExecutionId);
    }

    [Fact]
    public async Task GetOutOfRange_AlertHasExpectedFields()
    {
        await CreateOutOfRangeAlertScenario(outOfRangeValue: "9999");

        var result = await Client.GetFromJsonAsync<List<OutOfRangeAlertDto>>(
            "/api/alerts/out-of-range?days=0", JsonOptions);

        Assert.NotNull(result);
        var alert = result!.FirstOrDefault(a => a.Value == "9999");
        Assert.NotNull(alert);
        Assert.False(string.IsNullOrEmpty(alert!.JobCode));
        Assert.False(string.IsNullOrEmpty(alert.JobName));
        Assert.False(string.IsNullOrEmpty(alert.ProcessName));
        Assert.False(string.IsNullOrEmpty(alert.StepName));
        Assert.Equal("Width", alert.PromptLabel);
        Assert.Equal("test override", alert.OverrideNote);
    }

    [Fact]
    public async Task GetOutOfRange_ValueWithinRange_IsNotReturned()
    {
        var scenario = await BuildWidgetFinishingScenario();

        // Add prompt (0-100 range)
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{scenario.DeburrStep.Id}/content/prompt",
            new AddStepTemplatePromptBlockDto("Temp", "NumericEntry", true, "°C", MinValue: 0m, MaxValue: 100m),
            JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var promptContent = await promptResp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions);

        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Value within range
        var saveDto = new SavePromptResponsesDto(new List<PromptResponseItemDto>
        {
            new PromptResponseItemDto(null, promptContent!.Id, "50", null)
        });
        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/prompt-responses", saveDto, JsonOptions);

        var result = await Client.GetFromJsonAsync<List<OutOfRangeAlertDto>>(
            "/api/alerts/out-of-range?days=0", JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(result, a => a.StepExecutionId == step1.Id);
    }

    [Fact]
    public async Task GetOutOfRange_LimitParameter_CapResultCount()
    {
        // Create two distinct out-of-range scenarios
        await CreateOutOfRangeAlertScenario();
        await CreateOutOfRangeAlertScenario();

        var result = await Client.GetFromJsonAsync<List<OutOfRangeAlertDto>>(
            "/api/alerts/out-of-range?days=0&limit=1", JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetOutOfRange_OrderedMostRecentFirst()
    {
        await CreateOutOfRangeAlertScenario();
        await CreateOutOfRangeAlertScenario();

        var result = await Client.GetFromJsonAsync<List<OutOfRangeAlertDto>>(
            "/api/alerts/out-of-range?days=0", JsonOptions);

        Assert.NotNull(result);
        if (result!.Count >= 2)
        {
            for (int i = 1; i < result.Count; i++)
                Assert.True(result[i - 1].RespondedAt >= result[i].RespondedAt);
        }
    }

    // ──────────────────── GET /api/alerts/out-of-range/count ────────────────────

    [Fact]
    public async Task GetOutOfRangeCount_ReturnsNonNegativeCount()
    {
        var result = await Client.GetFromJsonAsync<AlertCountDto>(
            "/api/alerts/out-of-range/count", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Count >= 0);
    }

    [Fact]
    public async Task GetOutOfRangeCount_WithAlerts_ReturnsCorrectCount()
    {
        await CreateOutOfRangeAlertScenario();
        await CreateOutOfRangeAlertScenario();

        var result = await Client.GetFromJsonAsync<AlertCountDto>(
            "/api/alerts/out-of-range/count?days=0", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Count >= 2);
    }

    [Fact]
    public async Task GetOutOfRangeCount_DaysFilter_IncludesRecentAlerts()
    {
        await CreateOutOfRangeAlertScenario();

        var result = await Client.GetFromJsonAsync<AlertCountDto>(
            "/api/alerts/out-of-range/count?days=1", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Count >= 1);
    }
}
