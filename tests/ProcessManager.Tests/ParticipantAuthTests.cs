using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Verifies that the Participant role is properly restricted from mutation endpoints
/// and that read operations remain accessible.
/// </summary>
public class ParticipantAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _participant;
    private readonly HttpClient _engineer;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ParticipantAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _participant = factory.CreateAuthenticatedClient("participant-user", "Participant");
        _engineer    = factory.CreateAuthenticatedClient("engineer-user",    "Engineer");
    }

    // ── Kinds ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Participant_CanRead_Kinds()
    {
        var response = await _participant.GetAsync("/api/kinds");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreate_Kind()
    {
        var dto = new KindCreateDto("PART-K001", "Participant Kind", null, false, false);
        var response = await _participant.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── StepTemplates ───────────────────────────────────────────────────────

    [Fact]
    public async Task Participant_CanRead_StepTemplates()
    {
        var response = await _participant.GetAsync("/api/steptemplates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreate_StepTemplate()
    {
        var dto = new StepTemplateCreateDto(
            "PART-ST001", "Participant Template", null,
            Domain.Enums.StepPattern.General, new());
        var response = await _participant.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Processes ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Participant_CanRead_Processes()
    {
        var response = await _participant.GetAsync("/api/processes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreate_Process()
    {
        var dto = new ProcessCreateDto("PART-P001", "Participant Process", null);
        var response = await _participant.PostAsJsonAsync("/api/processes", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Workflows ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Participant_CanRead_Workflows()
    {
        var response = await _participant.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreate_Workflow()
    {
        var dto = new CreateWorkflowDto("PART-WF001", "Participant Workflow", null);
        var response = await _participant.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── StepExecutions ──────────────────────────────────────────────────────

    [Fact]
    public async Task Participant_CanRead_StepExecutions()
    {
        var response = await _participant.GetAsync("/api/step-executions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Engineer can still mutate ───────────────────────────────────────────

    [Fact]
    public async Task Engineer_CanCreate_Kind()
    {
        var dto = new KindCreateDto($"ENG-K{Guid.NewGuid().ToString()[..4]}", "Engineer Kind", null, false, false);
        var response = await _engineer.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
