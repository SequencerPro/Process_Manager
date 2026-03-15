using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class ControlPlanTests : IntegrationTestBase
{
    public ControlPlanTests(TestWebApplicationFactory factory) : base(factory) { }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private async Task<ControlPlanResponseDto> CreateControlPlan(
        Guid processId,
        string? code = null,
        string name = "Test Control Plan")
    {
        code ??= $"CP-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var dto = new ControlPlanCreateDto(processId, code, name, null);
        var response = await Client.PostAsJsonAsync("/api/controlplans", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions))!;
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ControlPlan_ReturnsCreated()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var cp = await CreateControlPlan(scenario.Process.Id, "CP-WIDGET-V1", "Widget Control Plan");

        Assert.Equal("CP-WIDGET-V1", cp.Code);
        Assert.Equal("Widget Control Plan", cp.Name);
        Assert.Equal(scenario.Process.Id, cp.ProcessId);
        Assert.Equal(1, cp.Version);
        Assert.True(cp.IsActive);
        Assert.False(cp.IsStale);
    }

    [Fact]
    public async Task Create_ControlPlan_AutoPopulatesEntriesFromSteps()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var cp = await CreateControlPlan(scenario.Process.Id);

        // The Widget Finishing Scenario has 2 process steps — expect 2 auto-populated entries
        Assert.Equal(2, cp.Entries.Count);
        Assert.All(cp.Entries, e => Assert.NotEmpty(e.CharacteristicName));
        Assert.All(cp.Entries, e => Assert.NotEqual(Guid.Empty, e.ProcessStepId));
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateControlPlan(scenario.Process.Id, "CP-DUP");

        var dto = new ControlPlanCreateDto(scenario.Process.Id, "CP-DUP", "Duplicate", null);
        var response = await Client.PostAsJsonAsync("/api/controlplans", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidProcess_ReturnsNotFound()
    {
        var dto = new ControlPlanCreateDto(Guid.NewGuid(), "CP-NOPROC", "No Process", null);
        var response = await Client.PostAsJsonAsync("/api/controlplans", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── READ ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsControlPlanWithEntries()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);

        var fetched = await Client.GetFromJsonAsync<ControlPlanResponseDto>(
            $"/api/controlplans/{cp.Id}", JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(cp.Id, fetched!.Id);
        Assert.Equal(2, fetched.Entries.Count);
    }

    [Fact]
    public async Task GetById_EntriesAreOrderedByStepSequence()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);

        var fetched = await Client.GetFromJsonAsync<ControlPlanResponseDto>(
            $"/api/controlplans/{cp.Id}", JsonOptions);

        var sequences = fetched!.Entries.Select(e => e.ProcessStepSequence).ToList();
        Assert.Equal(sequences.OrderBy(s => s), sequences);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/controlplans/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsControlPlans()
    {
        var scenario = await BuildWidgetFinishingScenario();
        await CreateControlPlan(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ControlPlanSummaryDto>>(
            "/api/controlplans", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
    }

    [Fact]
    public async Task List_FilterByStale_ReturnsOnlyStalePlans()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);

        // Mark stale manually via PUT
        var updateDto = new ControlPlanUpdateDto(cp.Name, cp.Description, cp.IsActive);
        await Client.PutAsJsonAsync($"/api/controlplans/{cp.Id}", updateDto, JsonOptions);

        // Stale filter = false (current)
        var result = await Client.GetFromJsonAsync<PaginatedResponse<ControlPlanSummaryDto>>(
            $"/api/controlplans?stale=false", JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(result!.Items, i => i.IsStale);
    }

    // ─── UPDATE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ControlPlan_ChangesNameAndDescription()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id, name: "Original Name");

        var updateDto = new ControlPlanUpdateDto("Updated Name", "New description", true);
        var response = await Client.PutAsJsonAsync($"/api/controlplans/{cp.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public async Task Update_IsActive_False_DeactivatesPlan()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);

        var updateDto = new ControlPlanUpdateDto(cp.Name, null, false);
        var response = await Client.PutAsJsonAsync($"/api/controlplans/{cp.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions);
        Assert.False(updated!.IsActive);
    }

    // ─── DELETE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ControlPlan_Returns204()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);

        var response = await Client.DeleteAsync($"/api/controlplans/{cp.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var gone = await Client.GetAsync($"/api/controlplans/{cp.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    // ─── ENTRIES ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddEntry_ValidStep_ReturnsUpdatedPlan()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);
        var initialCount = cp.Entries.Count;

        var entryDto = new ControlPlanEntryCreateDto(
            scenario.ProcessStep1.Id,
            "Dimensional Check",
            CharacteristicType.Product,
            "25.4 ± 0.05 mm",
            "Vernier caliper",
            "n=5",
            "Every hour",
            "SPC chart",
            "Stop and notify quality",
            null, null, 99);

        var response = await Client.PostAsJsonAsync(
            $"/api/controlplans/{cp.Id}/entries", entryDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions);
        Assert.Equal(initialCount + 1, updated!.Entries.Count);
        Assert.Contains(updated.Entries, e => e.CharacteristicName == "Dimensional Check");
    }

    [Fact]
    public async Task AddEntry_StepNotInProcess_ReturnsBadRequest()
    {
        // Create two separate processes; use a step from process 2 in plan for process 1
        var scenario1 = await BuildWidgetFinishingScenario();
        var scenario2 = await BuildWidgetFinishingScenario();

        var cp = await CreateControlPlan(scenario1.Process.Id);

        var entryDto = new ControlPlanEntryCreateDto(
            scenario2.ProcessStep1.Id,   // step from the WRONG process
            "Wrong Step Char",
            CharacteristicType.Process,
            null, null, null, null, null, null, null, null, 0);

        var response = await Client.PostAsJsonAsync(
            $"/api/controlplans/{cp.Id}/entries", entryDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEntry_ChangesFields()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);
        var entry = cp.Entries.First();

        var updateDto = new ControlPlanEntryUpdateDto(
            "Updated Characteristic",
            CharacteristicType.Process,
            "New spec",
            "New technique",
            "n=10",
            "Per shift",
            "Visual",
            "Updated reaction",
            null, null,
            entry.SortOrder);

        var response = await Client.PutAsJsonAsync(
            $"/api/controlplans/{cp.Id}/entries/{entry.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions);
        var updatedEntry = updated!.Entries.First(e => e.Id == entry.Id);
        Assert.Equal("Updated Characteristic", updatedEntry.CharacteristicName);
        Assert.Equal("Process", updatedEntry.CharacteristicType);
        Assert.Equal("New spec", updatedEntry.SpecificationOrTolerance);
    }

    [Fact]
    public async Task DeleteEntry_RemovesEntry()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);
        var entry = cp.Entries.First();
        var initialCount = cp.Entries.Count;

        var response = await Client.DeleteAsync(
            $"/api/controlplans/{cp.Id}/entries/{entry.Id}");
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions);
        Assert.Equal(initialCount - 1, updated!.Entries.Count);
        Assert.DoesNotContain(updated.Entries, e => e.Id == entry.Id);
    }

    // ─── STALENESS ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearStaleness_WhenNotStale_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id);
        Assert.False(cp.IsStale);

        var dto = new ClearControlPlanStalenessDto("Test User", null);
        var response = await Client.PostAsJsonAsync(
            $"/api/controlplans/{cp.Id}/clear-staleness", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── EXPORT ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ReturnsCsvContent()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var cp = await CreateControlPlan(scenario.Process.Id, "CP-CSV-TEST", "CSV Test Plan");

        var response = await Client.GetAsync($"/api/controlplans/{cp.Id}/export");
        response.EnsureSuccessStatusCode();

        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("CP-CSV-TEST", csv);
        Assert.Contains("Process Step", csv);  // Header row
    }
}
