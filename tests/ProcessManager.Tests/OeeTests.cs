using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class OeeTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OeeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ShiftDefinitionResponseDto> CreateShiftAsync(string? code = null, string startTime = "06:00", string endTime = "14:00")
    {
        var dto = new CreateShiftDefinitionDto
        {
            Code = code ?? $"SH-{Guid.NewGuid():N}"[..10],
            Name = $"Test Shift {Guid.NewGuid():N}"[..20],
            StartTime = startTime,
            EndTime = endTime
        };
        var resp = await _client.PostAsJsonAsync("/api/oee/shifts", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ShiftDefinitionResponseDto>(Json))!;
    }

    private async Task<Guid> CreateEquipmentAsync(string? code = null)
    {
        var catCode = $"CAT-{Guid.NewGuid():N}"[..10];
        var catResp = await _client.PostAsJsonAsync("/api/equipment/categories",
            new { Code = catCode, Name = $"Test Category {catCode}" }, Json);
        catResp.EnsureSuccessStatusCode();
        var cat = await catResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var catId = cat.GetProperty("id").GetGuid();

        var eqCode = code ?? $"EQ-{Guid.NewGuid():N}"[..10];
        var eqResp = await _client.PostAsJsonAsync("/api/equipment",
            new { Code = eqCode, Name = $"Test Equipment {eqCode}", CategoryId = catId }, Json);
        eqResp.EnsureSuccessStatusCode();
        var eq = await eqResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        return eq.GetProperty("id").GetGuid();
    }

    private async Task CreateDowntimeAsync(Guid equipmentId, DateTime start, DateTime? end, string reason = "Breakdown", string type = "Unplanned")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantCtx.BeginScope(TestWebApplicationFactory.DefaultTenantId, false);

        db.DowntimeRecords.Add(new DowntimeRecord
        {
            EquipmentId = equipmentId,
            Type = Enum.Parse<DowntimeType>(type),
            StartedAt = start,
            EndedAt = end,
            Reason = reason,
            TenantId = TestWebApplicationFactory.DefaultTenantId
        });
        await db.SaveChangesAsync();
    }

    // ── Shift CRUD Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateShift_ReturnsCreated()
    {
        var shift = await CreateShiftAsync("DAY-01", "06:00", "14:00");

        Assert.Equal("DAY-01", shift.Code);
        Assert.Equal("06:00", shift.StartTime);
        Assert.Equal("14:00", shift.EndTime);
        Assert.True(shift.IsActive);
    }

    [Fact]
    public async Task CreateShift_DuplicateCode_ReturnsConflict()
    {
        var code = $"DUP-{Guid.NewGuid():N}"[..10];
        await CreateShiftAsync(code);

        var dto = new CreateShiftDefinitionDto
        {
            Code = code,
            Name = "Duplicate shift",
            StartTime = "06:00",
            EndTime = "14:00"
        };
        var resp = await _client.PostAsJsonAsync("/api/oee/shifts", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task CreateShift_InvalidTime_ReturnsBadRequest()
    {
        var dto = new CreateShiftDefinitionDto
        {
            Code = $"BAD-{Guid.NewGuid():N}"[..10],
            Name = "Bad Shift",
            StartTime = "invalid",
            EndTime = "14:00"
        };
        var resp = await _client.PostAsJsonAsync("/api/oee/shifts", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetShift_ReturnsShift()
    {
        var created = await CreateShiftAsync();

        var resp = await _client.GetAsync($"/api/oee/shifts/{created.Id}");
        resp.EnsureSuccessStatusCode();
        var shift = await resp.Content.ReadFromJsonAsync<ShiftDefinitionResponseDto>(Json);
        Assert.Equal(created.Code, shift!.Code);
    }

    [Fact]
    public async Task GetShift_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/oee/shifts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateShift_UpdatesFields()
    {
        var created = await CreateShiftAsync();

        var updateDto = new UpdateShiftDefinitionDto
        {
            Name = "Updated Shift Name",
            StartTime = "07:00",
            EndTime = "15:00",
            IsActive = false
        };
        var resp = await _client.PutAsJsonAsync($"/api/oee/shifts/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<ShiftDefinitionResponseDto>(Json);

        Assert.Equal("Updated Shift Name", updated!.Name);
        Assert.Equal("07:00", updated.StartTime);
        Assert.Equal("15:00", updated.EndTime);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteShift_ReturnsNoContent()
    {
        var created = await CreateShiftAsync();

        var resp = await _client.DeleteAsync($"/api/oee/shifts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await _client.GetAsync($"/api/oee/shifts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task ListShifts_ReturnsList()
    {
        await CreateShiftAsync();
        await CreateShiftAsync();

        var resp = await _client.GetAsync("/api/oee/shifts");
        resp.EnsureSuccessStatusCode();
        var shifts = await resp.Content.ReadFromJsonAsync<List<ShiftDefinitionResponseDto>>(Json);
        Assert.True(shifts!.Count >= 2);
    }

    [Fact]
    public async Task ListShifts_ActiveOnly_FiltersInactive()
    {
        var active = await CreateShiftAsync();
        var inactive = await CreateShiftAsync();

        var updateDto = new UpdateShiftDefinitionDto
        {
            Name = "Inactive",
            StartTime = "06:00",
            EndTime = "14:00",
            IsActive = false
        };
        await _client.PutAsJsonAsync($"/api/oee/shifts/{inactive.Id}", updateDto, Json);

        var resp = await _client.GetAsync("/api/oee/shifts?activeOnly=true");
        resp.EnsureSuccessStatusCode();
        var shifts = await resp.Content.ReadFromJsonAsync<List<ShiftDefinitionResponseDto>>(Json);
        Assert.DoesNotContain(shifts!, s => s.Id == inactive.Id);
    }

    // ── OEE Dashboard Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_NoShifts_ReturnsEmptyDashboard()
    {
        // Use a unique tenant or just test on dates with no data
        var resp = await _client.GetAsync("/api/oee/dashboard?fromDate=2020-01-01&toDate=2020-01-02");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<OeeDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard!.AverageOee);
    }

    [Fact]
    public async Task Dashboard_WithShiftsAndEquipment_ReturnsData()
    {
        await CreateShiftAsync(null, "00:00", "23:59");
        var eqId = await CreateEquipmentAsync();

        var resp = await _client.GetAsync($"/api/oee/dashboard?equipmentId={eqId}");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<OeeDashboardDto>(Json);
        Assert.NotNull(dashboard);
    }

    [Fact]
    public async Task Dashboard_WithDowntime_AffectsAvailability()
    {
        var shift = await CreateShiftAsync(null, "00:00", "23:59");
        var eqId = await CreateEquipmentAsync();

        var today = DateTime.UtcNow.Date;
        await CreateDowntimeAsync(eqId, today.AddHours(2), today.AddHours(6), "Machine failure");

        var resp = await _client.GetAsync($"/api/oee/dashboard?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}&equipmentId={eqId}");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<OeeDashboardDto>(Json);
        Assert.NotNull(dashboard);
    }

    // ── OEE Trend Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Trend_InvalidEquipment_Returns404()
    {
        var resp = await _client.GetAsync($"/api/oee/trend/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Trend_ValidEquipment_ReturnsTrend()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateShiftAsync(null, "00:00", "23:59");

        var resp = await _client.GetAsync($"/api/oee/trend/{eqId}?fromDate=2020-01-01&toDate=2020-01-02");
        resp.EnsureSuccessStatusCode();
        var trend = await resp.Content.ReadFromJsonAsync<OeeTrendDto>(Json);
        Assert.Equal(eqId, trend!.EquipmentId);
    }

    // ── OEE Losses Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task Losses_ReturnsLossList()
    {
        var eqId = await CreateEquipmentAsync();
        var today = DateTime.UtcNow.Date;
        await CreateDowntimeAsync(eqId, today.AddHours(1), today.AddHours(3), "Tooling failure", "Unplanned");
        await CreateDowntimeAsync(eqId, today.AddHours(5), today.AddHours(6), "PM", "Planned");

        var resp = await _client.GetAsync($"/api/oee/losses?equipmentId={eqId}");
        resp.EnsureSuccessStatusCode();
        var losses = await resp.Content.ReadFromJsonAsync<List<OeeLossCategoryDto>>(Json);
        Assert.NotNull(losses);
        Assert.True(losses!.Count >= 2);
    }

    [Fact]
    public async Task Losses_FilterByDateRange_Works()
    {
        var eqId = await CreateEquipmentAsync();
        var pastDate = DateTime.UtcNow.Date.AddDays(-30);
        await CreateDowntimeAsync(eqId, pastDate, pastDate.AddHours(2), "Old failure");

        var resp = await _client.GetAsync($"/api/oee/losses?fromDate={pastDate:yyyy-MM-dd}&toDate={pastDate.AddDays(1):yyyy-MM-dd}&equipmentId={eqId}");
        resp.EnsureSuccessStatusCode();
        var losses = await resp.Content.ReadFromJsonAsync<List<OeeLossCategoryDto>>(Json);
        Assert.NotNull(losses);
        Assert.Contains(losses!, l => l.Category == "Old failure");
    }

    // ── OEE Calculate Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_MissingShift_ReturnsBadRequest()
    {
        var eqId = await CreateEquipmentAsync();
        var resp = await _client.GetAsync($"/api/oee/calculate?equipmentId={eqId}&shiftDate=2024-01-01&shiftId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Calculate_MissingEquipment_ReturnsNotFound()
    {
        var shift = await CreateShiftAsync();
        var resp = await _client.GetAsync($"/api/oee/calculate?equipmentId={Guid.NewGuid()}&shiftDate=2024-01-01&shiftId={shift.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Calculate_ValidParameters_ReturnsSnapshot()
    {
        var shift = await CreateShiftAsync(null, "06:00", "14:00");
        var eqId = await CreateEquipmentAsync();
        var today = DateTime.UtcNow.Date;

        var resp = await _client.GetAsync($"/api/oee/calculate?equipmentId={eqId}&shiftDate={today:yyyy-MM-dd}&shiftId={shift.Id}");
        resp.EnsureSuccessStatusCode();
        var snapshot = await resp.Content.ReadFromJsonAsync<OeeSnapshotDto>(Json);
        Assert.Equal(eqId, snapshot!.EquipmentId);
        Assert.Equal(shift.Code, snapshot.ShiftCode);
    }

    // ── Auth Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Endpoints_RequireAuth()
    {
        var anonClient = _factory.CreateClient();

        var resp1 = await anonClient.GetAsync("/api/oee/shifts");
        Assert.Equal(HttpStatusCode.Unauthorized, resp1.StatusCode);

        var resp2 = await anonClient.GetAsync("/api/oee/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resp2.StatusCode);
    }

    // ── Cross-Tenant Isolation Test ─────────────────────────────────────────

    [Fact]
    public async Task Shifts_CrossTenantIsolation()
    {
        var shift = await CreateShiftAsync();

        var otherClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await otherClient.GetAsync($"/api/oee/shifts/{shift.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ───────────────────────────────────────────────────────

    [Fact]
    public async Task McpGetOeeStatus_ReturnsData()
    {
        await CreateShiftAsync(null, "00:00", "23:59");
        await CreateEquipmentAsync();

        var mcpPayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_oee_status",
                arguments = new { days = "7" }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpPayload, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("OEE Status Summary", body);
    }
}
