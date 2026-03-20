using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class OrgUnitTests : IntegrationTestBase
{
    public OrgUnitTests(TestWebApplicationFactory factory) : base(factory) { }

    // ───── Helpers ─────

    private async Task<OrgUnitResponseDto> CreateOrgUnit(
        string code = "TST-001",
        string name = "Test Unit",
        OrgUnitType type = OrgUnitType.Department,
        Guid? parentId = null)
    {
        var dto = new OrgUnitCreateDto(code, name, type, parentId);
        var resp = await Client.PostAsJsonAsync("/api/orgunits", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions))!;
    }

    // ───── CRUD ─────

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var resp = await Client.GetAsync("/api/orgunits");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<OrgUnitResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    [Fact]
    public async Task Create_ValidOrgUnit_ReturnsCreated()
    {
        var ou = await CreateOrgUnit("DEPT-001", "Engineering", OrgUnitType.Department);

        Assert.Equal("DEPT-001", ou.Code);
        Assert.Equal("Engineering", ou.Name);
        Assert.Equal("Department", ou.Type);
        Assert.True(ou.IsActive);
        Assert.Null(ou.ParentId);
        Assert.NotEqual(Guid.Empty, ou.Id);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await CreateOrgUnit("DUP-OU-001", "First");

        var dto = new OrgUnitCreateDto("DUP-OU-001", "Second");
        var resp = await Client.PostAsJsonAsync("/api/orgunits", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Create_WithParent_SetsParentRelationship()
    {
        var parent = await CreateOrgUnit("PAR-001", "Quality Department");
        var child = await CreateOrgUnit("CHI-001", "Incoming Inspection", OrgUnitType.WorkArea, parent.Id);

        Assert.Equal(parent.Id, child.ParentId);
        Assert.Equal("Quality Department", child.ParentName);
    }

    [Fact]
    public async Task Create_WithInvalidParent_ReturnsBadRequest()
    {
        var dto = new OrgUnitCreateDto("BAD-PAR", "Orphan", OrgUnitType.Department, Guid.NewGuid());
        var resp = await Client.PostAsJsonAsync("/api/orgunits", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingUnit_ReturnsUnit()
    {
        var created = await CreateOrgUnit("GET-001", "Assembly");

        var resp = await Client.GetAsync($"/api/orgunits/{created.Id}");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("GET-001", result!.Code);
        Assert.Equal("Assembly", result.Name);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var resp = await Client.GetAsync($"/api/orgunits/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingUnit_UpdatesFields()
    {
        var created = await CreateOrgUnit("UPD-001", "Old Name", OrgUnitType.Department);

        var updateDto = new OrgUnitUpdateDto("New Name", OrgUnitType.WorkArea, null, false);
        var resp = await Client.PutAsJsonAsync($"/api/orgunits/{created.Id}", updateDto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions);
        Assert.Equal("New Name", result!.Name);
        Assert.Equal("WorkArea", result.Type);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Update_CircularParent_ReturnsBadRequest()
    {
        var parent = await CreateOrgUnit("CIRC-001", "Parent");
        var child = await CreateOrgUnit("CIRC-002", "Child", parentId: parent.Id);

        // Try to make parent a child of its own child
        var updateDto = new OrgUnitUpdateDto("Parent", OrgUnitType.Department, child.Id);
        var resp = await Client.PutAsJsonAsync($"/api/orgunits/{parent.Id}", updateDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Update_SelfParent_ReturnsBadRequest()
    {
        var ou = await CreateOrgUnit("SELF-001", "Self Ref");

        var updateDto = new OrgUnitUpdateDto("Self Ref", OrgUnitType.Department, ou.Id);
        var resp = await Client.PutAsJsonAsync($"/api/orgunits/{ou.Id}", updateDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingUnit_ReturnsNoContent()
    {
        var created = await CreateOrgUnit("DEL-001", "To Delete");

        var resp = await Client.DeleteAsync($"/api/orgunits/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        // Verify deleted
        var getResp = await Client.GetAsync($"/api/orgunits/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task Delete_UnitWithChildren_ReturnsConflict()
    {
        var parent = await CreateOrgUnit("DELP-001", "Parent With Children");
        await CreateOrgUnit("DELC-001", "Child", parentId: parent.Id);

        var resp = await Client.DeleteAsync($"/api/orgunits/{parent.Id}");
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ───── Filtering ─────

    [Fact]
    public async Task GetAll_FilterByType_ReturnsFilteredResults()
    {
        await CreateOrgUnit("FLT-001", "Dept One", OrgUnitType.Department);
        await CreateOrgUnit("FLT-002", "Role One", OrgUnitType.Role);

        var resp = await Client.GetAsync("/api/orgunits?type=Role");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<OrgUnitResponseDto>>(JsonOptions);
        Assert.All(result!.Items, item => Assert.Equal("Role", item.Type));
    }

    [Fact]
    public async Task GetAll_TopLevelOnly_ExcludesChildren()
    {
        var parent = await CreateOrgUnit("TL-001", "Top Level");
        await CreateOrgUnit("TL-002", "Child Level", parentId: parent.Id);

        var resp = await Client.GetAsync("/api/orgunits?topLevelOnly=true");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<OrgUnitResponseDto>>(JsonOptions);
        Assert.DoesNotContain(result!.Items, item => item.ParentId.HasValue);
    }

    // ───── Children Endpoint ─────

    [Fact]
    public async Task GetChildren_ReturnsOnlyDirectChildren()
    {
        var parent = await CreateOrgUnit("GCH-001", "Parent");
        var child1 = await CreateOrgUnit("GCH-002", "Child 1", parentId: parent.Id);
        var child2 = await CreateOrgUnit("GCH-003", "Child 2", parentId: parent.Id);
        // Grandchild should NOT appear
        await CreateOrgUnit("GCH-004", "Grandchild", parentId: child1.Id);

        var resp = await Client.GetAsync($"/api/orgunits/{parent.Id}/children");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<List<OrgUnitResponseDto>>(JsonOptions);
        Assert.Equal(2, result!.Count);
        Assert.Contains(result, r => r.Code == "GCH-002");
        Assert.Contains(result, r => r.Code == "GCH-003");
    }

    [Fact]
    public async Task GetById_IncludesChildCount()
    {
        var parent = await CreateOrgUnit("CC-001", "Parent For Count");
        await CreateOrgUnit("CC-002", "Child A", parentId: parent.Id);
        await CreateOrgUnit("CC-003", "Child B", parentId: parent.Id);

        var resp = await Client.GetAsync($"/api/orgunits/{parent.Id}");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions);
        Assert.Equal(2, result!.ChildCount);
    }

    // ───── All OrgUnitType Values ─────

    [Theory]
    [InlineData("Department")]
    [InlineData("WorkArea")]
    [InlineData("Role")]
    [InlineData("Person")]
    public async Task Create_AllTypes_Succeeds(string typeName)
    {
        var type = Enum.Parse<OrgUnitType>(typeName);
        var code = $"TYPE-{typeName[..3].ToUpper()}";
        var ou = await CreateOrgUnit(code, $"{typeName} Unit", type);

        Assert.Equal(typeName, ou.Type);
    }
}
