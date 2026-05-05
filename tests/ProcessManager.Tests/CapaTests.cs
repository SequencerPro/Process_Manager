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

public class CapaTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CapaTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<CapaRecordResponseDto> CreateCapa(
        string? type = null,
        string? sourceType = null,
        string? problem = null)
    {
        var dto = new CreateCapaRecordDto
        {
            Type = type ?? "Corrective",
            SourceType = sourceType ?? "Manual",
            ProblemStatement = problem ?? $"Test problem {Guid.NewGuid():N}"[..30],
            OwnerUserId = "test-user-id",
            OwnerDisplayName = "Test User"
        };
        var resp = await _client.PostAsJsonAsync("/api/capas", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json))!;
    }

    private async Task<CapaRecordResponseDto> TransitionCapa(Guid id, string? notes = null)
    {
        var dto = new TransitionCapaDto { Notes = notes };
        var resp = await _client.PostAsJsonAsync($"/api/capas/{id}/transition", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json))!;
    }

    // ── CRUD Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCapa_ReturnsCreated_WithAutoCode()
    {
        var capa = await CreateCapa();

        Assert.StartsWith("CAPA-", capa.Code);
        Assert.Equal("Corrective", capa.Type);
        Assert.Equal("Manual", capa.SourceType);
        Assert.Equal("Open", capa.Status);
        Assert.Equal("Test User", capa.OwnerDisplayName);
        Assert.True(capa.StepCount >= 1);
    }

    [Fact]
    public async Task CreateCapa_InvalidType_ReturnsBadRequest()
    {
        var dto = new CreateCapaRecordDto
        {
            Type = "InvalidType",
            ProblemStatement = "Test",
            OwnerUserId = "test",
            OwnerDisplayName = "Test"
        };
        var resp = await _client.PostAsJsonAsync("/api/capas", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetCapaById_ReturnsDetails()
    {
        var created = await CreateCapa();
        var resp = await _client.GetAsync($"/api/capas/{created.Id}");
        resp.EnsureSuccessStatusCode();
        var capa = await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json);
        Assert.NotNull(capa);
        Assert.Equal(created.Code, capa!.Code);
    }

    [Fact]
    public async Task GetCapaById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/capas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateCapa_UpdatesFields()
    {
        var capa = await CreateCapa();
        var updateDto = new UpdateCapaRecordDto
        {
            ContainmentAction = "Quarantine affected lot",
            PermanentCorrectiveAction = "Update work instruction"
        };

        var resp = await _client.PutAsJsonAsync($"/api/capas/{capa.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json);
        Assert.Equal("Quarantine affected lot", updated!.ContainmentAction);
        Assert.Equal("Update work instruction", updated.PermanentCorrectiveAction);
    }

    [Fact]
    public async Task DeleteCapa_OnlyInOpenStatus()
    {
        var capa = await CreateCapa();
        var resp = await _client.DeleteAsync($"/api/capas/{capa.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var check = await _client.GetAsync($"/api/capas/{capa.Id}");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task DeleteCapa_NonOpen_ReturnsBadRequest()
    {
        var capa = await CreateCapa();

        // Update containment so transition succeeds
        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine" }, Json);

        await TransitionCapa(capa.Id);

        var resp = await _client.DeleteAsync($"/api/capas/{capa.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── List & Filter Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        await CreateCapa();
        await CreateCapa(type: "Preventive");

        var resp = await _client.GetAsync("/api/capas?pageSize=10");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<CapaRecordSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2);
    }

    [Fact]
    public async Task GetAll_FilterByType()
    {
        await CreateCapa(type: "Preventive", problem: "Preventive test filter");

        var resp = await _client.GetAsync("/api/capas?type=Preventive");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<CapaRecordSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.All(result!.Items, c => Assert.Equal("Preventive", c.Type));
    }

    [Fact]
    public async Task GetAll_FilterBySourceType()
    {
        await CreateCapa(sourceType: "NonConformance", problem: "NC source test");

        var resp = await _client.GetAsync("/api/capas?sourceType=NonConformance");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PaginatedResponse<CapaRecordSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.All(result!.Items, c => Assert.Equal("NonConformance", c.SourceType));
    }

    // ── Lifecycle Transition Tests ───────────────────────────────────────────

    [Fact]
    public async Task Transition_Open_To_Containment()
    {
        var capa = await CreateCapa();

        // Set containment action first
        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine affected parts" }, Json);

        var transitioned = await TransitionCapa(capa.Id);
        Assert.Equal("Containment", transitioned.Status);
    }

    [Fact]
    public async Task Transition_Containment_Without_ContainmentAction_Fails()
    {
        var capa = await CreateCapa();

        // Transition Open -> Containment (no containment action set)
        // The transition itself checks for containment action when going TO RCA, not TO Containment.
        // Let's try Open -> Containment -> RCA without setting containment
        var transitioned = await TransitionCapa(capa.Id); // Open -> Containment
        Assert.Equal("Containment", transitioned.Status);

        // Now try Containment -> RCA without containment action (it was set to null)
        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/transition",
            new TransitionCapaDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Transition_Full_Lifecycle()
    {
        var capa = await CreateCapa();

        // Set containment action
        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine" }, Json);

        // Open -> Containment
        var result = await TransitionCapa(capa.Id);
        Assert.Equal("Containment", result.Status);

        // Link RCA (create an Ishikawa first)
        var ishikawa = await CreateIshikawaDiagram();
        await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = ishikawa, RootCauseAnalysisType = "Ishikawa" }, Json);

        // Containment -> RCA
        result = await TransitionCapa(capa.Id);
        Assert.Equal("RootCauseAnalysis", result.Status);

        // Set permanent corrective action
        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { PermanentCorrectiveAction = "Updated work instruction" }, Json);

        // RCA -> Implementation
        result = await TransitionCapa(capa.Id);
        Assert.Equal("Implementation", result.Status);

        // Implementation -> Verification
        result = await TransitionCapa(capa.Id);
        Assert.Equal("Verification", result.Status);

        // Verification -> EffectivenessReview
        result = await TransitionCapa(capa.Id);
        Assert.Equal("EffectivenessReview", result.Status);
    }

    [Fact]
    public async Task Transition_Verification_Without_CorrectiveAction_Fails()
    {
        var capa = await CreateCapa();

        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine" }, Json);

        await TransitionCapa(capa.Id); // -> Containment

        var ishikawa = await CreateIshikawaDiagram();
        await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = ishikawa, RootCauseAnalysisType = "Ishikawa" }, Json);

        await TransitionCapa(capa.Id); // -> RCA
        await TransitionCapa(capa.Id); // -> Implementation

        // Try Implementation -> Verification without corrective action
        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/transition",
            new TransitionCapaDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Transition_RCA_Without_LinkedRCA_Fails()
    {
        var capa = await CreateCapa();

        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine" }, Json);

        await TransitionCapa(capa.Id); // -> Containment

        // Try Containment -> RCA -> Implementation without linking RCA
        await TransitionCapa(capa.Id); // -> RCA (no RCA link required to enter RCA, but required to leave)

        // Wait... Containment -> RCA requires ContainmentAction. The controller checks:
        // nextStatus == RCA && capa.ContainmentAction == null -> error
        // But we set containment. The issue is RCA -> Implementation requires RCA linked.
        // Let me just verify.

        // This is actually checking that RCA->Implementation fails without linked RCA
        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { PermanentCorrectiveAction = "Fix" }, Json);

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/transition",
            new TransitionCapaDto(), Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Link RCA Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task LinkRca_Ishikawa_Succeeds()
    {
        var capa = await CreateCapa();
        var ishikawaId = await CreateIshikawaDiagram();

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = ishikawaId, RootCauseAnalysisType = "Ishikawa" }, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json);
        Assert.Equal(ishikawaId, updated!.RootCauseAnalysisId);
        Assert.Equal("Ishikawa", updated.RootCauseAnalysisType);
    }

    [Fact]
    public async Task LinkRca_InvalidType_ReturnsBadRequest()
    {
        var capa = await CreateCapa();

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = Guid.NewGuid(), RootCauseAnalysisType = "InvalidType" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task LinkRca_NonexistentRca_ReturnsNotFound()
    {
        var capa = await CreateCapa();

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = Guid.NewGuid(), RootCauseAnalysisType = "Ishikawa" }, Json);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Steps Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSteps_ReturnsOpenedStep()
    {
        var capa = await CreateCapa();
        var resp = await _client.GetAsync($"/api/capas/{capa.Id}/steps");
        resp.EnsureSuccessStatusCode();
        var steps = await resp.Content.ReadFromJsonAsync<List<CapaStepResponseDto>>(Json);
        Assert.NotNull(steps);
        Assert.Contains(steps!, s => s.StepType == "Opened");
    }

    [Fact]
    public async Task AddStep_CreatesRecord()
    {
        var capa = await CreateCapa();
        var dto = new CreateCapaStepDto
        {
            StepType = "Investigation",
            Notes = "Investigated root cause"
        };
        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/steps", dto, Json);
        resp.EnsureSuccessStatusCode();
        var step = await resp.Content.ReadFromJsonAsync<CapaStepResponseDto>(Json);
        Assert.Equal("Investigation", step!.StepType);
    }

    // ── Action Item Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateActionItem_LinkedToCapa()
    {
        var capa = await CreateCapa();
        var dto = new CreateActionItemDto(
            "Update work instruction",
            "Revise WI-001 to include torque spec",
            "test-user-id",
            "Test User",
            DateTime.UtcNow.AddDays(14),
            "High",
            "Capa",
            capa.Id
        );
        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/action-items", dto, Json);
        resp.EnsureSuccessStatusCode();
        var ai = await resp.Content.ReadFromJsonAsync<ActionItemDto>(Json);
        Assert.Equal("Capa", ai!.SourceType);
        Assert.Equal(capa.Id, ai.SourceEntityId);
    }

    [Fact]
    public async Task GetActionItems_ReturnsLinkedItems()
    {
        var capa = await CreateCapa();
        var dto = new CreateActionItemDto(
            "Action 1", null, "test-user-id", "Test User",
            DateTime.UtcNow.AddDays(7), "Medium", "Capa", capa.Id);
        await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/action-items", dto, Json);

        var resp = await _client.GetAsync($"/api/capas/{capa.Id}/action-items");
        resp.EnsureSuccessStatusCode();
        var items = await resp.Content.ReadFromJsonAsync<List<ActionItemDto>>(Json);
        Assert.NotEmpty(items!);
    }

    // ── Dashboard Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        await CreateCapa();

        var resp = await _client.GetAsync("/api/capas/dashboard");
        resp.EnsureSuccessStatusCode();
        var dashboard = await resp.Content.ReadFromJsonAsync<CapaDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TotalOpen >= 1);
        Assert.NotNull(dashboard.ByStatus);
        Assert.NotNull(dashboard.BySourceType);
    }

    // ── Verify & Close Tests ────────────────────────────────────────────────

    [Fact]
    public async Task Verify_AntiSelfCertification_BlocksOwner()
    {
        var capa = await AdvanceToVerification();

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/verify",
            new VerifyCapaDto { Notes = "Attempting self-verify" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Verify_DifferentUser_Succeeds()
    {
        var capa = await AdvanceToVerification();

        using var otherClient = _factory.CreateAuthenticatedClient("other-engineer-id", "Engineer");
        var resp = await otherClient.PostAsJsonAsync($"/api/capas/{capa.Id}/verify",
            new VerifyCapaDto { Notes = "Verified by independent engineer" }, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json);
        Assert.NotNull(updated!.VerifiedAt);
    }

    [Fact]
    public async Task Close_WithoutEffectivenessVerification_Fails()
    {
        var capa = await AdvanceToEffectivenessReview();

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/close",
            new TransitionCapaDto { Notes = "Close" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Close_AfterEffectivenessVerification_Succeeds()
    {
        var capa = await AdvanceToEffectivenessReview();

        using var otherClient = _factory.CreateAuthenticatedClient("verifier-id", "Engineer");
        await otherClient.PostAsJsonAsync($"/api/capas/{capa.Id}/verify-effectiveness",
            new VerifyCapaDto { Notes = "Effectiveness confirmed" }, Json);

        var resp = await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/close",
            new TransitionCapaDto { Notes = "CAPA closed" }, Json);
        resp.EnsureSuccessStatusCode();

        var closed = await resp.Content.ReadFromJsonAsync<CapaRecordResponseDto>(Json);
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.ClosedAt);
    }

    [Fact]
    public async Task UpdateClosedCapa_ReturnsBadRequest()
    {
        var capa = await AdvanceToEffectivenessReview();

        using var otherClient = _factory.CreateAuthenticatedClient("verifier-id", "Engineer");
        await otherClient.PostAsJsonAsync($"/api/capas/{capa.Id}/verify-effectiveness",
            new VerifyCapaDto { Notes = "OK" }, Json);

        await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/close",
            new TransitionCapaDto(), Json);

        var resp = await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ProblemStatement = "Changed" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Auth Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Capa_RequiresAuth()
    {
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/capas");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-Tenant Isolation ───────────────────────────────────────────────

    [Fact]
    public async Task Capa_CrossTenant_ReturnsNotFound()
    {
        var capa = await CreateCapa();

        using var tenantBClient = _factory.CreateTenantClient(Guid.NewGuid());
        var resp = await tenantBClient.GetAsync($"/api/capas/{capa.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ────────────────────────────────────────────────────────

    [Fact]
    public async Task Mcp_GetCapaStatus_ReturnsData()
    {
        await CreateCapa();

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_capa_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("CAPA", body);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateIshikawaDiagram()
    {
        var dto = new
        {
            title = "Test Ishikawa",
            problemStatement = "Test problem for CAPA",
            linkedEntityType = "Manual"
        };
        var resp = await _client.PostAsJsonAsync("/api/ishikawa", dto, Json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
        return result.GetProperty("id").GetGuid();
    }

    private async Task<CapaRecordResponseDto> AdvanceToVerification()
    {
        var capa = await CreateCapa();

        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { ContainmentAction = "Quarantine" }, Json);
        await TransitionCapa(capa.Id); // -> Containment

        var ishikawa = await CreateIshikawaDiagram();
        await _client.PostAsJsonAsync($"/api/capas/{capa.Id}/link-rca",
            new LinkRcaDto { RootCauseAnalysisId = ishikawa, RootCauseAnalysisType = "Ishikawa" }, Json);
        await TransitionCapa(capa.Id); // -> RCA

        await _client.PutAsJsonAsync($"/api/capas/{capa.Id}",
            new UpdateCapaRecordDto { PermanentCorrectiveAction = "Updated procedure" }, Json);
        await TransitionCapa(capa.Id); // -> Implementation
        return await TransitionCapa(capa.Id); // -> Verification
    }

    private async Task<CapaRecordResponseDto> AdvanceToEffectivenessReview()
    {
        var capa = await AdvanceToVerification();

        using var otherClient = _factory.CreateAuthenticatedClient("verifier-id", "Engineer");
        await otherClient.PostAsJsonAsync($"/api/capas/{capa.Id}/verify",
            new VerifyCapaDto { Notes = "Verified" }, Json);

        return await TransitionCapa(capa.Id); // -> EffectivenessReview
    }
}
