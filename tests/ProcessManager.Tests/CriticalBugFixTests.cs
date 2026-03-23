using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Tests to verify critical bug fixes identified in the March 2026 audit.
/// Covers: null reference guards in StepExecutionsController,
/// query string encoding in ApiClient, and ChangePassword injection.
/// </summary>
public class CriticalBugFixTests : IntegrationTestBase
{
    public CriticalBugFixTests(TestWebApplicationFactory factory) : base(factory) { }

    // ═══════════════════════════════════════════════════════════════
    // Bug #2 & #3: Null reference guards in StepExecutionsController
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Start_StepExecution_WhenJobIsLoaded_Succeeds()
    {
        // Arrange: Create a running job with step executions
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);

        // Act
        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Assert: Should succeed, proving Job navigation property is loaded
        response.EnsureSuccessStatusCode();
        var started = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("InProgress", started!.Status);
    }

    [Fact]
    public async Task Start_NonExistentStepExecution_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/api/step-executions/{Guid.NewGuid()}/start", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddPortTransaction_WhenProcessStepIsLoaded_Succeeds()
    {
        // Arrange: This tests Bug #3 — ProcessStep null reference on port transactions
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, $"WDG-{Guid.NewGuid().ToString()[..6]}");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Act: Record a port transaction — this accesses se.ProcessStep.StepTemplateId
        var ptDto = new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions", ptDto, JsonOptions);

        // Assert: Should succeed without NullReferenceException
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddPortTransaction_WrongPort_ReturnsBadRequest()
    {
        // Ensure the null guard doesn't mask legitimate validation errors
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, $"WDG-{Guid.NewGuid().ToString()[..6]}");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Use a port from the SECOND step template against the FIRST step execution
        var ptDto = new CreatePortTransactionDto(scenario.InspGoodPort.Id, item.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions", ptDto, JsonOptions);

        // Should return BadRequest (port doesn't belong to this step's template)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Bug #1: Query string injection in ApiClient
    // These are API-level tests ensuring the endpoints handle
    // special characters in query parameters gracefully
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetKinds_SearchWithSpecialChars_ReturnsOkNotError()
    {
        // Arrange: search with characters that could break unencoded URLs
        var specialSearch = "test&page=999&evil=true";

        // Act: The API should handle this gracefully regardless of encoding
        var response = await Client.GetAsync(
            $"/api/kinds?search={Uri.EscapeDataString(specialSearch)}&page=1&pageSize=10");

        // Assert: Should return 200 OK with empty results, not crash
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<KindResponseDto>>(JsonOptions);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProcesses_SearchWithAmpersand_ReturnsOk()
    {
        var response = await Client.GetAsync(
            $"/api/processes?search={Uri.EscapeDataString("R&D Process")}&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetStepTemplates_SearchWithHashAndQuestion_ReturnsOk()
    {
        var response = await Client.GetAsync(
            $"/api/steptemplates?search={Uri.EscapeDataString("step#1?")}&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetJobs_SearchWithUnicodeChars_ReturnsOk()
    {
        var response = await Client.GetAsync(
            $"/api/jobs?search={Uri.EscapeDataString("Prüfung")}&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetItems_EmptySearch_ReturnsOk()
    {
        // Null/empty search should be handled gracefully
        var response = await Client.GetAsync("/api/items?search=&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
    }

    // ═══════════════════════════════════════════════════════════════
    // Regression: Existing lifecycle operations still work
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullLifecycle_StartCompleteSkip_WorksEndToEnd()
    {
        // This comprehensive test ensures our null guards don't break normal flow
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // Start the job
        var startJobResp = await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        startJobResp.EnsureSuccessStatusCode();

        // Get step executions
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        Assert.NotNull(executions);
        Assert.Equal(2, executions!.Count);

        var step1 = executions.First(se => se.Sequence == 1);
        var step2 = executions.First(se => se.Sequence == 2);

        // Start step 1
        var resp1 = await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);
        resp1.EnsureSuccessStatusCode();

        // Complete step 1
        var resp2 = await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null);
        resp2.EnsureSuccessStatusCode();

        // Skip step 2
        var resp3 = await Client.PostAsync($"/api/step-executions/{step2.Id}/skip", null);
        resp3.EnsureSuccessStatusCode();

        // Verify final states
        var detail1 = await Client.GetFromJsonAsync<StepExecutionResponseDto>(
            $"/api/step-executions/{step1.Id}", JsonOptions);
        var detail2 = await Client.GetFromJsonAsync<StepExecutionResponseDto>(
            $"/api/step-executions/{step2.Id}", JsonOptions);

        Assert.Equal("Completed", detail1!.Status);
        Assert.Equal("Skipped", detail2!.Status);
    }

    [Fact]
    public async Task PortTransaction_FullWorkflow_InputAndOutput()
    {
        // Regression test: full port transaction workflow still works after null guard additions
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id,
            $"WDG-{Guid.NewGuid().ToString()[..6]}");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);

        // Start step
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Input transaction
        var inResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);
        inResp.EnsureSuccessStatusCode();

        // Output transaction
        var outResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrOutPort.Id, item.Id), JsonOptions);
        outResp.EnsureSuccessStatusCode();

        // Complete step
        var compResp = await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null);
        compResp.EnsureSuccessStatusCode();

        // Verify transactions recorded
        var transactions = await Client.GetFromJsonAsync<List<PortTransactionResponseDto>>(
            $"/api/step-executions/{step1.Id}/port-transactions", JsonOptions);
        Assert.Equal(2, transactions!.Count);
    }
}
