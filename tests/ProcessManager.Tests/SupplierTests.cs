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

public class SupplierTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SupplierTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SupplierResponseDto> CreateSupplier(string? code = null, string? name = null)
    {
        var dto = new CreateSupplierDto
        {
            Code = code ?? $"SUP-{Guid.NewGuid():N}"[..12],
            Name = name ?? $"Test Supplier {Guid.NewGuid():N}"[..25],
            ContactName = "John Doe",
            ContactEmail = "john@example.com",
            ContactPhone = "+1-555-0100"
        };
        var resp = await _client.PostAsJsonAsync("/api/suppliers", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json))!;
    }

    // ── CRUD Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_ReturnsCreated()
    {
        var supplier = await CreateSupplier("ACME-001", "ACME Corp");

        Assert.Equal("ACME-001", supplier.Code);
        Assert.Equal("ACME Corp", supplier.Name);
        Assert.Equal("Pending", supplier.Status);
        Assert.True(supplier.IsActive);
        Assert.Equal("John Doe", supplier.ContactName);
    }

    [Fact]
    public async Task CreateSupplier_DuplicateCode_ReturnsConflict()
    {
        var code = $"DUP-{Guid.NewGuid():N}"[..10];
        await CreateSupplier(code);

        var dto = new CreateSupplierDto { Code = code, Name = "Another Supplier" };
        var resp = await _client.PostAsJsonAsync("/api/suppliers", dto, Json);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task GetSupplierById_ReturnsSupplier()
    {
        var created = await CreateSupplier();

        var resp = await _client.GetAsync($"/api/suppliers/{created.Id}");
        resp.EnsureSuccessStatusCode();

        var supplier = await resp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json);
        Assert.NotNull(supplier);
        Assert.Equal(created.Code, supplier.Code);
    }

    [Fact]
    public async Task GetSupplierById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_ChangesFields()
    {
        var created = await CreateSupplier();

        var updateDto = new UpdateSupplierDto
        {
            Name = "Updated Name",
            ContactName = "Jane Smith",
            ContactEmail = "jane@example.com",
            IsActive = true
        };

        var resp = await _client.PutAsJsonAsync($"/api/suppliers/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Jane Smith", updated.ContactName);
    }

    [Fact]
    public async Task DeleteSupplier_SoftDeletes()
    {
        var created = await CreateSupplier();

        var resp = await _client.DeleteAsync($"/api/suppliers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await _client.GetAsync($"/api/suppliers/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var supplier = await getResp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json);
        Assert.NotNull(supplier);
        Assert.False(supplier.IsActive);
        Assert.Equal("Inactive", supplier.Status);
    }

    // ── List and Filter Tests ────────────────────────────────────────────────

    [Fact]
    public async Task ListSuppliers_Paginated()
    {
        await CreateSupplier();
        await CreateSupplier();

        var resp = await _client.GetAsync("/api/suppliers?page=1&pageSize=10");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<SupplierSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result.Items.Count >= 2);
    }

    [Fact]
    public async Task ListSuppliers_FilterByStatus()
    {
        var supplier = await CreateSupplier();
        await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Approved" }, Json);

        var resp = await _client.GetAsync("/api/suppliers?status=Approved");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<SupplierSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.Contains(result.Items, s => s.Id == supplier.Id);
    }

    [Fact]
    public async Task ListSuppliers_SearchByName()
    {
        var supplier = await CreateSupplier(name: "UniqueSearchName123");

        var resp = await _client.GetAsync("/api/suppliers?search=UniqueSearchName123");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<SupplierSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.Contains(result.Items, s => s.Id == supplier.Id);
    }

    // ── Status Transition Tests ──────────────────────────────────────────────

    [Fact]
    public async Task StatusTransition_PendingToApproved_Succeeds()
    {
        var supplier = await CreateSupplier();

        var resp = await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Approved" }, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.NotNull(updated.ApprovedDate);
    }

    [Fact]
    public async Task StatusTransition_PendingToSuspended_Fails()
    {
        var supplier = await CreateSupplier();

        var resp = await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Suspended" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task StatusTransition_ApprovedToSuspended_Succeeds()
    {
        var supplier = await CreateSupplier();
        await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Approved" }, Json);

        var resp = await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Suspended" }, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<SupplierResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal("Suspended", updated.Status);
    }

    [Fact]
    public async Task StatusTransition_InvalidStatus_ReturnsBadRequest()
    {
        var supplier = await CreateSupplier();

        var resp = await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "InvalidStatus" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Evaluation Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddEvaluation_ReturnsCreated()
    {
        var supplier = await CreateSupplier();

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 85,
            DeliveryScore = 90,
            ResponsivenessScore = 80,
            Notes = "Good performance"
        };

        var resp = await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);
        resp.EnsureSuccessStatusCode();

        var eval = await resp.Content.ReadFromJsonAsync<SupplierEvaluationResponseDto>(Json);
        Assert.NotNull(eval);
        Assert.Equal(85, eval.QualityScore);
        Assert.Equal(90, eval.DeliveryScore);
        Assert.Equal(80, eval.ResponsivenessScore);
        Assert.Equal(85, eval.OverallScore); // (85+90+80)/3 = 85
    }

    [Fact]
    public async Task AddEvaluation_InvalidScores_ReturnsBadRequest()
    {
        var supplier = await CreateSupplier();

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 150, // invalid
            DeliveryScore = 90,
            ResponsivenessScore = 80
        };

        var resp = await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetEvaluations_ReturnsAll()
    {
        var supplier = await CreateSupplier();

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 70,
            DeliveryScore = 75,
            ResponsivenessScore = 80
        };
        await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);
        await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);

        var resp = await _client.GetAsync($"/api/suppliers/{supplier.Id}/evaluations");
        resp.EnsureSuccessStatusCode();

        var evals = await resp.Content.ReadFromJsonAsync<List<SupplierEvaluationResponseDto>>(Json);
        Assert.NotNull(evals);
        Assert.Equal(2, evals.Count);
    }

    [Fact]
    public async Task DeleteEvaluation_Succeeds()
    {
        var supplier = await CreateSupplier();

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 70,
            DeliveryScore = 75,
            ResponsivenessScore = 80
        };
        var createResp = await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);
        var created = await createResp.Content.ReadFromJsonAsync<SupplierEvaluationResponseDto>(Json);

        var resp = await _client.DeleteAsync($"/api/suppliers/{supplier.Id}/evaluations/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var listResp = await _client.GetAsync($"/api/suppliers/{supplier.Id}/evaluations");
        var evals = await listResp.Content.ReadFromJsonAsync<List<SupplierEvaluationResponseDto>>(Json);
        Assert.NotNull(evals);
        Assert.Empty(evals);
    }

    [Fact]
    public async Task GetEvaluations_NonExistentSupplier_Returns404()
    {
        var resp = await _client.GetAsync($"/api/suppliers/{Guid.NewGuid()}/evaluations");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Dashboard Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_ReturnsAggregateData()
    {
        var supplier = await CreateSupplier();
        await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Approved" }, Json);

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 90,
            DeliveryScore = 85,
            ResponsivenessScore = 88
        };
        await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);

        var resp = await _client.GetAsync("/api/suppliers/dashboard");
        resp.EnsureSuccessStatusCode();

        var dashboard = await resp.Content.ReadFromJsonAsync<SupplierQualityDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalSuppliers >= 1);
        Assert.True(dashboard.ApprovedSuppliers >= 1);
    }

    // ── Auth Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Suppliers_RequiresAuthentication()
    {
        var unauthClient = _factory.CreateClient();
        var resp = await unauthClient.GetAsync("/api/suppliers");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task Suppliers_CrossTenantIsolation()
    {
        var supplier = await CreateSupplier();

        var otherTenantId = _factory.CreateTenant("supplier-isolation-test");
        var otherClient = _factory.CreateTenantClient(otherTenantId);

        var resp = await otherClient.GetAsync($"/api/suppliers/{supplier.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        otherClient.Dispose();
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task McpGetSupplierQualityStatus_ReturnsData()
    {
        var supplier = await CreateSupplier();
        await _client.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status",
            new UpdateSupplierStatusDto { Status = "Approved" }, Json);

        var evalDto = new CreateSupplierEvaluationDto
        {
            EvaluationDate = DateTime.UtcNow,
            QualityScore = 90,
            DeliveryScore = 85,
            ResponsivenessScore = 88
        };
        await _client.PostAsJsonAsync($"/api/suppliers/{supplier.Id}/evaluations", evalDto, Json);

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_supplier_quality_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Supplier Quality", body);
        Assert.Contains("Approved", body);
    }
}
