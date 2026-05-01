using System.Net;
using System.Net.Http.Json;
using System.Text;
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

public class SpcTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SpcTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    private async Task<Guid> CreateTestProcess()
    {
        var dto = new { Code = $"SPC-PROC-{Guid.NewGuid():N}"[..20], Name = "SPC Test Process", Pattern = "Transform" };
        var resp = await _client.PostAsJsonAsync("/api/processes", dto, Json);
        resp.EnsureSuccessStatusCode();
        var process = await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
        return process.GetProperty("id").GetGuid();
    }

    private async Task<(Guid processId, Guid chartId)> CreateTestChart(
        decimal? lsl = null, decimal? usl = null, int subgroupSize = 5)
    {
        var processId = await CreateTestProcess();
        var createDto = new CreateSpcChartDto(
            processId, Guid.NewGuid(), $"Test Chart {Guid.NewGuid():N}"[..30],
            "XbarR", subgroupSize, "Calculated",
            null, null, null, null, null, null, null, lsl, usl);

        var resp = await _client.PostAsJsonAsync("/api/spc", createDto, Json);
        resp.EnsureSuccessStatusCode();
        var chart = await resp.Content.ReadFromJsonAsync<SpcChartDto>(Json);
        return (processId, chart!.Id);
    }

    private async Task<Guid> CreateTestStepExecution(Guid processId)
    {
        var jobDto = new { Code = $"SPC-JOB-{Guid.NewGuid():N}"[..20], Name = "SPC Test Job", ProcessId = processId };
        var jobResp = await _client.PostAsJsonAsync("/api/jobs", jobDto, Json);
        if (!jobResp.IsSuccessStatusCode) return Guid.Empty;

        var job = await jobResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var steps = job.GetProperty("stepExecutions");
        if (steps.GetArrayLength() > 0)
            return steps[0].GetProperty("id").GetGuid();

        return Guid.Empty;
    }

    // ── Chart CRUD ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSpcChart_ReturnsCreatedChart()
    {
        var (_, chartId) = await CreateTestChart();
        Assert.NotEqual(Guid.Empty, chartId);

        var resp = await _client.GetAsync($"/api/spc/{chartId}");
        resp.EnsureSuccessStatusCode();
        var chart = await resp.Content.ReadFromJsonAsync<SpcChartDto>(Json);
        Assert.NotNull(chart);
        Assert.Equal("XbarR", chart.ChartType);
        Assert.Equal(5, chart.SubgroupSize);
        Assert.True(chart.IsActive);
    }

    [Fact]
    public async Task UpdateSpcChart_ModifiesChart()
    {
        var (_, chartId) = await CreateTestChart();
        var updateDto = new UpdateSpcChartDto("Updated Chart Name", "XbarS", 3, "Manual",
            100m, 80m, 90m, null, null, null, 1.33m, 75m, 105m, true);

        var resp = await _client.PutAsJsonAsync($"/api/spc/{chartId}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var chart = await resp.Content.ReadFromJsonAsync<SpcChartDto>(Json);
        Assert.Equal("Updated Chart Name", chart!.Name);
        Assert.Equal("XbarS", chart.ChartType);
        Assert.Equal(3, chart.SubgroupSize);
        Assert.Equal("Manual", chart.ControlLimitSource);
    }

    [Fact]
    public async Task DeleteSpcChart_ReturnsNoContent()
    {
        var (_, chartId) = await CreateTestChart();

        var resp = await _client.DeleteAsync($"/api/spc/{chartId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await _client.GetAsync($"/api/spc/{chartId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task GetSpcChart_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/spc/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── List & Dashboard ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSpcCharts_ReturnsPaginatedList()
    {
        await CreateTestChart();

        var resp = await _client.GetAsync("/api/spc?page=1&pageSize=10");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<SpcChartSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task GetSpcDashboard_ReturnsActiveCharts()
    {
        await CreateTestChart();

        var resp = await _client.GetAsync("/api/spc/dashboard");
        resp.EnsureSuccessStatusCode();
        var charts = await resp.Content.ReadFromJsonAsync<List<SpcChartSummaryDto>>(Json);
        Assert.NotNull(charts);
        Assert.True(charts.Count >= 1);
    }

    // ── Data Points ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDataPoints_EmptyChart_ReturnsEmptyList()
    {
        var (_, chartId) = await CreateTestChart();

        var resp = await _client.GetAsync($"/api/spc/{chartId}/data-points");
        resp.EnsureSuccessStatusCode();
        var points = await resp.Content.ReadFromJsonAsync<List<SpcDataPointDto>>(Json);
        Assert.NotNull(points);
        Assert.Empty(points);
    }

    // ── Calculate ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_InsufficientData_ReturnsBadRequest()
    {
        var (_, chartId) = await CreateTestChart();

        var resp = await _client.GetAsync($"/api/spc/{chartId}/calculate");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Calculation Service Unit Tests ──────────────────────────────────────

    [Fact]
    public void SpcCalculationService_Calculate_BasicValues()
    {
        var svc = new SpcCalculationService();
        var values = new List<decimal>
        {
            10.1m, 10.2m, 10.0m, 10.3m, 10.1m,
            10.2m, 10.1m, 10.3m, 10.0m, 10.2m,
            10.1m, 10.2m, 10.1m, 10.0m, 10.3m
        };

        var result = svc.Calculate(values, 5, 9.5m, 10.5m);

        Assert.Equal(3, result.SubgroupCount);
        Assert.Equal(15, result.TotalPoints);
        Assert.True(result.XBar > 0);
        Assert.True(result.RBar >= 0);
        Assert.True(result.UCL > result.CL);
        Assert.True(result.CL > result.LCL);
        Assert.NotNull(result.Cp);
        Assert.NotNull(result.Cpk);
        Assert.True(result.Cp > 0);
        Assert.True(result.Cpk > 0);
    }

    [Fact]
    public void SpcCalculationService_Calculate_NoSpecLimits_NullCapability()
    {
        var svc = new SpcCalculationService();
        var values = Enumerable.Range(1, 20).Select(i => (decimal)i).ToList();

        var result = svc.Calculate(values, 5, null, null);

        Assert.Null(result.Cp);
        Assert.Null(result.Cpk);
        Assert.True(result.SubgroupCount > 0);
    }

    [Fact]
    public void SpcCalculationService_Calculate_EmptyValues_ReturnsZeros()
    {
        var svc = new SpcCalculationService();
        var result = svc.Calculate(new List<decimal>(), 5, null, null);

        Assert.Equal(0, result.XBar);
        Assert.Equal(0, result.SubgroupCount);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public void SpcCalculationService_DetectOutOfControl_Rule1()
    {
        var svc = new SpcCalculationService();
        // 4 stable subgroups around 10.0 with small variation, then 1 extreme outlier subgroup
        var values = new List<decimal>
        {
            10.0m, 10.1m, 10.0m, 9.9m, 10.0m,
            10.0m, 10.1m, 9.9m, 10.0m, 10.1m,
            9.9m, 10.0m, 10.1m, 10.0m, 9.9m,
            10.0m, 10.0m, 10.1m, 9.9m, 10.0m,
            15.0m, 15.1m, 15.0m, 14.9m, 15.0m,
        };

        var result = svc.Calculate(values, 5, null, null);
        Assert.True(result.OutOfControlPoints.Count > 0, "Expected at least one OOC point for the extreme subgroup");
    }

    [Fact]
    public void SpcCalculationService_BuildSubgroups_CorrectGrouping()
    {
        var values = new List<decimal> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        var subgroups = SpcCalculationService.BuildSubgroups(values, 5);

        Assert.Equal(2, subgroups.Count);
        Assert.Equal(5, subgroups[0].Values.Count);
        Assert.Equal(5, subgroups[1].Values.Count);
        Assert.Equal(3m, subgroups[0].Mean);
        Assert.Equal(4m, subgroups[0].Range);
    }

    // ── Validation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSpcChart_InvalidChartType_ReturnsBadRequest()
    {
        var processId = await CreateTestProcess();
        var dto = new CreateSpcChartDto(processId, Guid.NewGuid(), "Bad Type Chart",
            "InvalidType", 5, "Calculated");

        var resp = await _client.PostAsJsonAsync("/api/spc", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSpcChart_InvalidProcessId_ReturnsNotFound()
    {
        var dto = new CreateSpcChartDto(Guid.NewGuid(), Guid.NewGuid(), "No Process",
            "XbarR", 5, "Calculated");

        var resp = await _client.PostAsJsonAsync("/api/spc", dto, Json);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSpcChart_InvalidSubgroupSize_ReturnsBadRequest()
    {
        var processId = await CreateTestProcess();
        var dto = new CreateSpcChartDto(processId, Guid.NewGuid(), "Bad Subgroup",
            "XbarR", 15, "Calculated");

        var resp = await _client.PostAsJsonAsync("/api/spc", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Filter ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSpcCharts_FilterByProcess()
    {
        var (processId, _) = await CreateTestChart();

        var resp = await _client.GetAsync($"/api/spc?processId={processId}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<SpcChartSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.All(result.Items, c => Assert.Equal(processId, c.ProcessId));
    }

    // ── Auth Required ───────────────────────────────────────────────────────

    [Fact]
    public async Task SpcEndpoints_RequireAuth()
    {
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/spc");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-Tenant Isolation ──────────────────────────────────────────────

    [Fact]
    public async Task SpcChart_CrossTenant_Returns404()
    {
        var (_, chartId) = await CreateTestChart();

        using var otherClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await otherClient.GetAsync($"/api/spc/{chartId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tools ───────────────────────────────────────────────────────────

    [Fact]
    public async Task McpToolGetSpcStatus_ReturnsData()
    {
        await CreateTestChart();

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_spc_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", request, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("SPC Status", body);
    }

    [Fact]
    public async Task McpToolGetProcessCapability_MissingChartId_ReturnsError()
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_process_capability",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", request, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("chart_id is required", body);
    }

    [Fact]
    public async Task McpToolGetProcessCapability_NonexistentChart_ReturnsError()
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_process_capability",
                arguments = new { chart_id = Guid.NewGuid().ToString() }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", request, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("not found", body);
    }

    // ── Calculation Service: Pp/Ppk ─────────────────────────────────────────

    [Fact]
    public void SpcCalculationService_Calculate_PpPpk_WithSpecLimits()
    {
        var svc = new SpcCalculationService();
        var rng = new Random(42);
        var values = Enumerable.Range(0, 50).Select(_ => 10m + (decimal)(rng.NextDouble() * 0.6 - 0.3)).ToList();

        var result = svc.Calculate(values, 5, 9.0m, 11.0m);

        Assert.NotNull(result.Pp);
        Assert.NotNull(result.Ppk);
        Assert.True(result.Pp > 0);
        Assert.True(result.Ppk > 0);
    }

    // ── Calculation Service: one-sided spec limits ──────────────────────────

    [Fact]
    public void SpcCalculationService_OneSidedLSL_OnlyCpkCalculated()
    {
        var svc = new SpcCalculationService();
        var values = Enumerable.Range(1, 25).Select(i => 50m + i * 0.1m).ToList();

        var result = svc.Calculate(values, 5, 45m, null);

        Assert.Null(result.Cp);
        Assert.NotNull(result.Cpk);
    }

    [Fact]
    public void SpcCalculationService_OneSidedUSL_OnlyCpkCalculated()
    {
        var svc = new SpcCalculationService();
        var values = Enumerable.Range(1, 25).Select(i => 50m + i * 0.1m).ToList();

        var result = svc.Calculate(values, 5, null, 60m);

        Assert.Null(result.Cp);
        Assert.NotNull(result.Cpk);
    }
}
