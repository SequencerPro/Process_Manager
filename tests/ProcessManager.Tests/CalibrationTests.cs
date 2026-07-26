using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class CalibrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CalibrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    private async Task<CalibrationRecordResponseDto> CreateCalibrationRecord(Guid equipmentId, string result = "Pass")
    {
        var dto = new CreateCalibrationRecordDto
        {
            EquipmentId = equipmentId,
            CalibrationType = "Internal",
            CalibrationDate = DateTime.UtcNow.AddDays(-5),
            NextDueDate = DateTime.UtcNow.AddDays(360),
            CertificateNumber = $"CERT-{Guid.NewGuid():N}"[..12],
            Result = result,
            PerformedBy = "Test Technician",
            StandardsUsed = "ISO 10012",
            AsFoundReading = "0.001",
            AsLeftReading = "0.000",
            Uncertainty = 0.0005m,
            Notes = "Test calibration record"
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/records", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CalibrationRecordResponseDto>(Json))!;
    }

    private async Task<CalibrationScheduleResponseDto> CreateCalibrationSchedule(Guid equipmentId, string method = "Fixed")
    {
        var dto = new CreateCalibrationScheduleDto
        {
            EquipmentId = equipmentId,
            IntervalDays = 90,
            IntervalAdjustmentMethod = method,
            MaxIntervalDays = 365,
            MinIntervalDays = 30,
            ExtensionPercent = 25
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/schedules", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json))!;
    }

    // ── Record CRUD Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRecord_ReturnsCreated()
    {
        var eqId = await CreateEquipmentAsync();
        var record = await CreateCalibrationRecord(eqId);

        Assert.Equal(eqId, record.EquipmentId);
        Assert.Equal("Internal", record.CalibrationType);
        Assert.Equal("Pass", record.Result);
        Assert.Equal("Test Technician", record.PerformedBy);
        Assert.Equal(0.0005m, record.Uncertainty);
    }

    [Fact]
    public async Task CreateRecord_InvalidEquipment_ReturnsBadRequest()
    {
        var dto = new CreateCalibrationRecordDto
        {
            EquipmentId = Guid.NewGuid(),
            CalibrationType = "Internal",
            CalibrationDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(90),
            Result = "Pass"
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/records", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateRecord_InvalidResult_ReturnsBadRequest()
    {
        var eqId = await CreateEquipmentAsync();
        var dto = new CreateCalibrationRecordDto
        {
            EquipmentId = eqId,
            CalibrationType = "Internal",
            CalibrationDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddDays(90),
            Result = "InvalidResult"
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/records", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetRecordById_ReturnsRecord()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationRecord(eqId);

        var resp = await _client.GetAsync($"/api/calibration/records/{created.Id}");
        resp.EnsureSuccessStatusCode();

        var record = await resp.Content.ReadFromJsonAsync<CalibrationRecordResponseDto>(Json);
        Assert.NotNull(record);
        Assert.Equal(created.Id, record.Id);
        Assert.Equal("ISO 10012", record.StandardsUsed);
    }

    [Fact]
    public async Task GetRecordById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/calibration/records/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateRecord_ChangesFields()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationRecord(eqId);

        var updateDto = new UpdateCalibrationRecordDto
        {
            CalibrationType = "External",
            CalibrationDate = DateTime.UtcNow.AddDays(-1),
            NextDueDate = DateTime.UtcNow.AddDays(180),
            Result = "Limited",
            PerformedBy = "External Lab",
            StandardsUsed = "NIST traceable",
            Notes = "Updated notes"
        };

        var resp = await _client.PutAsJsonAsync($"/api/calibration/records/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<CalibrationRecordResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal("External", updated.CalibrationType);
        Assert.Equal("Limited", updated.Result);
        Assert.Equal("External Lab", updated.PerformedBy);
    }

    [Fact]
    public async Task DeleteRecord_ReturnsNoContent()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationRecord(eqId);

        var resp = await _client.DeleteAsync($"/api/calibration/records/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/calibration/records/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task GetRecords_Paginated()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationRecord(eqId);
        await CreateCalibrationRecord(eqId, "Fail");

        var resp = await _client.GetAsync($"/api/calibration/records?equipmentId={eqId}");
        resp.EnsureSuccessStatusCode();

        var page = await resp.Content.ReadFromJsonAsync<PaginatedResponse<CalibrationRecordSummaryDto>>(Json);
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
    }

    [Fact]
    public async Task GetRecords_FilterByResult()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationRecord(eqId, "Pass");
        await CreateCalibrationRecord(eqId, "Fail");

        var resp = await _client.GetAsync($"/api/calibration/records?equipmentId={eqId}&result=Fail");
        resp.EnsureSuccessStatusCode();

        var page = await resp.Content.ReadFromJsonAsync<PaginatedResponse<CalibrationRecordSummaryDto>>(Json);
        Assert.NotNull(page);
        Assert.All(page.Items, r => Assert.Equal("Fail", r.Result));
    }

    [Fact]
    public async Task GetEquipmentHistory_ReturnsList()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationRecord(eqId);
        await CreateCalibrationRecord(eqId);

        var resp = await _client.GetAsync($"/api/calibration/equipment/{eqId}/history");
        resp.EnsureSuccessStatusCode();

        var records = await resp.Content.ReadFromJsonAsync<List<CalibrationRecordSummaryDto>>(Json);
        Assert.NotNull(records);
        Assert.True(records.Count >= 2);
    }

    // ── Schedule Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSchedule_ReturnsCreated()
    {
        var eqId = await CreateEquipmentAsync();
        var schedule = await CreateCalibrationSchedule(eqId);

        Assert.Equal(eqId, schedule.EquipmentId);
        Assert.Equal(90, schedule.IntervalDays);
        Assert.Equal("Fixed", schedule.IntervalAdjustmentMethod);
        Assert.True(schedule.IsActive);
    }

    [Fact]
    public async Task CreateSchedule_DuplicateEquipment_ReturnsConflict()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationSchedule(eqId);

        var dto = new CreateCalibrationScheduleDto
        {
            EquipmentId = eqId,
            IntervalDays = 60,
            IntervalAdjustmentMethod = "Fixed",
            MaxIntervalDays = 365,
            MinIntervalDays = 30,
            ExtensionPercent = 25
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/schedules", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_InvalidEquipment_ReturnsBadRequest()
    {
        var dto = new CreateCalibrationScheduleDto
        {
            EquipmentId = Guid.NewGuid(),
            IntervalDays = 90,
            IntervalAdjustmentMethod = "Fixed",
            MaxIntervalDays = 365,
            MinIntervalDays = 30,
            ExtensionPercent = 25
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/schedules", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_MinExceedsMax_ReturnsBadRequest()
    {
        var eqId = await CreateEquipmentAsync();
        var dto = new CreateCalibrationScheduleDto
        {
            EquipmentId = eqId,
            IntervalDays = 90,
            IntervalAdjustmentMethod = "Fixed",
            MaxIntervalDays = 30,
            MinIntervalDays = 365,
            ExtensionPercent = 25
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/schedules", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetScheduleById_ReturnsSchedule()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationSchedule(eqId);

        var resp = await _client.GetAsync($"/api/calibration/schedules/{created.Id}");
        resp.EnsureSuccessStatusCode();

        var schedule = await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json);
        Assert.NotNull(schedule);
        Assert.Equal(created.Id, schedule.Id);
    }

    [Fact]
    public async Task UpdateSchedule_ChangesFields()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationSchedule(eqId);

        var updateDto = new UpdateCalibrationScheduleDto
        {
            IntervalDays = 180,
            IntervalAdjustmentMethod = "ReliabilityBased",
            MaxIntervalDays = 730,
            MinIntervalDays = 60,
            ExtensionPercent = 30,
            IsActive = true
        };

        var resp = await _client.PutAsJsonAsync($"/api/calibration/schedules/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal(180, updated.IntervalDays);
        Assert.Equal("ReliabilityBased", updated.IntervalAdjustmentMethod);
        Assert.Equal(730, updated.MaxIntervalDays);
    }

    [Fact]
    public async Task DeleteSchedule_ReturnsNoContent()
    {
        var eqId = await CreateEquipmentAsync();
        var created = await CreateCalibrationSchedule(eqId);

        var resp = await _client.DeleteAsync($"/api/calibration/schedules/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/calibration/schedules/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_ListsAll()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationSchedule(eqId);

        var resp = await _client.GetAsync("/api/calibration/schedules");
        resp.EnsureSuccessStatusCode();

        var schedules = await resp.Content.ReadFromJsonAsync<List<CalibrationScheduleResponseDto>>(Json);
        Assert.NotNull(schedules);
        Assert.True(schedules.Count >= 1);
    }

    // ── Reliability-Based Interval Adjustment Tests ──────────────────────────

    [Fact]
    public async Task CreateRecord_PassWithReliabilitySchedule_ExtendsInterval()
    {
        var eqId = await CreateEquipmentAsync();
        var schedule = await CreateCalibrationSchedule(eqId, "ReliabilityBased");

        Assert.Equal(90, schedule.IntervalDays);

        await CreateCalibrationRecord(eqId, "Pass");

        var resp = await _client.GetAsync($"/api/calibration/schedules/{schedule.Id}");
        var updated = await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal(1, updated.ConsecutivePassCount);
        Assert.Equal(112, updated.IntervalDays); // 90 * 1.25 = 112.5 → 112
    }

    [Fact]
    public async Task CreateRecord_FailWithReliabilitySchedule_ResetsToMin()
    {
        var eqId = await CreateEquipmentAsync();
        var schedule = await CreateCalibrationSchedule(eqId, "ReliabilityBased");

        await CreateCalibrationRecord(eqId, "Pass");
        await CreateCalibrationRecord(eqId, "Fail");

        var resp = await _client.GetAsync($"/api/calibration/schedules/{schedule.Id}");
        var updated = await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal(0, updated.ConsecutivePassCount);
        Assert.Equal(30, updated.IntervalDays); // Reset to min
    }

    [Fact]
    public async Task CreateRecord_PassWithFixedSchedule_DoesNotChangeInterval()
    {
        var eqId = await CreateEquipmentAsync();
        var schedule = await CreateCalibrationSchedule(eqId, "Fixed");

        await CreateCalibrationRecord(eqId, "Pass");

        var resp = await _client.GetAsync($"/api/calibration/schedules/{schedule.Id}");
        var updated = await resp.Content.ReadFromJsonAsync<CalibrationScheduleResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal(90, updated.IntervalDays); // Unchanged
        Assert.Equal(1, updated.ConsecutivePassCount);
    }

    // ── Dashboard Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationSchedule(eqId);
        await CreateCalibrationRecord(eqId, "Pass");

        var resp = await _client.GetAsync("/api/calibration/dashboard");
        resp.EnsureSuccessStatusCode();

        var dashboard = await resp.Content.ReadFromJsonAsync<CalibrationDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalSchedules >= 1);
        Assert.True(dashboard.ActiveSchedules >= 1);
        Assert.True(dashboard.TotalRecords >= 1);
        Assert.True(dashboard.PassCount >= 1);
    }

    [Fact]
    public async Task Dashboard_OverdueRecallsDetected()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationSchedule(eqId);

        // Create a record with a past NextDueDate
        var dto = new CreateCalibrationRecordDto
        {
            EquipmentId = eqId,
            CalibrationType = "Internal",
            CalibrationDate = DateTime.UtcNow.AddDays(-100),
            NextDueDate = DateTime.UtcNow.AddDays(-10),
            Result = "Pass",
        };
        var resp = await _client.PostAsJsonAsync("/api/calibration/records", dto, Json);
        resp.EnsureSuccessStatusCode();

        var dashResp = await _client.GetAsync("/api/calibration/dashboard");
        dashResp.EnsureSuccessStatusCode();

        var dashboard = await dashResp.Content.ReadFromJsonAsync<CalibrationDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.OverdueCount >= 1);
        Assert.True(dashboard.OverdueRecalls.Count >= 1);
    }

    // ── Auth Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CalibrationEndpoints_RequireAuth()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/calibration/records");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task CrossTenantIsolation_RecordsNotVisible()
    {
        var eqId = await CreateEquipmentAsync();
        var record = await CreateCalibrationRecord(eqId);

        var otherTenantId = _factory.CreateTenant("cal-other");
        using var otherClient = _factory.CreateTenantClient(otherTenantId);

        var resp = await otherClient.GetAsync($"/api/calibration/records/{record.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task McpGetCalibrationStatus_ReturnsData()
    {
        var eqId = await CreateEquipmentAsync();
        await CreateCalibrationSchedule(eqId);
        await CreateCalibrationRecord(eqId);

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_calibration_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Calibration Status Summary", body);
        Assert.Contains("Active Schedules", body);
    }
}
