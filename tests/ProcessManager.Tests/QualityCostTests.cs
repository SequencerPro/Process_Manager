using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class QualityCostTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public QualityCostTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<QualityCostResponseDto> CreateCost(
        string? sourceType = null,
        string? costCategory = null,
        decimal amount = 150.00m,
        string? kindName = null,
        string? description = null)
    {
        var dto = new CreateQualityCostDto
        {
            SourceType = sourceType ?? "Scrap",
            Amount = amount,
            Currency = "USD",
            CostCategory = costCategory ?? "InternalFailure",
            KindName = kindName ?? "Widget-A",
            Description = description ?? $"Test quality cost {Guid.NewGuid():N}",
            RecordedByUserId = "test-user-id",
            RecordedByDisplayName = "Test User"
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<QualityCostResponseDto>(Json))!;
    }

    private async Task<QualityCostRuleResponseDto> CreateRule(
        string? triggerEvent = null,
        decimal amount = 100.00m,
        bool isActive = true)
    {
        var dto = new CreateQualityCostRuleDto
        {
            TriggerEvent = triggerEvent ?? "NcScrapped",
            DefaultCategory = "InternalFailure",
            DefaultSourceType = "Scrap",
            DefaultAmount = amount,
            Description = "Test auto-cost rule",
            IsActive = isActive
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs/rules", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<QualityCostRuleResponseDto>(Json))!;
    }

    // ── CRUD Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCost_ReturnsCreated()
    {
        var cost = await CreateCost();

        Assert.Equal("Scrap", cost.SourceType);
        Assert.Equal("InternalFailure", cost.CostCategory);
        Assert.Equal(150.00m, cost.Amount);
        Assert.Equal("USD", cost.Currency);
        Assert.Equal("Widget-A", cost.KindName);
        Assert.Equal("Test User", cost.RecordedByDisplayName);
    }

    [Fact]
    public async Task CreateCost_InvalidSourceType_ReturnsBadRequest()
    {
        var dto = new CreateQualityCostDto
        {
            SourceType = "InvalidType",
            Amount = 100,
            CostCategory = "InternalFailure",
            RecordedByUserId = "test",
            RecordedByDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateCost_InvalidCategory_ReturnsBadRequest()
    {
        var dto = new CreateQualityCostDto
        {
            SourceType = "Manual",
            Amount = 100,
            CostCategory = "InvalidCategory",
            RecordedByUserId = "test",
            RecordedByDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetCostById_ReturnsDetails()
    {
        var created = await CreateCost();
        var resp = await _client.GetAsync($"/api/quality-costs/{created.Id}");
        resp.EnsureSuccessStatusCode();
        var cost = await resp.Content.ReadFromJsonAsync<QualityCostResponseDto>(Json);
        Assert.NotNull(cost);
        Assert.Equal(created.Id, cost!.Id);
        Assert.Equal(created.Amount, cost.Amount);
    }

    [Fact]
    public async Task GetCostById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/quality-costs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateCost_UpdatesFields()
    {
        var created = await CreateCost();
        var updateDto = new UpdateQualityCostDto
        {
            Amount = 500.00m,
            CostCategory = "ExternalFailure",
            Description = "Updated cost description"
        };
        var resp = await _client.PutAsJsonAsync($"/api/quality-costs/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<QualityCostResponseDto>(Json);
        Assert.Equal(500.00m, updated!.Amount);
        Assert.Equal("ExternalFailure", updated.CostCategory);
        Assert.Equal("Updated cost description", updated.Description);
    }

    [Fact]
    public async Task UpdateCost_InvalidCategory_ReturnsBadRequest()
    {
        var created = await CreateCost();
        var updateDto = new UpdateQualityCostDto { CostCategory = "InvalidCategory" };
        var resp = await _client.PutAsJsonAsync($"/api/quality-costs/{created.Id}", updateDto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteCost_ReturnsNoContent()
    {
        var created = await CreateCost();
        var resp = await _client.DeleteAsync($"/api/quality-costs/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/quality-costs/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    // ── List & Filter Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task ListCosts_Paginated()
    {
        await CreateCost();
        await CreateCost();

        var resp = await _client.GetAsync("/api/quality-costs?page=1&pageSize=10");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<QualityCostSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2);
    }

    [Fact]
    public async Task ListCosts_FilterByCategory()
    {
        await CreateCost(costCategory: "Prevention");
        await CreateCost(costCategory: "InternalFailure");

        var resp = await _client.GetAsync("/api/quality-costs?category=Prevention");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<QualityCostSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.All(result!.Items, c => Assert.Equal("Prevention", c.CostCategory));
    }

    [Fact]
    public async Task ListCosts_FilterBySourceType()
    {
        await CreateCost(sourceType: "Rework");
        await CreateCost(sourceType: "Scrap");

        var resp = await _client.GetAsync("/api/quality-costs?sourceType=Rework");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<QualityCostSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.All(result!.Items, c => Assert.Equal("Rework", c.SourceType));
    }

    [Fact]
    public async Task ListCosts_FilterByDateRange()
    {
        await CreateCost();

        var from = DateTime.UtcNow.AddDays(-1).ToString("O");
        var to = DateTime.UtcNow.AddDays(1).ToString("O");
        var resp = await _client.GetAsync($"/api/quality-costs?dateFrom={from}&dateTo={to}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<QualityCostSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
    }

    // ── Dashboard Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        await CreateCost(costCategory: "InternalFailure", amount: 200);
        await CreateCost(costCategory: "Prevention", amount: 50);
        await CreateCost(costCategory: "Appraisal", amount: 75);

        var resp = await _client.GetAsync("/api/quality-costs/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<CoqDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TotalCostThisMonth > 0);
        Assert.True(dashboard.TotalCostThisYear > 0);
        Assert.True(dashboard.ByCategory.ContainsKey("InternalFailure"));
        Assert.True(dashboard.TotalEntries >= 3);
    }

    [Fact]
    public async Task Dashboard_TopDriversByKind()
    {
        await CreateCost(kindName: "Part-A", amount: 300);
        await CreateCost(kindName: "Part-B", amount: 100);

        var resp = await _client.GetAsync("/api/quality-costs/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<CoqDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TopDriversByKind.Count >= 1);
    }

    [Fact]
    public async Task Dashboard_MonthlyTrend_IncludesCurrentMonth()
    {
        await CreateCost(amount: 250);

        var resp = await _client.GetAsync("/api/quality-costs/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<CoqDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.MonthlyTrend.Count >= 1);
        Assert.True(dashboard.MonthlyTrend.Any(t => t.Total > 0));
    }

    // ── Rules Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRule_ReturnsCreated()
    {
        var rule = await CreateRule("NcCreated", 75.00m);

        Assert.Equal("NcCreated", rule.TriggerEvent);
        Assert.Equal("InternalFailure", rule.DefaultCategory);
        Assert.Equal("Scrap", rule.DefaultSourceType);
        Assert.Equal(75.00m, rule.DefaultAmount);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task CreateRule_InvalidTrigger_ReturnsBadRequest()
    {
        var dto = new CreateQualityCostRuleDto
        {
            TriggerEvent = "InvalidTrigger",
            DefaultCategory = "InternalFailure",
            DefaultSourceType = "Scrap",
            DefaultAmount = 100
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs/rules", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateRule_DuplicateActiveTrigger_ReturnsConflict()
    {
        await CreateRule("CapaOpened");

        var dto = new CreateQualityCostRuleDto
        {
            TriggerEvent = "CapaOpened",
            DefaultCategory = "Prevention",
            DefaultSourceType = "PreventionCost",
            DefaultAmount = 200
        };
        var resp = await _client.PostAsJsonAsync("/api/quality-costs/rules", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task GetRuleById_ReturnsDetails()
    {
        var created = await CreateRule("ComplaintReceived", 50.00m);
        var resp = await _client.GetAsync($"/api/quality-costs/rules/{created.Id}");
        resp.EnsureSuccessStatusCode();
        var rule = await resp.Content.ReadFromJsonAsync<QualityCostRuleResponseDto>(Json);
        Assert.NotNull(rule);
        Assert.Equal(created.Id, rule!.Id);
    }

    [Fact]
    public async Task GetRuleById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/quality-costs/rules/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateRule_UpdatesFields()
    {
        var created = await CreateRule("InspectionPerformed", 80.00m);
        var updateDto = new UpdateQualityCostRuleDto
        {
            DefaultAmount = 120.00m,
            Description = "Updated description",
            IsActive = false
        };
        var resp = await _client.PutAsJsonAsync($"/api/quality-costs/rules/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<QualityCostRuleResponseDto>(Json);
        Assert.Equal(120.00m, updated!.DefaultAmount);
        Assert.Equal("Updated description", updated.Description);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteRule_ReturnsNoContent()
    {
        var created = await CreateRule("NcReworked", 60.00m);
        var resp = await _client.DeleteAsync($"/api/quality-costs/rules/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/quality-costs/rules/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task ListRules_ReturnsAll()
    {
        await CreateRule("NcScrapped", 100);

        var resp = await _client.GetAsync("/api/quality-costs/rules");
        resp.EnsureSuccessStatusCode();
        var rules = await resp.Content.ReadFromJsonAsync<List<QualityCostRuleResponseDto>>(Json);
        Assert.NotNull(rules);
        Assert.True(rules!.Count >= 1);
    }

    [Fact]
    public async Task ListRules_ActiveOnly()
    {
        var active = await CreateRule("NcScrapped", 100, isActive: true);

        var resp = await _client.GetAsync("/api/quality-costs/rules?activeOnly=true");
        resp.EnsureSuccessStatusCode();
        var rules = await resp.Content.ReadFromJsonAsync<List<QualityCostRuleResponseDto>>(Json);
        Assert.NotNull(rules);
        Assert.All(rules!, r => Assert.True(r.IsActive));
    }

    // ── Auth Required ────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthRequired_ReturnsUnauthorized()
    {
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/quality-costs");
        Assert.True(resp.StatusCode == HttpStatusCode.Unauthorized
                 || resp.StatusCode == HttpStatusCode.Redirect);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task CrossTenant_ReturnsNotFound()
    {
        var cost = await CreateCost();

        using var tenantBClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await tenantBClient.GetAsync($"/api/quality-costs/{cost.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task Mcp_GetCostOfQuality_ReturnsData()
    {
        await CreateCost(costCategory: "InternalFailure", amount: 500);

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_cost_of_quality",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Cost of Quality", body);
    }
}
