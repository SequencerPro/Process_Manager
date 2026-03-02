using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class GradeTests : IntegrationTestBase
{
    public GradeTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── CREATE ────────────

    [Fact]
    public async Task CreateGrade_Valid_ReturnsCreated()
    {
        var kind = await CreateKind("GC-001", "Grade Create Kind");
        var grade = await CreateGrade(kind.Id, "RAW", "Raw", "Unprocessed", true, 0);

        Assert.Equal("RAW", grade.Code);
        Assert.Equal("Raw", grade.Name);
        Assert.Equal("Unprocessed", grade.Description);
        Assert.True(grade.IsDefault);
        Assert.Equal(kind.Id, grade.KindId);
    }

    [Fact]
    public async Task CreateGrade_DuplicateCode_ReturnsConflict()
    {
        var kind = await CreateKind("GC-002", "Grade Dup Kind");
        await CreateGrade(kind.Id, "DUP", "First");

        var dto = new GradeCreateDto("DUP", "Second", null, false, 1);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{kind.Id}/grades", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateGrade_NonExistentKind_ReturnsNotFound()
    {
        var dto = new GradeCreateDto("RAW", "Raw", null, false, 0);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{Guid.NewGuid()}/grades", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── DEFAULT GRADE BEHAVIOR ────────────

    [Fact]
    public async Task CreateGrade_SetDefault_ClearsPreviousDefault()
    {
        var kind = await CreateKind("GC-003", "Default Toggle Kind");
        var grade1 = await CreateGrade(kind.Id, "G1", "Grade 1", isDefault: true, sortOrder: 0);
        var grade2 = await CreateGrade(kind.Id, "G2", "Grade 2", isDefault: true, sortOrder: 1);

        // Fetch kind — only grade2 should be default
        var response = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);

        Assert.NotNull(result);
        var g1 = result.Grades.Single(g => g.Code == "G1");
        var g2 = result.Grades.Single(g => g.Code == "G2");
        Assert.False(g1.IsDefault);
        Assert.True(g2.IsDefault);
    }

    // ──────────── UPDATE ────────────

    [Fact]
    public async Task UpdateGrade_Valid_ReturnsUpdated()
    {
        var kind = await CreateKind("GC-004", "Grade Update Kind");
        var grade = await CreateGrade(kind.Id, "UPD", "Original");

        var updateDto = new GradeUpdateDto("Updated Grade", "New desc", true, 5);
        var response = await Client.PutAsJsonAsync(
            $"/api/kinds/{kind.Id}/grades/{grade.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<GradeResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated Grade", updated.Name);
        Assert.Equal("New desc", updated.Description);
        Assert.True(updated.IsDefault);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public async Task UpdateGrade_WrongKind_ReturnsNotFound()
    {
        var kind1 = await CreateKind("GC-005", "Kind A");
        var kind2 = await CreateKind("GC-006", "Kind B");
        var grade = await CreateGrade(kind1.Id, "G1", "Grade 1");

        // Try to update grade via kind2's URL
        var updateDto = new GradeUpdateDto("Hacked", null, false, 0);
        var response = await Client.PutAsJsonAsync(
            $"/api/kinds/{kind2.Id}/grades/{grade.Id}", updateDto, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── DELETE ────────────

    [Fact]
    public async Task DeleteGrade_Existing_ReturnsNoContent()
    {
        var kind = await CreateKind("GC-007", "Grade Delete Kind");
        var grade = await CreateGrade(kind.Id, "DEL", "To Delete");

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{grade.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone by fetching the kind
        var getResponse = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var kind2 = await getResponse.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind2);
        Assert.DoesNotContain(kind2.Grades, g => g.Code == "DEL");
    }

    [Fact]
    public async Task DeleteGrade_NonExistent_ReturnsNotFound()
    {
        var kind = await CreateKind("GC-008", "Kind for 404");
        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
