using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class WorkstationAdminTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public WorkstationAdminTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    private async Task<Guid> CreateStorageLocation(string code, string name)
    {
        var dto = new { Code = code, Name = name };
        var resp = await _client.PostAsJsonAsync("/api/warehouse/locations", dto, Json);
        resp.EnsureSuccessStatusCode();
        var loc = await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
        return loc.GetProperty("id").GetGuid();
    }

    private async Task<WorkstationResponseDto> CreateWorkstation(string code, string name, Guid locationId)
    {
        var dto = new CreateWorkstationDto { Code = code, Name = name, FixedLocationId = locationId };
        var resp = await _client.PostAsJsonAsync("/api/admin/workstations", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkstationResponseDto>(Json))!;
    }

    private async Task<ApiKeyCreatedDto> CreateApiKey(string name, Guid workstationId, DateTime? expiresAt = null)
    {
        var dto = new CreateApiKeyDto { Name = name, WorkstationId = workstationId, ExpiresAt = expiresAt };
        var resp = await _client.PostAsJsonAsync("/api/admin/api-keys", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ApiKeyCreatedDto>(Json))!;
    }

    // ── Workstation GetById returns full detail ──

    [Fact]
    public async Task Workstation_GetById_ReturnsFullDetail()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-GBI-{pfx}", "Detail Loc");
        var ws = await CreateWorkstation($"WS-GBI-{pfx}", "Detail Cell", locId);

        var resp = await _client.GetAsync($"/api/admin/workstations/{ws.Id}");
        resp.EnsureSuccessStatusCode();
        var detail = await resp.Content.ReadFromJsonAsync<WorkstationResponseDto>(Json);

        Assert.NotNull(detail);
        Assert.Equal(ws.Id, detail.Id);
        Assert.Equal($"WS-GBI-{pfx}", detail.Code);
        Assert.Equal("Detail Cell", detail.Name);
        Assert.Equal(locId, detail.FixedLocationId);
        Assert.True(detail.IsActive);
        Assert.Equal(0, detail.ApiKeyCount);
    }

    [Fact]
    public async Task Workstation_GetById_NotFound()
    {
        var resp = await _client.GetAsync($"/api/admin/workstations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Workstation search filter ──

    [Fact]
    public async Task Workstation_List_SearchByName()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-SN-{pfx}", "Search Loc");
        await CreateWorkstation($"WS-SN-{pfx}", $"UniqueSearchTerm{pfx}", locId);

        var resp = await _client.GetAsync($"/api/admin/workstations?search=UniqueSearchTerm{pfx}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<WorkstationSummaryDto>>(Json);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal($"WS-SN-{pfx}", result.Items[0].Code);
    }

    // ── Workstation with inactive location rejected ──

    [Fact]
    public async Task Workstation_Create_WithInvalidLocation_ReturnsBadRequest()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var dto = new CreateWorkstationDto
        {
            Code = $"WS-INV-{pfx}",
            Name = "Invalid Loc Cell",
            FixedLocationId = Guid.NewGuid()
        };

        var resp = await _client.PostAsJsonAsync("/api/admin/workstations", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Workstation update changes location ──

    [Fact]
    public async Task Workstation_Update_ChangesLocation()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var loc1 = await CreateStorageLocation($"LOC-CL1-{pfx}", "Loc A");
        var loc2 = await CreateStorageLocation($"LOC-CL2-{pfx}", "Loc B");
        var ws = await CreateWorkstation($"WS-CL-{pfx}", "Move Cell", loc1);

        var updateDto = new UpdateWorkstationDto { Name = "Move Cell", FixedLocationId = loc2, IsActive = true };
        var resp = await _client.PutAsJsonAsync($"/api/admin/workstations/{ws.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<WorkstationResponseDto>(Json);

        Assert.Equal(loc2, updated!.FixedLocationId);
    }

    // ── Workstation deactivation via update ──

    [Fact]
    public async Task Workstation_Update_Deactivate()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-DA-{pfx}", "Deact Loc");
        var ws = await CreateWorkstation($"WS-DA-{pfx}", "Deact Cell", locId);

        var updateDto = new UpdateWorkstationDto { Name = "Deact Cell", FixedLocationId = locId, IsActive = false };
        var resp = await _client.PutAsJsonAsync($"/api/admin/workstations/{ws.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<WorkstationResponseDto>(Json);

        Assert.False(updated!.IsActive);
    }

    // ── API Key: past expiry rejected ──

    [Fact]
    public async Task ApiKey_Create_PastExpiry_ReturnsBadRequest()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-PE-{pfx}", "Past Exp Loc");
        var ws = await CreateWorkstation($"WS-PE-{pfx}", "Past Exp Cell", locId);

        var dto = new CreateApiKeyDto
        {
            Name = "Expired Key",
            WorkstationId = ws.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        var resp = await _client.PostAsJsonAsync("/api/admin/api-keys", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── API Key: create for inactive workstation rejected ──

    [Fact]
    public async Task ApiKey_Create_InactiveWorkstation_ReturnsBadRequest()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-IW-{pfx}", "Inactive WS Loc");
        var ws = await CreateWorkstation($"WS-IW-{pfx}", "Inactive WS Cell", locId);

        // Deactivate via delete (soft-delete)
        await _client.DeleteAsync($"/api/admin/workstations/{ws.Id}");

        var dto = new CreateApiKeyDto { Name = "Should Fail", WorkstationId = ws.Id };
        var resp = await _client.PostAsJsonAsync("/api/admin/api-keys", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── API Key: GetById returns detail ──

    [Fact]
    public async Task ApiKey_GetById_ReturnsDetail()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-KB-{pfx}", "Key Detail Loc");
        var ws = await CreateWorkstation($"WS-KB-{pfx}", "Key Detail Cell", locId);
        var key = await CreateApiKey("Detail Key", ws.Id);

        var resp = await _client.GetAsync($"/api/admin/api-keys/{key.Id}");
        resp.EnsureSuccessStatusCode();
        var detail = await resp.Content.ReadFromJsonAsync<ApiKeyResponseDto>(Json);

        Assert.NotNull(detail);
        Assert.Equal(key.Id, detail.Id);
        Assert.Equal("Detail Key", detail.Name);
        Assert.True(detail.IsActive);
        Assert.Equal(ws.Id, detail.WorkstationId);
    }

    // ── API Key: filter by active status ──

    [Fact]
    public async Task ApiKey_List_FilterByActive()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-AF-{pfx}", "Active Filter Loc");
        var ws = await CreateWorkstation($"WS-AF-{pfx}", "Active Filter Cell", locId);
        var key1 = await CreateApiKey("Active Key", ws.Id);
        var key2 = await CreateApiKey("Inactive Key", ws.Id);

        // Deactivate one
        await _client.PatchAsJsonAsync($"/api/admin/api-keys/{key2.Id}",
            new UpdateApiKeyDto { Name = "Inactive Key", IsActive = false }, Json);

        var resp = await _client.GetAsync($"/api/admin/api-keys?workstationId={ws.Id}&active=true");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<ApiKeyResponseDto>>(Json);

        Assert.NotNull(result);
        Assert.All(result.Items, k => Assert.True(k.IsActive));
    }

    // ── Role gating: non-admin cannot access ──

    [Fact]
    public async Task Workstation_Endpoints_RequireAdminRole()
    {
        using var operatorClient = _factory.CreateAuthenticatedClient("operator-user", "Operator");
        var resp = await operatorClient.GetAsync("/api/admin/workstations");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task ApiKey_Endpoints_RequireAdminRole()
    {
        using var operatorClient = _factory.CreateAuthenticatedClient("operator-user", "Operator");
        var resp = await operatorClient.GetAsync("/api/admin/api-keys");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── API Key: future expiry accepted ──

    [Fact]
    public async Task ApiKey_Create_FutureExpiry_Succeeds()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-FE-{pfx}", "Future Exp Loc");
        var ws = await CreateWorkstation($"WS-FE-{pfx}", "Future Exp Cell", locId);

        var key = await CreateApiKey("Future Key", ws.Id, DateTime.UtcNow.AddDays(90));
        Assert.NotNull(key.RawKey);
        Assert.StartsWith("pk_", key.RawKey);
    }

    // ── Workstation: ApiKeyCount reflects actual key count ──

    [Fact]
    public async Task Workstation_List_ApiKeyCount_Accurate()
    {
        var pfx = Guid.NewGuid().ToString()[..4];
        var locId = await CreateStorageLocation($"LOC-KC-{pfx}", "Count Loc");
        var ws = await CreateWorkstation($"WS-KC-{pfx}", "Count Cell", locId);
        await CreateApiKey("Key A", ws.Id);
        await CreateApiKey("Key B", ws.Id);

        var resp = await _client.GetAsync($"/api/admin/workstations?search=WS-KC-{pfx}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<WorkstationSummaryDto>>(Json);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ApiKeyCount);
    }
}
