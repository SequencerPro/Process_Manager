using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class Phase21Tests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Phase21Tests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateStorageLocation(string code, string name, string? zone = null)
    {
        var dto = new { Code = code, Name = name, Zone = zone };
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

    private async Task<ApiKeyCreatedDto> CreateApiKey(string name, Guid workstationId)
    {
        var dto = new CreateApiKeyDto { Name = name, WorkstationId = workstationId };
        var resp = await _client.PostAsJsonAsync("/api/admin/api-keys", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ApiKeyCreatedDto>(Json))!;
    }

    private HttpClient CreateApiKeyClient(string rawKey)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", rawKey);
        return client;
    }

    private async Task<Guid> CreateItemWithBarcode(string barcode, string? serialNumber = null)
    {
        var kindDto = new KindCreateDto($"K-{Guid.NewGuid().ToString()[..6]}", "Test Kind", null, true, false);
        var kindResp = await _client.PostAsJsonAsync("/api/kinds", kindDto, Json);
        kindResp.EnsureSuccessStatusCode();
        var kind = await kindResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var kindId = kind.GetProperty("id").GetGuid();

        var gradeDto = new GradeCreateDto("STD", "Standard", null, true, 0);
        var gradeResp = await _client.PostAsJsonAsync($"/api/kinds/{kindId}/grades", gradeDto, Json);
        gradeResp.EnsureSuccessStatusCode();
        var grade = await gradeResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var gradeId = grade.GetProperty("id").GetGuid();

        var proc = new ProcessCreateDto($"P-{Guid.NewGuid().ToString()[..6]}", "Test Process", null);
        var procResp = await _client.PostAsJsonAsync("/api/processes", proc, Json);
        procResp.EnsureSuccessStatusCode();
        var procEl = await procResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var processId = procEl.GetProperty("id").GetGuid();

        var releaseDto = new AdminReleaseDocumentDto(null, null, null);
        await _client.PostAsJsonAsync($"/api/processes/{processId}/admin-release", releaseDto, Json);

        var jobDto = new CreateJobDto($"J-{Guid.NewGuid().ToString()[..6]}", "Test Job", null, processId, 0);
        var jobResp = await _client.PostAsJsonAsync("/api/jobs", jobDto, Json);
        jobResp.EnsureSuccessStatusCode();
        var job = await jobResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var jobId = job.GetProperty("id").GetGuid();

        var itemDto = new CreateItemDto(kindId, gradeId, jobId, serialNumber ?? barcode, null);
        var itemResp = await _client.PostAsJsonAsync("/api/items", itemDto, Json);
        itemResp.EnsureSuccessStatusCode();
        var item = await itemResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var itemId = item.GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var entity = await db.Items.FindAsync(itemId);
        entity!.Barcode = barcode;
        await db.SaveChangesAsync();

        return itemId;
    }

    private async Task SetItemLocation(Guid itemId, Guid locationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var item = await db.Items.FindAsync(itemId);
        item!.StorageLocationId = locationId;
        await db.SaveChangesAsync();
    }

    // ── Workstation CRUD Tests ──────────────────────────────────────────────

    [Fact]
    public async Task Workstation_Create_ReturnsCreated()
    {
        var locId = await CreateStorageLocation($"LOC-WS-{Guid.NewGuid().ToString()[..4]}", "Recv Dock");
        var ws = await CreateWorkstation($"WS-{Guid.NewGuid().ToString()[..4]}", "Assembly Cell 1", locId);

        Assert.NotEqual(Guid.Empty, ws.Id);
        Assert.Equal(locId, ws.FixedLocationId);
        Assert.True(ws.IsActive);
    }

    [Fact]
    public async Task Workstation_DuplicateCode_ReturnsConflict()
    {
        var locId = await CreateStorageLocation($"LOC-DUP-{Guid.NewGuid().ToString()[..4]}", "Location");
        var code = $"WS-DUP-{Guid.NewGuid().ToString()[..4]}";
        await CreateWorkstation(code, "First", locId);

        var dto = new CreateWorkstationDto { Code = code, Name = "Second", FixedLocationId = locId };
        var resp = await _client.PostAsJsonAsync("/api/admin/workstations", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Workstation_Update_ReturnsUpdated()
    {
        var locId = await CreateStorageLocation($"LOC-UPD-{Guid.NewGuid().ToString()[..4]}", "Loc A");
        var ws = await CreateWorkstation($"WS-UPD-{Guid.NewGuid().ToString()[..4]}", "Original", locId);

        var updateDto = new UpdateWorkstationDto { Name = "Renamed Cell", FixedLocationId = locId, IsActive = true };
        var resp = await _client.PutAsJsonAsync($"/api/admin/workstations/{ws.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<WorkstationResponseDto>(Json);
        Assert.Equal("Renamed Cell", updated!.Name);
    }

    [Fact]
    public async Task Workstation_Delete_NoActiveKeys_Succeeds()
    {
        var locId = await CreateStorageLocation($"LOC-DEL-{Guid.NewGuid().ToString()[..4]}", "Loc B");
        var ws = await CreateWorkstation($"WS-DEL-{Guid.NewGuid().ToString()[..4]}", "To Delete", locId);

        var resp = await _client.DeleteAsync($"/api/admin/workstations/{ws.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Workstation_Delete_WithActiveKeys_ReturnsConflict()
    {
        var locId = await CreateStorageLocation($"LOC-DK-{Guid.NewGuid().ToString()[..4]}", "Loc C");
        var ws = await CreateWorkstation($"WS-DK-{Guid.NewGuid().ToString()[..4]}", "Has Key", locId);
        await CreateApiKey("Test Key", ws.Id);

        var resp = await _client.DeleteAsync($"/api/admin/workstations/{ws.Id}");
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Workstation_List_PaginatedAndFilterable()
    {
        var locId = await CreateStorageLocation($"LOC-LST-{Guid.NewGuid().ToString()[..4]}", "Loc D");
        await CreateWorkstation($"WS-LST-{Guid.NewGuid().ToString()[..4]}", "List Test", locId);

        var resp = await _client.GetAsync("/api/admin/workstations?active=true&page=1&pageSize=5");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<WorkstationSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    // ── API Key CRUD Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task ApiKey_Create_ReturnsRawKeyOnce()
    {
        var locId = await CreateStorageLocation($"LOC-AK-{Guid.NewGuid().ToString()[..4]}", "Loc E");
        var ws = await CreateWorkstation($"WS-AK-{Guid.NewGuid().ToString()[..4]}", "Key Cell", locId);
        var key = await CreateApiKey("Scanner Key", ws.Id);

        Assert.NotNull(key.RawKey);
        Assert.StartsWith("pk_", key.RawKey);
        Assert.NotEmpty(key.KeyPrefix);
        Assert.Equal(ws.Id, key.WorkstationId);
    }

    [Fact]
    public async Task ApiKey_List_FilterByWorkstation()
    {
        var locId = await CreateStorageLocation($"LOC-AKL-{Guid.NewGuid().ToString()[..4]}", "Loc F");
        var ws = await CreateWorkstation($"WS-AKL-{Guid.NewGuid().ToString()[..4]}", "Filter Cell", locId);
        await CreateApiKey("Key 1", ws.Id);
        await CreateApiKey("Key 2", ws.Id);

        var resp = await _client.GetAsync($"/api/admin/api-keys?workstationId={ws.Id}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<ApiKeyResponseDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
    }

    [Fact]
    public async Task ApiKey_Update_NameAndActive()
    {
        var locId = await CreateStorageLocation($"LOC-AKU-{Guid.NewGuid().ToString()[..4]}", "Loc G");
        var ws = await CreateWorkstation($"WS-AKU-{Guid.NewGuid().ToString()[..4]}", "Update Cell", locId);
        var key = await CreateApiKey("Old Name", ws.Id);

        var updateDto = new UpdateApiKeyDto { Name = "New Name", IsActive = false };
        var resp = await _client.PatchAsJsonAsync($"/api/admin/api-keys/{key.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<ApiKeyResponseDto>(Json);
        Assert.Equal("New Name", updated!.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ApiKey_Delete_HardRemoves()
    {
        var locId = await CreateStorageLocation($"LOC-AKD-{Guid.NewGuid().ToString()[..4]}", "Loc H");
        var ws = await CreateWorkstation($"WS-AKD-{Guid.NewGuid().ToString()[..4]}", "Delete Cell", locId);
        var key = await CreateApiKey("To Delete", ws.Id);

        var resp = await _client.DeleteAsync($"/api/admin/api-keys/{key.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await _client.GetAsync($"/api/admin/api-keys/{key.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    // ── API Key Authentication Tests ────────────────────────────────────────

    [Fact]
    public async Task ApiKeyAuth_ValidKey_Authenticates()
    {
        var locId = await CreateStorageLocation($"LOC-AUTH-{Guid.NewGuid().ToString()[..4]}", "Auth Loc");
        var ws = await CreateWorkstation($"WS-AUTH-{Guid.NewGuid().ToString()[..4]}", "Auth Cell", locId);
        var key = await CreateApiKey("Auth Test", ws.Id);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var resp = await apiClient.GetAsync("/api/warehouse/locations");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ApiKeyAuth_InvalidKey_Returns401()
    {
        using var apiClient = CreateApiKeyClient("pk_invalid_key_that_does_not_exist");
        var resp = await apiClient.GetAsync("/api/warehouse/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ApiKeyAuth_InactiveKey_Returns401()
    {
        var locId = await CreateStorageLocation($"LOC-INA-{Guid.NewGuid().ToString()[..4]}", "Inactive Loc");
        var ws = await CreateWorkstation($"WS-INA-{Guid.NewGuid().ToString()[..4]}", "Inactive Cell", locId);
        var key = await CreateApiKey("Inactive Key", ws.Id);

        var updateDto = new UpdateApiKeyDto { Name = "Inactive Key", IsActive = false };
        await _client.PatchAsJsonAsync($"/api/admin/api-keys/{key.Id}", updateDto, Json);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var resp = await apiClient.GetAsync("/api/warehouse/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Scan Endpoint Tests ��────────────────────────────────────────────────

    [Fact]
    public async Task Scan_TransfersItem_ToWorkstationLocation()
    {
        var fromLocId = await CreateStorageLocation($"LOC-SF-{Guid.NewGuid().ToString()[..4]}", "Source", "Raw");
        var toLocId = await CreateStorageLocation($"LOC-ST-{Guid.NewGuid().ToString()[..4]}", "Dest", "Assembly");
        var ws = await CreateWorkstation($"WS-SC-{Guid.NewGuid().ToString()[..4]}", "Scan Cell", toLocId);
        var key = await CreateApiKey("Scan Key", ws.Id);

        var barcode = $"BC-{Guid.NewGuid().ToString()[..6]}";
        var itemId = await CreateItemWithBarcode(barcode);
        await SetItemLocation(itemId, fromLocId);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = barcode };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("transferred", result!.Result);
        Assert.NotNull(result.TransactionId);
        Assert.Equal(barcode, result.Item!.Barcode);
    }

    [Fact]
    public async Task Scan_UnknownBarcode_Returns404()
    {
        var locId = await CreateStorageLocation($"LOC-UB-{Guid.NewGuid().ToString()[..4]}", "Unknown Loc");
        var ws = await CreateWorkstation($"WS-UB-{Guid.NewGuid().ToString()[..4]}", "Unknown Cell", locId);
        var key = await CreateApiKey("Unknown Key", ws.Id);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = "NONEXISTENT-BARCODE-99999" };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("unknown_barcode", body!.Result);
    }

    [Fact]
    public async Task Scan_AlreadyAtLocation_ReturnsIdempotent200()
    {
        var locId = await CreateStorageLocation($"LOC-AL-{Guid.NewGuid().ToString()[..4]}", "Same Loc");
        var ws = await CreateWorkstation($"WS-AL-{Guid.NewGuid().ToString()[..4]}", "Same Cell", locId);
        var key = await CreateApiKey("Already Key", ws.Id);

        var barcode = $"BC-AL-{Guid.NewGuid().ToString()[..6]}";
        var itemId = await CreateItemWithBarcode(barcode);
        await SetItemLocation(itemId, locId);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = barcode };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("already_at_location", result!.Result);
        Assert.Null(result.TransactionId);
    }

    [Fact]
    public async Task Scan_ConsumedItem_Returns409()
    {
        var locId = await CreateStorageLocation($"LOC-CI-{Guid.NewGuid().ToString()[..4]}", "Consumed Loc");
        var ws = await CreateWorkstation($"WS-CI-{Guid.NewGuid().ToString()[..4]}", "Consumed Cell", locId);
        var key = await CreateApiKey("Consumed Key", ws.Id);

        var barcode = $"BC-CI-{Guid.NewGuid().ToString()[..6]}";
        var itemId = await CreateItemWithBarcode(barcode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var item = await db.Items.FindAsync(itemId);
        item!.Status = ItemStatus.Consumed;
        await db.SaveChangesAsync();

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = barcode };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("invalid_item_status", body!.Result);
    }

    [Fact]
    public async Task Scan_ReceiptForUnlocatedItem()
    {
        var locId = await CreateStorageLocation($"LOC-RC-{Guid.NewGuid().ToString()[..4]}", "Receipt Loc");
        var ws = await CreateWorkstation($"WS-RC-{Guid.NewGuid().ToString()[..4]}", "Receipt Cell", locId);
        var key = await CreateApiKey("Receipt Key", ws.Id);

        var barcode = $"BC-RC-{Guid.NewGuid().ToString()[..6]}";
        await CreateItemWithBarcode(barcode);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = barcode };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("transferred", result!.Result);
        Assert.Null(result.FromLocation);
        Assert.NotNull(result.ToLocation);
    }

    [Fact]
    public async Task Scan_FallbackToSerialNumber()
    {
        var locId = await CreateStorageLocation($"LOC-SN-{Guid.NewGuid().ToString()[..4]}", "SN Loc");
        var ws = await CreateWorkstation($"WS-SN-{Guid.NewGuid().ToString()[..4]}", "SN Cell", locId);
        var key = await CreateApiKey("SN Key", ws.Id);

        var serial = $"SN-{Guid.NewGuid().ToString()[..6]}";
        await CreateItemWithBarcode($"DIFFERENT-BC-{Guid.NewGuid().ToString()[..4]}", serialNumber: serial);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        var scanDto = new ScanRequestDto { Barcode = serial };
        var resp = await apiClient.PostAsJsonAsync("/api/warehouse/scan", scanDto, Json);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ScanResponseDto>(Json);
        Assert.Equal("transferred", result!.Result);
    }

    [Fact]
    public async Task Scan_CreatesAuditScanEvent()
    {
        var locId = await CreateStorageLocation($"LOC-SE-{Guid.NewGuid().ToString()[..4]}", "Event Loc");
        var ws = await CreateWorkstation($"WS-SE-{Guid.NewGuid().ToString()[..4]}", "Event Cell", locId);
        var key = await CreateApiKey("Event Key", ws.Id);

        var barcode = $"BC-SE-{Guid.NewGuid().ToString()[..6]}";
        await CreateItemWithBarcode(barcode);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        await apiClient.PostAsJsonAsync("/api/warehouse/scan", new ScanRequestDto { Barcode = barcode }, Json);

        var evtsResp = await _client.GetAsync($"/api/warehouse/scan-events?workstationId={ws.Id}");
        evtsResp.EnsureSuccessStatusCode();
        var events = await evtsResp.Content.ReadFromJsonAsync<PaginatedResponse<ScanEventResponseDto>>(Json);
        Assert.True(events!.TotalCount >= 1);
        Assert.Contains(events.Items, e => e.ScannedBarcode == barcode);
    }

    // ── Scan Events Query Tests ─────────────────────────────────────────────

    [Fact]
    public async Task ScanEvents_RequiresAuth()
    {
        using var unauthClient = _factory.CreateClient();
        var resp = await unauthClient.GetAsync("/api/warehouse/scan-events");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ScanEvents_FilterByResult()
    {
        var locId = await CreateStorageLocation($"LOC-FR-{Guid.NewGuid().ToString()[..4]}", "Filter Loc");
        var ws = await CreateWorkstation($"WS-FR-{Guid.NewGuid().ToString()[..4]}", "Filter Cell", locId);
        var key = await CreateApiKey("Filter Key", ws.Id);

        using var apiClient = CreateApiKeyClient(key.RawKey);
        await apiClient.PostAsJsonAsync("/api/warehouse/scan", new ScanRequestDto { Barcode = "MISSING-BC-FILTER" }, Json);

        var resp = await _client.GetAsync($"/api/warehouse/scan-events?result=UnknownBarcode&workstationId={ws.Id}");
        resp.EnsureSuccessStatusCode();
        var events = await resp.Content.ReadFromJsonAsync<PaginatedResponse<ScanEventResponseDto>>(Json);
        Assert.NotNull(events);
        Assert.All(events.Items, e => Assert.Equal("UnknownBarcode", e.Result));
    }

    // ── Cross-Tenant Isolation ──────────────────────────────────────────────

    [Fact]
    public async Task Workstation_CrossTenant_Isolated()
    {
        var locId = await CreateStorageLocation($"LOC-CT-{Guid.NewGuid().ToString()[..4]}", "Tenant Loc");
        var ws = await CreateWorkstation($"WS-CT-{Guid.NewGuid().ToString()[..4]}", "Tenant Cell", locId);

        var otherTenantId = _factory.CreateTenant($"other-ws-{Guid.NewGuid().ToString()[..6]}");
        using var otherClient = _factory.CreateTenantClient(otherTenantId);

        var resp = await otherClient.GetAsync($"/api/admin/workstations/{ws.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ─��� MCP Tool Test ───────────────────────────────────────────────────────

    [Fact]
    public async Task McpTool_GetWorkstationStatus_ReturnsData()
    {
        var locId = await CreateStorageLocation($"LOC-MCP-{Guid.NewGuid().ToString()[..4]}", "MCP Loc");
        await CreateWorkstation($"WS-MCP-{Guid.NewGuid().ToString()[..4]}", "MCP Cell", locId);

        var mcpPayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_workstation_status",
                arguments = new { active_only = "true" }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpPayload, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Workstation Status", body);
    }
}
