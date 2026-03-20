using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class OrgUnitMemberTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public OrgUnitMemberTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

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

    /// <summary>Creates a real ApplicationUser in the DB and returns the user ID.</summary>
    private string CreateTestUser(string userName, string? displayName = null, string? email = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email ?? $"{userName}@test.local",
            NormalizedEmail = (email ?? $"{userName}@test.local").ToUpperInvariant(),
            DisplayName = displayName,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    // ───── GetMembers ─────

    [Fact]
    public async Task GetMembers_EmptyOrgUnit_ReturnsEmptyList()
    {
        var ou = await CreateOrgUnit("MEM-EMPTY", "Empty Unit");

        var resp = await Client.GetAsync($"/api/orgunits/{ou.Id}/members");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<List<OrgUnitMemberResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public async Task GetMembers_NonExistentOrgUnit_ReturnsNotFound()
    {
        var resp = await Client.GetAsync($"/api/orgunits/{Guid.NewGuid()}/members");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ───── AddMember ─────

    [Fact]
    public async Task AddMember_ValidUser_ReturnsCreated()
    {
        var ou = await CreateOrgUnit("MEM-ADD", "Add Member Unit");
        var userId = CreateTestUser("alice", "Alice Johnson", "alice@test.local");

        var dto = new OrgUnitMemberAddDto(userId);
        var resp = await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OrgUnitMemberResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(userId, result!.UserId);
        Assert.Equal("alice", result.UserName);
        Assert.Equal("Alice Johnson", result.DisplayName);
        Assert.Equal(ou.Id, result.OrgUnitId);
        Assert.Equal(ou.Name, result.OrgUnitName);
    }

    [Fact]
    public async Task AddMember_DuplicateUser_ReturnsConflict()
    {
        var ou = await CreateOrgUnit("MEM-DUP", "Dup Member Unit");
        var userId = CreateTestUser("bob-dup");

        var dto = new OrgUnitMemberAddDto(userId);
        await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", dto, JsonOptions);

        // Try adding same user again
        var resp = await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task AddMember_InvalidUser_ReturnsBadRequest()
    {
        var ou = await CreateOrgUnit("MEM-INV", "Invalid User Unit");

        var dto = new OrgUnitMemberAddDto("non-existent-user-id");
        var resp = await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AddMember_NonExistentOrgUnit_ReturnsNotFound()
    {
        var userId = CreateTestUser("charlie-nf");

        var dto = new OrgUnitMemberAddDto(userId);
        var resp = await Client.PostAsJsonAsync($"/api/orgunits/{Guid.NewGuid()}/members", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ───── RemoveMember ─────

    [Fact]
    public async Task RemoveMember_ExistingMember_ReturnsNoContent()
    {
        var ou = await CreateOrgUnit("MEM-REM", "Remove Member Unit");
        var userId = CreateTestUser("dave-rem");

        var addDto = new OrgUnitMemberAddDto(userId);
        var addResp = await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", addDto, JsonOptions);
        var member = await addResp.Content.ReadFromJsonAsync<OrgUnitMemberResponseDto>(JsonOptions);

        var resp = await Client.DeleteAsync($"/api/orgunits/{ou.Id}/members/{member!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        // Verify removed
        var membersResp = await Client.GetAsync($"/api/orgunits/{ou.Id}/members");
        var members = await membersResp.Content.ReadFromJsonAsync<List<OrgUnitMemberResponseDto>>(JsonOptions);
        Assert.Empty(members!);
    }

    [Fact]
    public async Task RemoveMember_NonExistentMember_ReturnsNotFound()
    {
        var ou = await CreateOrgUnit("MEM-RNF", "Remove NF Unit");

        var resp = await Client.DeleteAsync($"/api/orgunits/{ou.Id}/members/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ───── Multiple Members ─────

    [Fact]
    public async Task GetMembers_MultipleMembers_ReturnsAll()
    {
        var ou = await CreateOrgUnit("MEM-MULTI", "Multi Member Unit");
        var user1 = CreateTestUser("user-multi-1", "User One");
        var user2 = CreateTestUser("user-multi-2", "User Two");

        await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", new OrgUnitMemberAddDto(user1), JsonOptions);
        await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", new OrgUnitMemberAddDto(user2), JsonOptions);

        var resp = await Client.GetAsync($"/api/orgunits/{ou.Id}/members");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<List<OrgUnitMemberResponseDto>>(JsonOptions);
        Assert.Equal(2, result!.Count);
    }

    // ───── User's OrgUnits ─────

    [Fact]
    public async Task GetUserOrgUnits_UserWithMemberships_ReturnsList()
    {
        var ou1 = await CreateOrgUnit("UOU-001", "Engineering");
        var ou2 = await CreateOrgUnit("UOU-002", "Quality");
        var userId = CreateTestUser("user-orgunits");

        await Client.PostAsJsonAsync($"/api/orgunits/{ou1.Id}/members", new OrgUnitMemberAddDto(userId), JsonOptions);
        await Client.PostAsJsonAsync($"/api/orgunits/{ou2.Id}/members", new OrgUnitMemberAddDto(userId), JsonOptions);

        var resp = await Client.GetAsync($"/api/users/{userId}/orgunits");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<List<UserOrgUnitResponseDto>>(JsonOptions);
        Assert.Equal(2, result!.Count);
        Assert.Contains(result, r => r.OrgUnitCode == "UOU-001");
        Assert.Contains(result, r => r.OrgUnitCode == "UOU-002");
    }

    [Fact]
    public async Task GetUserOrgUnits_NonExistentUser_ReturnsNotFound()
    {
        var resp = await Client.GetAsync($"/api/users/non-existent-id/orgunits");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ───── MemberCount in OrgUnit response ─────

    [Fact]
    public async Task GetById_IncludesMemberCount()
    {
        var ou = await CreateOrgUnit("MC-001", "Member Count Unit");
        var user1 = CreateTestUser("mc-user-1");
        var user2 = CreateTestUser("mc-user-2");

        await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", new OrgUnitMemberAddDto(user1), JsonOptions);
        await Client.PostAsJsonAsync($"/api/orgunits/{ou.Id}/members", new OrgUnitMemberAddDto(user2), JsonOptions);

        var resp = await Client.GetAsync($"/api/orgunits/{ou.Id}");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions);
        Assert.Equal(2, result!.MemberCount);
    }

}
