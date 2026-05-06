using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class CustomerComplaintTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CustomerComplaintTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<CustomerComplaintResponseDto> CreateComplaint(
        string? category = null,
        string? severity = null,
        string? customerName = null,
        DateTime? responseDueDate = null)
    {
        var dto = new CreateCustomerComplaintDto
        {
            CustomerName = customerName ?? $"Customer-{Guid.NewGuid():N}"[..20],
            Category = category ?? "ProductDefect",
            Severity = severity ?? "Minor",
            Description = $"Test complaint description {Guid.NewGuid():N}",
            QuantityAffected = 5,
            OwnerUserId = "test-user-id",
            OwnerDisplayName = "Test User",
            ResponseDueDate = responseDueDate
        };
        var resp = await _client.PostAsJsonAsync("/api/complaints", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CustomerComplaintResponseDto>(Json))!;
    }

    private async Task<ComplaintInvestigationResponseDto> AddInvestigation(Guid complaintId, string? type = null)
    {
        var dto = new CreateComplaintInvestigationDto
        {
            InvestigationType = type ?? "InitialAssessment",
            Findings = "Test investigation findings",
            InvestigatedByUserId = "investigator-user-id",
            InvestigatedByDisplayName = "Investigator"
        };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaintId}/investigations", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ComplaintInvestigationResponseDto>(Json))!;
    }

    private async Task<ComplaintResponseResponseDto> AddResponse(Guid complaintId, string? type = null)
    {
        var dto = new CreateComplaintResponseDto
        {
            ResponseType = type ?? "Acknowledgment",
            Content = "We acknowledge your complaint and are investigating.",
            SentByUserId = "sender-user-id",
            SentByDisplayName = "Sender"
        };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaintId}/responses", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ComplaintResponseResponseDto>(Json))!;
    }

    // ── CRUD Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateComplaint_ReturnsCreated_WithAutoCode()
    {
        var complaint = await CreateComplaint();

        Assert.StartsWith("CC-", complaint.Code);
        Assert.Equal("ProductDefect", complaint.Category);
        Assert.Equal("Minor", complaint.Severity);
        Assert.Equal("New", complaint.Status);
        Assert.Equal("Test User", complaint.OwnerDisplayName);
    }

    [Fact]
    public async Task CreateComplaint_InvalidCategory_ReturnsBadRequest()
    {
        var dto = new CreateCustomerComplaintDto
        {
            CustomerName = "Test",
            Category = "InvalidCategory",
            Severity = "Minor",
            Description = "Test",
            OwnerUserId = "test",
            OwnerDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync("/api/complaints", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateComplaint_InvalidSeverity_ReturnsBadRequest()
    {
        var dto = new CreateCustomerComplaintDto
        {
            CustomerName = "Test",
            Category = "ProductDefect",
            Severity = "InvalidSeverity",
            Description = "Test",
            OwnerUserId = "test",
            OwnerDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync("/api/complaints", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetComplaintById_ReturnsDetails()
    {
        var created = await CreateComplaint();
        var resp = await _client.GetAsync($"/api/complaints/{created.Id}");
        resp.EnsureSuccessStatusCode();
        var complaint = await resp.Content.ReadFromJsonAsync<CustomerComplaintResponseDto>(Json);
        Assert.NotNull(complaint);
        Assert.Equal(created.Code, complaint!.Code);
    }

    [Fact]
    public async Task GetComplaintById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/complaints/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateComplaint_UpdatesFields()
    {
        var created = await CreateComplaint();
        var updateDto = new UpdateCustomerComplaintDto
        {
            CustomerName = "Updated Customer",
            Severity = "Critical",
            QuantityAffected = 100
        };
        var resp = await _client.PatchAsJsonAsync($"/api/complaints/{created.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<CustomerComplaintResponseDto>(Json);
        Assert.Equal("Updated Customer", updated!.CustomerName);
        Assert.Equal("Critical", updated.Severity);
        Assert.Equal(100, updated.QuantityAffected);
    }

    [Fact]
    public async Task DeleteComplaint_ReturnsNoContent()
    {
        var created = await CreateComplaint();
        var resp = await _client.DeleteAsync($"/api/complaints/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/complaints/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    // ── List & Filter Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task ListComplaints_Paginated()
    {
        await CreateComplaint();
        await CreateComplaint();

        var resp = await _client.GetAsync("/api/complaints?page=1&pageSize=10");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 2);
    }

    [Fact]
    public async Task ListComplaints_FilterByStatus()
    {
        await CreateComplaint();

        var resp = await _client.GetAsync("/api/complaints?status=New");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task ListComplaints_FilterByCategory()
    {
        await CreateComplaint(category: "Packaging");

        var resp = await _client.GetAsync("/api/complaints?category=Packaging");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task ListComplaints_FilterBySeverity()
    {
        await CreateComplaint(severity: "Critical");

        var resp = await _client.GetAsync("/api/complaints?severity=Critical");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }

    // ── Status Transition Tests ────────────────────────────���────────────────

    [Fact]
    public async Task Transition_NewToUnderInvestigation_RequiresInvestigation()
    {
        var complaint = await CreateComplaint();
        var dto = new TransitionComplaintStatusDto { TargetStatus = "UnderInvestigation" };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Transition_NewToUnderInvestigation_WithInvestigation_Succeeds()
    {
        var complaint = await CreateComplaint();
        await AddInvestigation(complaint.Id);

        var dto = new TransitionComplaintStatusDto { TargetStatus = "UnderInvestigation" };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition", dto, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<CustomerComplaintResponseDto>(Json);
        Assert.Equal("UnderInvestigation", result!.Status);
    }

    [Fact]
    public async Task Transition_ResponseSent_RequiresResponse()
    {
        var complaint = await CreateComplaint();
        await AddInvestigation(complaint.Id);

        // Advance to CorrectiveActionImplemented
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "UnderInvestigation" }, Json);

        // Link a CAPA/NC for RootCauseIdentified gate
        await _client.PatchAsJsonAsync($"/api/complaints/{complaint.Id}",
            new UpdateCustomerComplaintDto { LinkedCapaId = Guid.NewGuid() }, Json);

        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "RootCauseIdentified" }, Json);

        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "CorrectiveActionImplemented" }, Json);

        // Try to move to ResponseSent without a response
        var dto = new TransitionComplaintStatusDto { TargetStatus = "ResponseSent" };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Transition_FullLifecycle_Succeeds()
    {
        var complaint = await CreateComplaint();

        // Add investigation -> UnderInvestigation
        await AddInvestigation(complaint.Id);
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "UnderInvestigation" }, Json);

        // ContainmentInPlace
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "ContainmentInPlace" }, Json);

        // Link CAPA for RootCauseIdentified
        await _client.PatchAsJsonAsync($"/api/complaints/{complaint.Id}",
            new UpdateCustomerComplaintDto { LinkedCapaId = Guid.NewGuid() }, Json);

        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "RootCauseIdentified" }, Json);

        // CorrectiveActionImplemented
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "CorrectiveActionImplemented" }, Json);

        // Add response -> ResponseSent
        await AddResponse(complaint.Id);
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "ResponseSent" }, Json);

        // Closed
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "Closed" }, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<CustomerComplaintResponseDto>(Json);
        Assert.Equal("Closed", result!.Status);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task Transition_InvalidTransition_ReturnsBadRequest()
    {
        var complaint = await CreateComplaint();
        // Can't skip straight to Closed from New without being in valid flow
        // Actually Closed from any non-closed is valid per code, so test invalid enum
        var dto = new TransitionComplaintStatusDto { TargetStatus = "InvalidStatus" };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Transition_RootCauseIdentified_RequiresLink()
    {
        var complaint = await CreateComplaint();
        await AddInvestigation(complaint.Id);
        await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition",
            new TransitionComplaintStatusDto { TargetStatus = "UnderInvestigation" }, Json);

        // Try to transition without linked CAPA or NC
        var dto = new TransitionComplaintStatusDto { TargetStatus = "RootCauseIdentified" };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/transition", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Investigation Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task AddInvestigation_Succeeds()
    {
        var complaint = await CreateComplaint();
        var investigation = await AddInvestigation(complaint.Id);
        Assert.Equal("InitialAssessment", investigation.InvestigationType);
        Assert.Equal("Test investigation findings", investigation.Findings);
    }

    [Fact]
    public async Task AddInvestigation_InvalidType_ReturnsBadRequest()
    {
        var complaint = await CreateComplaint();
        var dto = new CreateComplaintInvestigationDto
        {
            InvestigationType = "InvalidType",
            Findings = "Test",
            InvestigatedByUserId = "test",
            InvestigatedByDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/investigations", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetInvestigations_ReturnsList()
    {
        var complaint = await CreateComplaint();
        await AddInvestigation(complaint.Id, "InitialAssessment");
        await AddInvestigation(complaint.Id, "LabAnalysis");

        var resp = await _client.GetAsync($"/api/complaints/{complaint.Id}/investigations");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<ComplaintInvestigationResponseDto>>(Json);
        Assert.True(list!.Count >= 2);
    }

    // ── Response Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddResponse_Succeeds()
    {
        var complaint = await CreateComplaint();
        var response = await AddResponse(complaint.Id);
        Assert.Equal("Acknowledgment", response.ResponseType);
    }

    [Fact]
    public async Task AddResponse_InvalidType_ReturnsBadRequest()
    {
        var complaint = await CreateComplaint();
        var dto = new CreateComplaintResponseDto
        {
            ResponseType = "InvalidType",
            Content = "Test",
            SentByUserId = "test",
            SentByDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync($"/api/complaints/{complaint.Id}/responses", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetResponses_ReturnsList()
    {
        var complaint = await CreateComplaint();
        await AddResponse(complaint.Id, "Acknowledgment");
        await AddResponse(complaint.Id, "InterimUpdate");

        var resp = await _client.GetAsync($"/api/complaints/{complaint.Id}/responses");
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<ComplaintResponseResponseDto>>(Json);
        Assert.True(list!.Count >= 2);
    }

    // ── Action Items ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActionItems_ReturnsEmpty_WhenNone()
    {
        var complaint = await CreateComplaint();
        var resp = await _client.GetAsync($"/api/complaints/{complaint.Id}/action-items");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<JsonElement>>(body, Json);
        Assert.Empty(list!);
    }

    // ── Dashboard ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        await CreateComplaint(category: "ProductDefect", severity: "Major");
        await CreateComplaint(category: "Packaging", severity: "Minor");

        var resp = await _client.GetAsync("/api/complaints/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<ComplaintDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TotalOpen >= 2);
        Assert.True(dashboard.ByCategory.ContainsKey("ProductDefect"));
        Assert.True(dashboard.BySeverity.ContainsKey("Major"));
    }

    [Fact]
    public async Task Dashboard_OverdueCount()
    {
        await CreateComplaint(responseDueDate: DateTime.UtcNow.AddDays(-5));

        var resp = await _client.GetAsync("/api/complaints/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<ComplaintDashboardDto>(Json);
        Assert.True(dashboard!.TotalOverdue >= 1);
    }

    // ── Auth Required ────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthRequired_ReturnsUnauthorized()
    {
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/complaints");
        Assert.True(resp.StatusCode == HttpStatusCode.Unauthorized
                 || resp.StatusCode == HttpStatusCode.Redirect);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task CrossTenant_ReturnsNotFound()
    {
        var complaint = await CreateComplaint();

        using var tenantBClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await tenantBClient.GetAsync($"/api/complaints/{complaint.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task Mcp_GetComplaintStatus_ReturnsData()
    {
        await CreateComplaint();

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_complaint_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Customer Complaint", body);
    }
}
