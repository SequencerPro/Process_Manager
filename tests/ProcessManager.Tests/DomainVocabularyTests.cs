using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class DomainVocabularyTests : IntegrationTestBase
{
    public DomainVocabularyTests(TestWebApplicationFactory factory) : base(factory) { }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid().ToString()[..8]}";

    private async Task<DomainVocabularyResponseDto> CreateVocabulary(string name)
    {
        var dto = new DomainVocabularyCreateDto(name,
            "Kind", "KindCode", "Grade", "Item", "ItemId",
            "Batch", "BatchId", "Job", "Workflow", "Process", "Step");
        var response = await Client.PostAsJsonAsync("/api/domainvocabularies", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions))!;
    }

    private static DomainVocabularyUpdateDto UpdateDto(string name, string termJob = "Job") => new(name,
        "Kind", "KindCode", "Grade", "Item", "ItemId",
        "Batch", "BatchId", termJob, "Workflow", "Process", "Step");

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var name = UniqueName("Dup Create");
        await CreateVocabulary(name);

        var dto = new DomainVocabularyCreateDto(name,
            "Kind", "KindCode", "Grade", "Item", "ItemId",
            "Batch", "BatchId", "Job", "Workflow", "Process", "Step");
        var response = await Client.PostAsJsonAsync("/api/domainvocabularies", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_RenameToExistingName_ReturnsConflict()
    {
        var existing = await CreateVocabulary(UniqueName("Existing"));
        var other = await CreateVocabulary(UniqueName("Other"));

        var response = await Client.PutAsJsonAsync(
            $"/api/domainvocabularies/{other.Id}", UpdateDto(existing.Name), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await Client.GetFromJsonAsync<DomainVocabularyResponseDto>(
            $"/api/domainvocabularies/{other.Id}", JsonOptions);
        Assert.Equal(other.Name, unchanged!.Name);
    }

    [Fact]
    public async Task Update_KeepingOwnName_Succeeds()
    {
        var vocab = await CreateVocabulary(UniqueName("Self"));

        var response = await Client.PutAsJsonAsync(
            $"/api/domainvocabularies/{vocab.Id}", UpdateDto(vocab.Name, termJob: "Work Order"), JsonOptions);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);
        Assert.Equal(vocab.Name, updated!.Name);
        Assert.Equal("Work Order", updated.TermJob);
    }

    [Fact]
    public async Task Update_RenameToNewName_Succeeds()
    {
        var vocab = await CreateVocabulary(UniqueName("Before"));
        var newName = UniqueName("After");

        var response = await Client.PutAsJsonAsync(
            $"/api/domainvocabularies/{vocab.Id}", UpdateDto(newName), JsonOptions);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(JsonOptions);
        Assert.Equal(newName, updated!.Name);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/domainvocabularies/{Guid.NewGuid()}", UpdateDto(UniqueName("Ghost")), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
