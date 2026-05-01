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

public class ConformanceTests : IClassFixture<TestWebApplicationFactory>, IDisposable, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ConformanceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tc = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using (tc.BeginScope(TestWebApplicationFactory.DefaultTenantId))
        {
            await DataSeeder.SeedStandardsClausesAsync(db);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Standards Clauses: seeded data is accessible ─────────────────────────

    [Fact]
    public async Task GetStandardsClauses_ReturnsSeededClauses()
    {
        var resp = await _client.GetAsync("/api/standards-clauses");
        resp.EnsureSuccessStatusCode();

        var clauses = await resp.Content.ReadFromJsonAsync<List<StandardsClauseSummaryDto>>(Json);
        Assert.NotNull(clauses);
        Assert.True(clauses.Count >= 10, "Expected at least 10 seeded ISO 9001 clauses");
        Assert.Contains(clauses, c => c.ClauseNumber == "4.1");
    }

    // ── Standards Clauses: filter by standard ────────────────────────────────

    [Fact]
    public async Task GetStandardsClauses_FilterByStandard()
    {
        var resp = await _client.GetAsync("/api/standards-clauses?standard=Iso9001_2015");
        resp.EnsureSuccessStatusCode();

        var clauses = await resp.Content.ReadFromJsonAsync<List<StandardsClauseSummaryDto>>(Json);
        Assert.NotNull(clauses);
        Assert.All(clauses, c => Assert.Equal("Iso9001_2015", c.Standard));
    }

    // ── Standards Clauses: get by ID ─────────────────────────────────────────

    [Fact]
    public async Task GetStandardsClause_ById_ReturnsDetail()
    {
        var all = await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json);
        var first = all!.First();

        var resp = await _client.GetAsync($"/api/standards-clauses/{first.Id}");
        resp.EnsureSuccessStatusCode();

        var clause = await resp.Content.ReadFromJsonAsync<StandardsClauseDto>(Json);
        Assert.NotNull(clause);
        Assert.Equal(first.ClauseNumber, clause.ClauseNumber);
        Assert.False(string.IsNullOrEmpty(clause.RequirementSummary));
    }

    // ── Conformance Dashboard: returns aggregate data ────────────────────────

    [Fact]
    public async Task ConformanceDashboard_ReturnsAggregate()
    {
        var resp = await _client.GetAsync("/api/standards-clauses/dashboard");
        resp.EnsureSuccessStatusCode();

        var dashboard = await resp.Content.ReadFromJsonAsync<ConformanceDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalClauses >= 10);
        Assert.Equal(dashboard.TotalClauses,
            dashboard.CoveredCount + dashboard.PartialCount + dashboard.GapCount + dashboard.OpenMajorFindingCount);
    }

    // ── Evidence Links: add and delete ───────────────────────────────────────

    [Fact]
    public async Task EvidenceLink_AddAndDelete_RoundTrip()
    {
        var clauses = await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json);
        var clauseId = clauses!.First().Id;
        var fakeEntityId = Guid.NewGuid();

        var createDto = new CreateClauseEvidenceLinkDto(clauseId, "Process", fakeEntityId, "Test evidence note");
        var createResp = await _client.PostAsJsonAsync($"/api/standards-clauses/{clauseId}/evidence", createDto, Json);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var link = await createResp.Content.ReadFromJsonAsync<ClauseEvidenceLinkDto>(Json);
        Assert.NotNull(link);
        Assert.Equal("Process", link.EntityType);
        Assert.Equal("Test evidence note", link.EvidenceNote);
        Assert.False(link.IsAutoLinked);

        var links = await _client.GetFromJsonAsync<List<ClauseEvidenceLinkDto>>(
            $"/api/standards-clauses/{clauseId}/evidence", Json);
        Assert.Contains(links!, l => l.Id == link.Id);

        var delResp = await _client.DeleteAsync($"/api/standards-clauses/{clauseId}/evidence/{link.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    }

    // ── Evidence Links: duplicate rejected ───────────────────────────────────

    [Fact]
    public async Task EvidenceLink_Duplicate_Returns409()
    {
        var clauses = await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json);
        var clauseId = clauses!.First().Id;
        var entityId = Guid.NewGuid();

        var dto = new CreateClauseEvidenceLinkDto(clauseId, "ControlPlan", entityId, null);
        var r1 = await _client.PostAsJsonAsync($"/api/standards-clauses/{clauseId}/evidence", dto, Json);
        r1.EnsureSuccessStatusCode();

        var r2 = await _client.PostAsJsonAsync($"/api/standards-clauses/{clauseId}/evidence", dto, Json);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    // ── Audit Programme: CRUD lifecycle ──────────────────────────────────────

    [Fact]
    public async Task AuditProgram_CrudLifecycle()
    {
        var createDto = new CreateAuditProgramDto("Test Audit Prog", "Iso9001_2015", 2026, "Alice Auditor");
        var createResp = await _client.PostAsJsonAsync("/api/audit-programs", createDto, Json);
        createResp.EnsureSuccessStatusCode();

        var program = await createResp.Content.ReadFromJsonAsync<AuditProgramDto>(Json);
        Assert.NotNull(program);
        Assert.Equal("Planning", program.Status);
        Assert.Equal(2026, program.Year);

        var updateDto = new UpdateAuditProgramDto("Updated Prog Name", "Iso9001_2015", 2026, "Bob Auditor");
        var updateResp = await _client.PutAsJsonAsync($"/api/audit-programs/{program.Id}", updateDto, Json);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<AuditProgramDto>(Json);
        Assert.Equal("Updated Prog Name", updated!.Name);
        Assert.Equal("Bob Auditor", updated.LeadAuditor);

        var getResp = await _client.GetFromJsonAsync<AuditProgramDto>($"/api/audit-programs/{program.Id}", Json);
        Assert.Equal("Updated Prog Name", getResp!.Name);
    }

    // ── Audit Programme: activate and close lifecycle ─────────────────────────

    [Fact]
    public async Task AuditProgram_ActivateAndClose()
    {
        var program = await CreateProgram();

        var actResp = await _client.PostAsync($"/api/audit-programs/{program.Id}/activate", null);
        actResp.EnsureSuccessStatusCode();
        var activated = await actResp.Content.ReadFromJsonAsync<AuditProgramDto>(Json);
        Assert.Equal("Active", activated!.Status);

        var closeResp = await _client.PostAsync($"/api/audit-programs/{program.Id}/close", null);
        closeResp.EnsureSuccessStatusCode();
        var closed = await closeResp.Content.ReadFromJsonAsync<AuditProgramDto>(Json);
        Assert.Equal("Closed", closed!.Status);
    }

    // ── Audit Programme: invalid state transitions blocked ───────────────────

    [Fact]
    public async Task AuditProgram_InvalidTransition_Returns400()
    {
        var program = await CreateProgram();

        var closeResp = await _client.PostAsync($"/api/audit-programs/{program.Id}/close", null);
        Assert.Equal(HttpStatusCode.BadRequest, closeResp.StatusCode);
    }

    // ── Audit Programme: delete blocked with audits ─────────────────────────

    [Fact]
    public async Task AuditProgram_DeleteBlockedWithAudits()
    {
        var program = await CreateProgram();
        await CreateAudit(program.Id);

        var delResp = await _client.DeleteAsync($"/api/audit-programs/{program.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delResp.StatusCode);
    }

    // ── Audit Programme: delete succeeds when empty ─────────────────────────

    [Fact]
    public async Task AuditProgram_DeleteSucceeds_WhenEmpty()
    {
        var program = await CreateProgram();

        var delResp = await _client.DeleteAsync($"/api/audit-programs/{program.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
    }

    // ── Audit: CRUD and lifecycle ────────────────────────────────────────────

    [Fact]
    public async Task Audit_CreateStartComplete_Lifecycle()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);

        Assert.Equal("Planned", audit.Status);
        Assert.Equal(program.Id, audit.ProgramId);

        var startResp = await _client.PostAsync($"/api/audits/{audit.Id}/start", null);
        startResp.EnsureSuccessStatusCode();
        var started = await startResp.Content.ReadFromJsonAsync<AuditDto>(Json);
        Assert.Equal("InProgress", started!.Status);
        Assert.NotNull(started.ActualDate);

        var completeResp = await _client.PostAsync($"/api/audits/{audit.Id}/complete", null);
        completeResp.EnsureSuccessStatusCode();
        var completed = await completeResp.Content.ReadFromJsonAsync<AuditDto>(Json);
        Assert.Equal("Complete", completed!.Status);
    }

    // ── Audit: invalid transitions blocked ───────────────────────────────────

    [Fact]
    public async Task Audit_CompleteWithoutStart_Returns400()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);

        var resp = await _client.PostAsync($"/api/audits/{audit.Id}/complete", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Finding: add and raise corrective action ─────────────────────────────

    [Fact]
    public async Task Finding_AddAndRaiseCorrectiveAction()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);

        var clauses = await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json);
        var clauseId = clauses!.First().Id;

        var findingDto = new CreateAuditFindingDto(clauseId, "MajorNonconformance",
            "Missing documented procedure for context of the organisation",
            "No evidence of documented process for 4.1 requirements");

        var findResp = await _client.PostAsJsonAsync($"/api/audits/{audit.Id}/findings", findingDto, Json);
        findResp.EnsureSuccessStatusCode();
        var finding = await findResp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);
        Assert.NotNull(finding);
        Assert.Equal("Open", finding.Status);
        Assert.Equal("MajorNonconformance", finding.FindingType);
        Assert.Null(finding.ActionItemId);

        var caResp = await _client.PostAsync($"/api/audits/{audit.Id}/findings/{finding.Id}/raise-ca", null);
        caResp.EnsureSuccessStatusCode();
        var withCa = await caResp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);
        Assert.NotNull(withCa!.ActionItemId);
        Assert.Equal("CorrectiveActionRaised", withCa.Status);
        Assert.NotNull(withCa.ActionItemTitle);
    }

    // ── Finding: observation cannot have CA raised ───────────────────────────

    [Fact]
    public async Task Finding_ObservationCannotRaiseCa()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);
        var clauseId = (await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json))!.First().Id;

        var dto = new CreateAuditFindingDto(clauseId, "Observation", "Observed gap", "Evidence");
        var resp = await _client.PostAsJsonAsync($"/api/audits/{audit.Id}/findings", dto, Json);
        resp.EnsureSuccessStatusCode();
        var finding = await resp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);

        var caResp = await _client.PostAsync($"/api/audits/{audit.Id}/findings/{finding!.Id}/raise-ca", null);
        Assert.Equal(HttpStatusCode.BadRequest, caResp.StatusCode);
    }

    // ── Finding: close requires verified CA when present ─────────────────────

    [Fact]
    public async Task Finding_CloseBlockedUntilCaVerified()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);
        var clauseId = (await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json))!.First().Id;

        var dto = new CreateAuditFindingDto(clauseId, "MinorNonconformance", "Minor gap", "Evidence");
        var fResp = await _client.PostAsJsonAsync($"/api/audits/{audit.Id}/findings", dto, Json);
        fResp.EnsureSuccessStatusCode();
        var finding = await fResp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);

        await _client.PostAsync($"/api/audits/{audit.Id}/findings/{finding!.Id}/raise-ca", null);

        var closeResp = await _client.PostAsJsonAsync(
            $"/api/audits/{audit.Id}/findings/{finding.Id}/close",
            new CloseAuditFindingDto("Trying to close"), Json);
        Assert.Equal(HttpStatusCode.BadRequest, closeResp.StatusCode);
    }

    // ── Finding: close succeeds without CA ───────────────────────────────────

    [Fact]
    public async Task Finding_CloseSucceeds_ObservationWithoutCa()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);
        var clauseId = (await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json))!.First().Id;

        var dto = new CreateAuditFindingDto(clauseId, "Observation", "Observed gap", "Evidence");
        var fResp = await _client.PostAsJsonAsync($"/api/audits/{audit.Id}/findings", dto, Json);
        fResp.EnsureSuccessStatusCode();
        var finding = await fResp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);

        var closeResp = await _client.PostAsJsonAsync(
            $"/api/audits/{audit.Id}/findings/{finding!.Id}/close",
            new CloseAuditFindingDto("Acknowledged and noted"), Json);
        closeResp.EnsureSuccessStatusCode();
        var closed = await closeResp.Content.ReadFromJsonAsync<AuditFindingDto>(Json);
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.ClosedAt);
    }

    // ── Dashboard: finding affects coverage status ───────────────────────────

    [Fact]
    public async Task Dashboard_MajorFinding_AffectsCoverage()
    {
        var program = await CreateProgram();
        var audit = await CreateAudit(program.Id);
        var clauses = await _client.GetFromJsonAsync<List<StandardsClauseSummaryDto>>("/api/standards-clauses", Json);
        var clauseId = clauses!.First().Id;

        var dto = new CreateAuditFindingDto(clauseId, "MajorNonconformance", "Major gap", "Evidence");
        await _client.PostAsJsonAsync($"/api/audits/{audit.Id}/findings", dto, Json);

        var dashboard = await _client.GetFromJsonAsync<ConformanceDashboardDto>("/api/standards-clauses/dashboard", Json);
        Assert.True(dashboard!.OpenMajorFindingCount >= 1);
    }

    // ── Conformance requires authentication ──────────────────────────────────

    [Fact]
    public async Task Conformance_RequiresAuth()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/standards-clauses");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Cross-tenant isolation ───────────────────────────────────────────────

    [Fact]
    public async Task AuditProgram_CrossTenantIsolation()
    {
        var tenant2Id = _factory.CreateTenant("conformance-iso-tenant");
        var tenant2Client = _factory.CreateTenantClient(tenant2Id);

        var program = await CreateProgram();

        var resp = await tenant2Client.GetAsync($"/api/audit-programs/{program.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP tool: get_conformance_status ─────────────────────────────────────

    [Fact]
    public async Task McpTool_GetConformanceStatus_ReturnsData()
    {
        var initPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "test", version = "1.0" } },
            id = 1
        });
        var initResp = await _client.PostAsync("/mcp",
            new StringContent(initPayload, System.Text.Encoding.UTF8, "application/json"));
        initResp.EnsureSuccessStatusCode();

        var toolPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_conformance_status", arguments = new { } },
            id = 2
        });
        var toolResp = await _client.PostAsync("/mcp",
            new StringContent(toolPayload, System.Text.Encoding.UTF8, "application/json"));
        toolResp.EnsureSuccessStatusCode();

        var body = await toolResp.Content.ReadAsStringAsync();
        Assert.Contains("Covered", body);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<AuditProgramDto> CreateProgram()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var dto = new CreateAuditProgramDto($"TestProg-{pfx}", "Iso9001_2015", 2026, "TestAuditor");
        var resp = await _client.PostAsJsonAsync("/api/audit-programs", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuditProgramDto>(Json))!;
    }

    private async Task<AuditDto> CreateAudit(Guid programId)
    {
        var dto = new CreateAuditDto(programId, "Internal", "Test scope — all clauses",
            DateTime.UtcNow.AddDays(30), "TestAuditor");
        var resp = await _client.PostAsJsonAsync("/api/audits", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuditDto>(Json))!;
    }
}
