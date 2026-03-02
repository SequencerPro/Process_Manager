using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class AnalyticsTests : IntegrationTestBase
{
    public AnalyticsTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────────────── helpers ────────────────────

    /// <summary>
    /// Creates a scenario with a NumericEntry prompt, a running job, and a submitted
    /// prompt-response value. Returns the ContentId of the prompt block.
    /// </summary>
    private async Task<Guid> CreateAnalyticsDataPoint(string responseValue = "5.5")
    {
        var scenario = await BuildWidgetFinishingScenario();

        // Add a numeric prompt block to the deburr step template
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{scenario.DeburrStep.Id}/content/prompt",
            new AddStepTemplatePromptBlockDto("Thickness", "NumericEntry", true, "mm"),
            JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var promptContent = await promptResp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions);

        // Create job and start it
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        // Get step executions, start step 1, submit a response
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var saveDto = new SavePromptResponsesDto(new List<PromptResponseItemDto>
        {
            new PromptResponseItemDto(null, promptContent!.Id, responseValue, null)
        });
        var saveResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/prompt-responses", saveDto, JsonOptions);
        saveResp.EnsureSuccessStatusCode();

        return promptContent.Id;
    }

    // ──────────────────── validation ────────────────────

    [Fact]
    public async Task Query_EmptySeries_ReturnsBadRequest()
    {
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddDays(-7),
            EndDate: DateTime.UtcNow,
            BucketSizeMinutes: 1440,
            Series: new List<AnalyticsSeriesRequestDto>()
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Query_StartDateAfterEndDate_ReturnsBadRequest()
    {
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(-1),
            BucketSizeMinutes: 1440,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(Guid.NewGuid(), "Label", "#0000ff", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Query_StartDateEqualToEndDate_ReturnsBadRequest()
    {
        var dt = DateTime.UtcNow;
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: dt,
            EndDate: dt,
            BucketSizeMinutes: 1440,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(Guid.NewGuid(), "Label", "#ff0000", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────────────── valid queries ────────────────────

    [Fact]
    public async Task Query_ValidQueryNoData_ReturnsOkWithEmptyRows()
    {
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddDays(-7),
            EndDate: DateTime.UtcNow,
            BucketSizeMinutes: 1440,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(Guid.NewGuid(), "No Data Series", "#aabbcc", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result!.Rows);
        Assert.Equal(0, result.TotalResponses);
    }

    [Fact]
    public async Task Query_ValidQuery_ReturnsSeries()
    {
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddDays(-7),
            EndDate: DateTime.UtcNow,
            BucketSizeMinutes: 1440,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(Guid.NewGuid(), "My Series", "#112233", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Single(result!.Series);
        Assert.Equal("My Series", result.Series[0].Label);
        Assert.Equal("#112233", result.Series[0].Color);
        Assert.Equal(0, result.Series[0].YAxis);
    }

    [Fact]
    public async Task Query_WithPromptResponse_ReturnsDataPoint()
    {
        var contentId = await CreateAnalyticsDataPoint("7.25");

        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddHours(-1),
            EndDate: DateTime.UtcNow.AddHours(1),
            BucketSizeMinutes: 60,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(contentId, "Thickness", "#198754", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalResponses);
        Assert.NotEmpty(result.Rows);

        // At least one row should contain a non-null value for this series
        Assert.Contains(result.Rows, row =>
            row.Values.TryGetValue(contentId.ToString(), out var v) && v.HasValue);
    }

    [Fact]
    public async Task Query_WithMultipleSeries_ReturnsAllSeriesMetadata()
    {
        var contentId1 = await CreateAnalyticsDataPoint("3.0");
        var contentId2 = await CreateAnalyticsDataPoint("8.0");

        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddHours(-1),
            EndDate: DateTime.UtcNow.AddHours(1),
            BucketSizeMinutes: 60,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(contentId1, "First", "#0d6efd", 0),
                new AnalyticsSeriesRequestDto(contentId2, "Second", "#dc3545", 1)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Series.Count);
        Assert.Contains(result.Series, s => s.ContentId == contentId1 && s.Label == "First");
        Assert.Contains(result.Series, s => s.ContentId == contentId2 && s.Label == "Second");
    }

    [Fact]
    public async Task Query_BucketRowKeysMatchSeriesContentIds()
    {
        var contentId = await CreateAnalyticsDataPoint("4.0");

        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddHours(-1),
            EndDate: DateTime.UtcNow.AddHours(1),
            BucketSizeMinutes: 60,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(contentId, "X", "#aaaaaa", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        foreach (var row in result!.Rows)
            Assert.Contains(contentId.ToString(), row.Values.Keys);
    }

    [Fact]
    public async Task Query_ReturnsMetadataFields()
    {
        var dto = new AnalyticsQueryDto(
            ChartType: "RunOverTime",
            StartDate: DateTime.UtcNow.AddDays(-1),
            EndDate: DateTime.UtcNow,
            BucketSizeMinutes: 480,
            Series: new List<AnalyticsSeriesRequestDto>
            {
                new AnalyticsSeriesRequestDto(Guid.NewGuid(), "Lbl", "#ffffff", 0)
            }
        );

        var response = await Client.PostAsJsonAsync("/api/analytics/query", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("RunOverTime", result!.ChartType);
        Assert.Equal(480, result.BucketSizeMinutes);
    }
}
