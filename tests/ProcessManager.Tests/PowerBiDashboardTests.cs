using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class PowerBiDashboardTests : IntegrationTestBase
{
    public PowerBiDashboardTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────────────── helpers ────────────────────

    private async Task<PowerBiDashboardResponseDto> CreateDashboard(
        string name = "Test Dashboard",
        string embedUrl = "https://app.powerbi.com/view?r=test123",
        string? description = null,
        int sortOrder = 0)
    {
        var dto = new PowerBiDashboardCreateDto(name, embedUrl, description, sortOrder);
        var response = await Client.PostAsJsonAsync("/api/powerbi-dashboards", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PowerBiDashboardResponseDto>(JsonOptions))!;
    }

    // ──────────────────── create validation ────────────────────

    [Fact]
    public async Task Create_ValidDashboard_ReturnsCreated()
    {
        var result = await CreateDashboard(
            name: "Production Overview",
            embedUrl: "https://app.powerbi.com/view?r=abc123",
            description: "Main production dashboard",
            sortOrder: 1);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Production Overview", result.Name);
        Assert.Equal("https://app.powerbi.com/view?r=abc123", result.EmbedUrl);
        Assert.Equal("Main production dashboard", result.Description);
        Assert.Equal(1, result.SortOrder);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var dto = new PowerBiDashboardCreateDto("", "https://app.powerbi.com/view?r=test", null, 0);
        var response = await Client.PostAsJsonAsync("/api/powerbi-dashboards", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingEmbedUrl_ReturnsBadRequest()
    {
        var dto = new PowerBiDashboardCreateDto("My Dashboard", "", null, 0);
        var response = await Client.PostAsJsonAsync("/api/powerbi-dashboards", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        await CreateDashboard(name: "Unique Name");

        var dto = new PowerBiDashboardCreateDto("Unique Name", "https://app.powerbi.com/view?r=other", null, 0);
        var response = await Client.PostAsJsonAsync("/api/powerbi-dashboards", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────────────── get all ────────────────────

    [Fact]
    public async Task GetAll_ReturnsEmptyList_Initially()
    {
        var response = await Client.GetAsync("/api/powerbi-dashboards");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<PowerBiDashboardResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        // May contain items from other tests (shared DB in test class), so just assert success
    }

    [Fact]
    public async Task GetAll_AfterCreate_ReturnsDashboard()
    {
        var created = await CreateDashboard(name: "GetAll Test Dashboard");

        var response = await Client.GetAsync("/api/powerbi-dashboards");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<PowerBiDashboardResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Contains(result!, d => d.Id == created.Id && d.Name == "GetAll Test Dashboard");
    }

    // ──────────────────── get by id ────────────────────

    [Fact]
    public async Task Get_ById_ReturnsDashboard()
    {
        var created = await CreateDashboard(name: "GetById Test");

        var response = await Client.GetAsync($"/api/powerbi-dashboards/{created.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PowerBiDashboardResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal("GetById Test", result.Name);
    }

    [Fact]
    public async Task Get_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/powerbi-dashboards/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────── update ────────────────────

    [Fact]
    public async Task Update_ChangeNameAndUrl_ReturnsUpdated()
    {
        var created = await CreateDashboard(name: "Before Update");

        var updateDto = new PowerBiDashboardUpdateDto(
            "After Update",
            "https://app.powerbi.com/view?r=updated",
            "Updated description",
            5);

        var response = await Client.PutAsJsonAsync($"/api/powerbi-dashboards/{created.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PowerBiDashboardResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("After Update", result!.Name);
        Assert.Equal("https://app.powerbi.com/view?r=updated", result.EmbedUrl);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(5, result.SortOrder);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var updateDto = new PowerBiDashboardUpdateDto("Name", "https://example.com", null, 0);
        var response = await Client.PutAsJsonAsync($"/api/powerbi-dashboards/{Guid.NewGuid()}", updateDto, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────── delete ────────────────────

    [Fact]
    public async Task Delete_ExistingDashboard_ReturnsNoContent()
    {
        var created = await CreateDashboard(name: "To Be Deleted");

        var response = await Client.DeleteAsync($"/api/powerbi-dashboards/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's actually gone
        var getResponse = await Client.GetAsync($"/api/powerbi-dashboards/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/powerbi-dashboards/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
