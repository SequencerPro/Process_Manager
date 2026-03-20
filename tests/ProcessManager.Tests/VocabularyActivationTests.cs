using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Web.Services;

namespace ProcessManager.Tests;

public class VocabularyActivationTests : IntegrationTestBase
{
    public VocabularyActivationTests(TestWebApplicationFactory factory) : base(factory) { }

    private static DomainVocabularyCreateDto MakeVocab(string name) => new(
        name,
        "Material Kind", "Material Code", "Grade",
        "Unit", "Serial No.",
        "Lot", "Lot No.",
        "Work Order", "Workflow", "Process", "Operation", "Production Order");

    private async Task<DomainVocabularyResponseDto> CreateVocab(string name)
    {
        var resp = await Client.PostAsJsonAsync("/api/domainvocabularies", MakeVocab(name), JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions))!;
    }

    // ── Activate / Deactivate ────────────────────────────────────────────

    [Fact]
    public async Task Activate_sets_vocabulary_active()
    {
        var vocab = await CreateVocab($"Activate-{Guid.NewGuid().ToString()[..6]}");
        Assert.False(vocab.IsActive);

        var resp = await Client.PutAsync($"/api/domainvocabularies/{vocab.Id}/activate", null);
        resp.EnsureSuccessStatusCode();
        var activated = await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);

        Assert.NotNull(activated);
        Assert.True(activated!.IsActive);
    }

    [Fact]
    public async Task Only_one_vocabulary_active_at_a_time()
    {
        var vocabA = await CreateVocab($"VocabA-{Guid.NewGuid().ToString()[..6]}");
        var vocabB = await CreateVocab($"VocabB-{Guid.NewGuid().ToString()[..6]}");

        // Activate A
        await Client.PutAsync($"/api/domainvocabularies/{vocabA.Id}/activate", null);
        // Activate B — should deactivate A
        await Client.PutAsync($"/api/domainvocabularies/{vocabB.Id}/activate", null);

        var a = await Client.GetFromJsonAsync<DomainVocabularyResponseDto>(
            $"/api/domainvocabularies/{vocabA.Id}", JsonOptions);
        var b = await Client.GetFromJsonAsync<DomainVocabularyResponseDto>(
            $"/api/domainvocabularies/{vocabB.Id}", JsonOptions);

        Assert.False(a!.IsActive);
        Assert.True(b!.IsActive);
    }

    [Fact]
    public async Task Deactivate_clears_active()
    {
        var vocab = await CreateVocab($"Deact-{Guid.NewGuid().ToString()[..6]}");
        await Client.PutAsync($"/api/domainvocabularies/{vocab.Id}/activate", null);

        var resp = await Client.PutAsync($"/api/domainvocabularies/{vocab.Id}/deactivate", null);
        resp.EnsureSuccessStatusCode();
        var deactivated = await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);

        Assert.False(deactivated!.IsActive);
    }

    // ── GET active ────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_active_returns_204_when_none_active()
    {
        // Ensure nothing is active by deactivating all
        var all = await Client.GetFromJsonAsync<PaginatedResponse<DomainVocabularyResponseDto>>(
            "/api/domainvocabularies?pageSize=100", JsonOptions);
        foreach (var v in all!.Items.Where(v => v.IsActive))
            await Client.PutAsync($"/api/domainvocabularies/{v.Id}/deactivate", null);

        var resp = await Client.GetAsync("/api/domainvocabularies/active");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Get_active_returns_active_vocabulary()
    {
        var vocab = await CreateVocab($"GetActive-{Guid.NewGuid().ToString()[..6]}");
        await Client.PutAsync($"/api/domainvocabularies/{vocab.Id}/activate", null);

        var resp = await Client.GetAsync("/api/domainvocabularies/active");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var active = await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);
        Assert.Equal(vocab.Id, active!.Id);
        Assert.True(active.IsActive);
    }

    // ── TermWorkorder roundtrip ──────────────────────────────────────────

    [Fact]
    public async Task Create_and_update_include_workorder_term()
    {
        var vocab = await CreateVocab($"WO-{Guid.NewGuid().ToString()[..6]}");
        Assert.Equal("Production Order", vocab.TermWorkorder);

        var updateDto = new DomainVocabularyUpdateDto(
            vocab.Name, vocab.TermKind, vocab.TermKindCode, vocab.TermGrade,
            vocab.TermItem, vocab.TermItemId, vocab.TermBatch, vocab.TermBatchId,
            vocab.TermJob, vocab.TermWorkflow, vocab.TermProcess, vocab.TermStep, "Manufacturing Order");
        var resp = await Client.PutAsJsonAsync($"/api/domainvocabularies/{vocab.Id}", updateDto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);

        Assert.Equal("Manufacturing Order", updated!.TermWorkorder);
    }

    // ── VocabularyService.Pluralize unit tests ────────────────────────────

    [Theory]
    [InlineData("Kind", "Kinds")]
    [InlineData("Process", "Processes")]
    [InlineData("Batch", "Batches")]
    [InlineData("Category", "Categories")]
    [InlineData("Item", "Items")]
    [InlineData("Station", "Stations")]
    [InlineData("Workorder", "Workorders")]
    [InlineData("Box", "Boxes")]
    [InlineData("Dish", "Dishes")]
    [InlineData("Match", "Matches")]
    [InlineData("Status", "Status")] // already ends in s — returned as-is
    public void Pluralize_handles_common_cases(string input, string expected)
    {
        Assert.Equal(expected, VocabularyService.Pluralize(input));
    }

    // ── VocabularyService defaults ───────────────────────────────────────

    [Fact]
    public void VocabularyService_returns_defaults_when_no_vocabulary_active()
    {
        var svc = new VocabularyService();
        svc.SetVocabulary(null);

        Assert.Equal("Kind", svc.Kind);
        Assert.Equal("Grade", svc.Grade);
        Assert.Equal("Step", svc.Step);
        Assert.Equal("Workflow", svc.Workflow);
        Assert.Equal("Process", svc.Process);
        Assert.Equal("Job", svc.Job);
        Assert.Equal("Workorder", svc.Workorder);
        Assert.Equal("Kinds", svc.Kinds);
        Assert.Equal("Batches", svc.Batches);
        Assert.Equal("Kinds & Grades", svc.KindsAndGrades);
        Assert.Equal("Step Templates", svc.StepTemplates);
        Assert.Equal("Step Executions", svc.StepExecutions);
    }

    [Fact]
    public void VocabularyService_returns_custom_terms_when_vocabulary_set()
    {
        var svc = new VocabularyService();
        svc.SetVocabulary(new DomainVocabularyResponseDto(
            Guid.NewGuid(), "Test", true,
            "Component Type", "Part No.", "Quality Grade",
            "Board", "Board S/N",
            "Panel", "Panel ID",
            "Production Order", "Build Plan", "Build Process", "Station", "Production Order",
            DateTime.UtcNow, DateTime.UtcNow));

        Assert.Equal("Component Type", svc.Kind);
        Assert.Equal("Quality Grade", svc.Grade);
        Assert.Equal("Station", svc.Step);
        Assert.Equal("Build Plan", svc.Workflow);
        Assert.Equal("Build Process", svc.Process);
        Assert.Equal("Production Order", svc.Job);
        Assert.Equal("Component Types", svc.Kinds);
        Assert.Equal("Panels", svc.Batches);
        Assert.Equal("Component Types & Quality Grades", svc.KindsAndGrades);
        Assert.Equal("Station Templates", svc.StepTemplates);
    }
}
