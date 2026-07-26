using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class ChangeOrderTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ChangeOrderTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ChangeOrderResponseDto> CreateEco(
        string? type = null,
        string? priority = null,
        string? title = null)
    {
        var dto = new CreateChangeOrderDto
        {
            Type = type ?? "ProcessChange",
            Priority = priority ?? "Routine",
            Title = title ?? $"Test ECO {Guid.NewGuid():N}"[..30],
            Description = "Test description",
            Justification = "Test justification",
            RequestedByUserId = "test-user-id",
            RequestedByDisplayName = "Test User"
        };
        var resp = await _client.PostAsJsonAsync("/api/change-orders", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json))!;
    }

    private async Task<ChangeOrderImpactResponseDto> AddImpact(Guid ecoId, string? entityType = null)
    {
        var dto = new CreateChangeOrderImpactDto
        {
            AffectedEntityType = entityType ?? "Process",
            AffectedEntityId = Guid.NewGuid(),
            AffectedEntityName = "Test Process",
            ImpactDescription = "Updated step sequence",
            MitigationPlan = "Retrain operators"
        };
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{ecoId}/impacts", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderImpactResponseDto>(Json))!;
    }

    private async Task<ChangeOrderApproverResponseDto> AddApprover(Guid ecoId, string? userId = null)
    {
        var dto = new AddChangeOrderApproverDto
        {
            UserId = userId ?? $"approver-{Guid.NewGuid():N}"[..20],
            DisplayName = "Approver User",
            Role = "Quality Manager"
        };
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{ecoId}/approvers", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderApproverResponseDto>(Json))!;
    }

    private async Task<ChangeOrderTaskResponseDto> AddTask(Guid ecoId, string? title = null)
    {
        var dto = new CreateChangeOrderTaskDto
        {
            Title = title ?? "Update work instruction",
            AssigneeUserId = "assignee-user",
            AssigneeDisplayName = "Assignee User",
            DueDate = DateTime.UtcNow.AddDays(14)
        };
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{ecoId}/tasks", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderTaskResponseDto>(Json))!;
    }

    // ── CRUD Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEco_ReturnsCreated_WithAutoCode()
    {
        var eco = await CreateEco();

        Assert.StartsWith("ECO-", eco.Code);
        Assert.Equal("ProcessChange", eco.Type);
        Assert.Equal("Routine", eco.Priority);
        Assert.Equal("Draft", eco.Status);
        Assert.Equal("Test User", eco.RequestedByDisplayName);
    }

    [Fact]
    public async Task CreateEco_InvalidType_ReturnsBadRequest()
    {
        var dto = new CreateChangeOrderDto
        {
            Type = "InvalidType",
            Title = "Test",
            RequestedByUserId = "user",
            RequestedByDisplayName = "User"
        };
        var resp = await _client.PostAsJsonAsync("/api/change-orders", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsEco()
    {
        var eco = await CreateEco();
        var resp = await _client.GetAsync($"/api/change-orders/{eco.Id}");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal(eco.Id, result!.Id);
        Assert.Equal(eco.Code, result.Code);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/change-orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateEco_UpdatesFields()
    {
        var eco = await CreateEco();
        var dto = new UpdateChangeOrderDto
        {
            Title = "Updated Title",
            Priority = "Urgent",
            Description = "Updated desc"
        };
        var resp = await _client.PutAsJsonAsync($"/api/change-orders/{eco.Id}", dto, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Updated Title", result!.Title);
        Assert.Equal("Urgent", result.Priority);
    }

    [Fact]
    public async Task DeleteEco_DraftOnly()
    {
        var eco = await CreateEco();
        var resp = await _client.DeleteAsync($"/api/change-orders/{eco.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteEco_NonDraft_ReturnsBadRequest()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id);
        await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);

        var resp = await _client.DeleteAsync($"/api/change-orders/{eco.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ListEcos_ReturnsPaginated()
    {
        await CreateEco();
        await CreateEco();

        var resp = await _client.GetAsync("/api/change-orders?page=1&pageSize=10");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 2);
    }

    [Fact]
    public async Task FilterByStatus_ReturnsFiltered()
    {
        await CreateEco();
        var resp = await _client.GetAsync("/api/change-orders?status=Draft");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(result.GetProperty("totalCount").GetInt32() >= 1);
    }

    // ── Lifecycle Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_RequiresImpact()
    {
        var eco = await CreateEco();
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Submit_WithImpact_Succeeds()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id);
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("ImpactAnalysis", result!.Status);
    }

    [Fact]
    public async Task RequestApproval_RequiresApprover()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id);
        await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/request-approval",
            new TransitionChangeOrderDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task RequestApproval_WithApprover_Succeeds()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id);
        await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);
        await AddApprover(eco.Id);

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/request-approval",
            new TransitionChangeOrderDto(), Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Approval", result!.Status);
    }

    [Fact]
    public async Task Approve_RequiresAllDecisions()
    {
        var eco = await AdvanceToApproval();

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/approve",
            new TransitionChangeOrderDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Approve_AllApproved_Succeeds()
    {
        var eco = await AdvanceToApproval();
        var approvers = await _client.GetFromJsonAsync<List<ChangeOrderApproverResponseDto>>(
            $"/api/change-orders/{eco.Id}/approvers", Json);

        foreach (var a in approvers!)
        {
            await _client.PostAsJsonAsync(
                $"/api/change-orders/{eco.Id}/approvers/{a.Id}/decide",
                new RecordApproverDecisionDto { Decision = "Approved", Comments = "Looks good" }, Json);
        }

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/approve",
            new TransitionChangeOrderDto(), Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Implementation", result!.Status);
    }

    [Fact]
    public async Task Approve_WithRejection_Fails()
    {
        var eco = await AdvanceToApproval();
        var approvers = await _client.GetFromJsonAsync<List<ChangeOrderApproverResponseDto>>(
            $"/api/change-orders/{eco.Id}/approvers", Json);

        await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/approvers/{approvers![0].Id}/decide",
            new RecordApproverDecisionDto { Decision = "Rejected", Comments = "Not acceptable" }, Json);

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/approve",
            new TransitionChangeOrderDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reject_FromApproval_Succeeds()
    {
        var eco = await AdvanceToApproval();
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/reject",
            new RejectChangeOrderDto { Reason = "Budget constraints" }, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Rejected", result!.Status);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task Reject_FromDraft_Fails()
    {
        var eco = await CreateEco();
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/reject",
            new RejectChangeOrderDto { Reason = "Test" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CompleteImplementation_RequiresTasksDone()
    {
        var eco = await AdvanceToImplementation();
        await AddTask(eco.Id);

        var resp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/complete-implementation",
            new TransitionChangeOrderDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_DraftToClosed()
    {
        var eco = await AdvanceToImplementation();
        var task = await AddTask(eco.Id);

        await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/tasks/{task.Id}/complete",
            new CompleteChangeOrderTaskDto { Notes = "Done" }, Json);

        var implResp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/complete-implementation",
            new TransitionChangeOrderDto(), Json);
        implResp.EnsureSuccessStatusCode();
        var verification = await implResp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Verification", verification!.Status);

        var closeResp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/close",
            new TransitionChangeOrderDto(), Json);
        closeResp.EnsureSuccessStatusCode();
        var closed = await closeResp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json);
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.ClosedAt);
    }

    // ── Impact Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddImpact_ReturnsImpact()
    {
        var eco = await CreateEco();
        var impact = await AddImpact(eco.Id);

        Assert.Equal("Process", impact.AffectedEntityType);
        Assert.Equal("Test Process", impact.AffectedEntityName);
    }

    [Fact]
    public async Task AddImpact_InvalidEntityType_ReturnsBadRequest()
    {
        var eco = await CreateEco();
        var dto = new CreateChangeOrderImpactDto
        {
            AffectedEntityType = "InvalidType",
            AffectedEntityId = Guid.NewGuid()
        };
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/impacts", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetImpacts_ReturnsList()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id, "Process");
        await AddImpact(eco.Id, "ControlPlan");

        var impacts = await _client.GetFromJsonAsync<List<ChangeOrderImpactResponseDto>>(
            $"/api/change-orders/{eco.Id}/impacts", Json);
        Assert.Equal(2, impacts!.Count);
    }

    [Fact]
    public async Task DeleteImpact_Succeeds()
    {
        var eco = await CreateEco();
        var impact = await AddImpact(eco.Id);

        var resp = await _client.DeleteAsync($"/api/change-orders/{eco.Id}/impacts/{impact.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    // ── Approver Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddApprover_DuplicateUser_ReturnsConflict()
    {
        var eco = await CreateEco();
        var userId = "same-user-id";
        await AddApprover(eco.Id, userId);

        var dto = new AddChangeOrderApproverDto
        {
            UserId = userId,
            DisplayName = "Same User"
        };
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/approvers", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task RecordDecision_InvalidDecision_ReturnsBadRequest()
    {
        var eco = await AdvanceToApproval();
        var approvers = await _client.GetFromJsonAsync<List<ChangeOrderApproverResponseDto>>(
            $"/api/change-orders/{eco.Id}/approvers", Json);

        var resp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/approvers/{approvers![0].Id}/decide",
            new RecordApproverDecisionDto { Decision = "Invalid" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task RecordDecision_Pending_ReturnsBadRequest()
    {
        var eco = await AdvanceToApproval();
        var approvers = await _client.GetFromJsonAsync<List<ChangeOrderApproverResponseDto>>(
            $"/api/change-orders/{eco.Id}/approvers", Json);

        var resp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/approvers/{approvers![0].Id}/decide",
            new RecordApproverDecisionDto { Decision = "Pending" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task RemoveApprover_Succeeds()
    {
        var eco = await CreateEco();
        var approver = await AddApprover(eco.Id);

        var resp = await _client.DeleteAsync(
            $"/api/change-orders/{eco.Id}/approvers/{approver.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    // ── Task Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddTask_ReturnsTask()
    {
        var eco = await CreateEco();
        var task = await AddTask(eco.Id);

        Assert.Equal("Update work instruction", task.Title);
        Assert.Equal("Open", task.Status);
    }

    [Fact]
    public async Task CompleteTask_SetsCompleted()
    {
        var eco = await CreateEco();
        var task = await AddTask(eco.Id);

        var resp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/tasks/{task.Id}/complete",
            new CompleteChangeOrderTaskDto { Notes = "All done" }, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderTaskResponseDto>(Json);
        Assert.Equal("Complete", result!.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_AlreadyCompleted_ReturnsBadRequest()
    {
        var eco = await CreateEco();
        var task = await AddTask(eco.Id);

        await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/tasks/{task.Id}/complete",
            new CompleteChangeOrderTaskDto(), Json);

        var resp = await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/tasks/{task.Id}/complete",
            new CompleteChangeOrderTaskDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetTasks_ReturnsList()
    {
        var eco = await CreateEco();
        await AddTask(eco.Id, "Task 1");
        await AddTask(eco.Id, "Task 2");

        var tasks = await _client.GetFromJsonAsync<List<ChangeOrderTaskResponseDto>>(
            $"/api/change-orders/{eco.Id}/tasks", Json);
        Assert.Equal(2, tasks!.Count);
    }

    [Fact]
    public async Task DeleteTask_Succeeds()
    {
        var eco = await CreateEco();
        var task = await AddTask(eco.Id);

        var resp = await _client.DeleteAsync($"/api/change-orders/{eco.Id}/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        await CreateEco();
        var resp = await _client.GetAsync("/api/change-orders/dashboard");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ChangeOrderDashboardDto>(Json);
        Assert.True(result!.TotalOpen >= 1);
        Assert.NotNull(result.ByStatus);
        Assert.NotNull(result.ByType);
    }

    // ── Update Closed ECO Blocked ────────────────────────────────────────────

    [Fact]
    public async Task UpdateClosedEco_ReturnsBadRequest()
    {
        var eco = await AdvanceToImplementation();
        // Complete implementation (no tasks)
        await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/complete-implementation",
            new TransitionChangeOrderDto(), Json);
        await _client.PostAsJsonAsync(
            $"/api/change-orders/{eco.Id}/close",
            new TransitionChangeOrderDto(), Json);

        var resp = await _client.PutAsJsonAsync($"/api/change-orders/{eco.Id}",
            new UpdateChangeOrderDto { Title = "Should fail" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Auth Required ────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthRequired_ReturnsUnauthorized()
    {
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/change-orders");
        Assert.True(resp.StatusCode == HttpStatusCode.Unauthorized
                 || resp.StatusCode == HttpStatusCode.Redirect);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task CrossTenant_ReturnsNotFound()
    {
        var eco = await CreateEco();

        using var tenantBClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await tenantBClient.GetAsync($"/api/change-orders/{eco.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task Mcp_GetChangeOrderStatus_ReturnsData()
    {
        await CreateEco();

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_change_order_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Change Order", body);
    }

    // ── Lifecycle Helpers ────────────────────────────────────────────────────

    private async Task<ChangeOrderResponseDto> AdvanceToApproval()
    {
        var eco = await CreateEco();
        await AddImpact(eco.Id);
        await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/submit",
            new TransitionChangeOrderDto(), Json);
        await AddApprover(eco.Id);
        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/request-approval",
            new TransitionChangeOrderDto(), Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json))!;
    }

    private async Task<ChangeOrderResponseDto> AdvanceToImplementation()
    {
        var eco = await AdvanceToApproval();
        var approvers = await _client.GetFromJsonAsync<List<ChangeOrderApproverResponseDto>>(
            $"/api/change-orders/{eco.Id}/approvers", Json);

        foreach (var a in approvers!)
        {
            await _client.PostAsJsonAsync(
                $"/api/change-orders/{eco.Id}/approvers/{a.Id}/decide",
                new RecordApproverDecisionDto { Decision = "Approved" }, Json);
        }

        var resp = await _client.PostAsJsonAsync($"/api/change-orders/{eco.Id}/approve",
            new TransitionChangeOrderDto(), Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeOrderResponseDto>(Json))!;
    }
}
