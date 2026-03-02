using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class KindTests : IntegrationTestBase
{
    public KindTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── GET ────────────

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/kinds");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<KindResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    // ──────────── CREATE ────────────

    [Fact]
    public async Task Create_ValidKind_ReturnsCreated()
    {
        var kind = await CreateKind("CRE-001", "Created Kind", "A test kind", true, false);

        Assert.Equal("CRE-001", kind.Code);
        Assert.Equal("Created Kind", kind.Name);
        Assert.Equal("A test kind", kind.Description);
        Assert.True(kind.IsSerialized);
        Assert.False(kind.IsBatchable);
        Assert.NotEqual(Guid.Empty, kind.Id);
        Assert.NotEqual(default, kind.CreatedAt);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await CreateKind("DUP-001", "First");

        var dto = new KindCreateDto("DUP-001", "Second", null, false, false);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────── GET BY ID ────────────

    [Fact]
    public async Task GetById_Existing_ReturnsKind()
    {
        var created = await CreateKind("GET-001", "Get Test");

        var response = await Client.GetAsync($"/api/kinds/{created.Id}");
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal(created.Id, kind.Id);
        Assert.Equal("GET-001", kind.Code);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/kinds/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── UPDATE ────────────

    [Fact]
    public async Task Update_ValidChanges_ReturnsUpdated()
    {
        var kind = await CreateKind("UPD-001", "Original");

        var updateDto = new KindUpdateDto("Updated Name", "New description", true, true);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{kind.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("New description", updated.Description);
        Assert.True(updated.IsSerialized);
        Assert.True(updated.IsBatchable);
        // Code should not change (not in update DTO)
        Assert.Equal("UPD-001", updated.Code);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var updateDto = new KindUpdateDto("Name", null, false, false);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{Guid.NewGuid()}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── DELETE ────────────

    [Fact]
    public async Task Delete_Existing_ReturnsNoContent()
    {
        var kind = await CreateKind("DEL-001", "To Delete");

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await Client.GetAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/kinds/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── KIND WITH GRADES ────────────

    [Fact]
    public async Task Create_KindWithGrades_IncludesGradesInResponse()
    {
        var kind = await CreateKind("GRD-001", "Graded Kind");
        var grade1 = await CreateGrade(kind.Id, "RAW", "Raw", isDefault: true, sortOrder: 0);
        var grade2 = await CreateGrade(kind.Id, "PASS", "Passed", isDefault: false, sortOrder: 1);

        // Fetch the kind and verify grades are included
        var response = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result.Grades.Count);
        Assert.Equal("RAW", result.Grades[0].Code);
        Assert.Equal("PASS", result.Grades[1].Code);
    }
}
