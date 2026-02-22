using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.Controllers;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class WorkflowTests : IntegrationTestBase
{
    public WorkflowTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────── Helpers ────────

    private async Task<WorkflowResponseDto> CreateWorkflow(
        string? code = null, string name = "Test Workflow")
    {
        code ??= $"WF-{Guid.NewGuid().ToString()[..6]}";
        var dto = new CreateWorkflowDto(code, name);
        var response = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowProcessResponseDto> AddWorkflowProcess(
        Guid workflowId, Guid processId, bool isEntryPoint = false, int sortOrder = 0)
    {
        var dto = new AddWorkflowProcessDto(processId, isEntryPoint, sortOrder);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/processes", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowLinkResponseDto> AddWorkflowLink(
        Guid workflowId, Guid sourceWpId, Guid targetWpId,
        RoutingType routingType = RoutingType.Always,
        string? name = null,
        List<Guid>? conditionGradeIds = null)
    {
        var dto = new CreateWorkflowLinkDto(sourceWpId, targetWpId, routingType, name, 0, conditionGradeIds);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/links", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(JsonOptions))!;
    }

    /// <summary>
    /// Builds a full Widget Manufacturing Workflow scenario:
    /// - Widget Finishing process (entry point)
    /// - Packaging process
    /// - Rework process
    /// - GradeBased links from Finishing → Packaging (Passed) and Finishing → Rework (Failed)
    /// - Always link from Rework → Finishing
    /// </summary>
    private async Task<WorkflowScenario> BuildWorkflowScenario()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Kind + Grades
        var widget = await CreateKind($"WDG-{pfx}", "Widget", isSerialized: true);
        var rawGrade = await CreateGrade(widget.Id, "RAW", "Raw", isDefault: true);
        var passedGrade = await CreateGrade(widget.Id, "PASS", "Passed");
        var failedGrade = await CreateGrade(widget.Id, "FAIL", "Failed-Dimensional");

        // Widget Finishing Process (Deburr → Inspection)
        var deburr = await CreateTransformStep($"DEB-{pfx}", "Deburr",
            widget.Id, rawGrade.Id, widget.Id, rawGrade.Id);
        var inspection = await CreateDivisionStep($"INS-{pfx}", "Inspection",
            widget.Id, rawGrade.Id,
            new List<(string, Guid, Guid)>
            {
                ("Good Part", widget.Id, passedGrade.Id),
                ("Failed Part", widget.Id, failedGrade.Id)
            });
        var finishing = await CreateProcess($"FIN-{pfx}", "Widget Finishing");
        var finStep1 = await AddProcessStep(finishing.Id, deburr.Id, 1);
        var finStep2 = await AddProcessStep(finishing.Id, inspection.Id, 2);
        var deburrOut = deburr.Ports!.First(p => p.Direction == PortDirection.Output);
        var inspIn = inspection.Ports!.First(p => p.Direction == PortDirection.Input);
        await AddFlow(finishing.Id, finStep1.Id, deburrOut.Id, finStep2.Id, inspIn.Id);

        // Packaging Process
        var pkgStep = await CreateTransformStep($"PKG-{pfx}", "Package",
            widget.Id, passedGrade.Id, widget.Id, passedGrade.Id);
        var packaging = await CreateProcess($"PKG-{pfx}", "Packaging");
        await AddProcessStep(packaging.Id, pkgStep.Id, 1);

        // Rework Process
        var rwkStep = await CreateTransformStep($"RWK-{pfx}", "Rework",
            widget.Id, failedGrade.Id, widget.Id, rawGrade.Id);
        var rework = await CreateProcess($"RWK-{pfx}", "Rework");
        await AddProcessStep(rework.Id, rwkStep.Id, 1);

        // Workflow
        var workflow = await CreateWorkflow($"WF-{pfx}", "Widget Manufacturing");
        var wpFinishing = await AddWorkflowProcess(workflow.Id, finishing.Id, isEntryPoint: true, sortOrder: 1);
        var wpPackaging = await AddWorkflowProcess(workflow.Id, packaging.Id, sortOrder: 2);
        var wpRework = await AddWorkflowProcess(workflow.Id, rework.Id, sortOrder: 3);

        // Links
        var linkToPackaging = await AddWorkflowLink(workflow.Id, wpFinishing.Id, wpPackaging.Id,
            RoutingType.GradeBased, "Passed → Packaging", new List<Guid> { passedGrade.Id });
        var linkToRework = await AddWorkflowLink(workflow.Id, wpFinishing.Id, wpRework.Id,
            RoutingType.GradeBased, "Failed → Rework", new List<Guid> { failedGrade.Id });
        var linkBackToFinishing = await AddWorkflowLink(workflow.Id, wpRework.Id, wpFinishing.Id,
            RoutingType.Always, "Return to Finishing");

        return new WorkflowScenario
        {
            WidgetKind = widget,
            RawGrade = rawGrade,
            PassedGrade = passedGrade,
            FailedGrade = failedGrade,
            FinishingProcess = finishing,
            PackagingProcess = packaging,
            ReworkProcess = rework,
            Workflow = workflow,
            WpFinishing = wpFinishing,
            WpPackaging = wpPackaging,
            WpRework = wpRework,
            LinkToPackaging = linkToPackaging,
            LinkToRework = linkToRework,
            LinkBackToFinishing = linkBackToFinishing
        };
    }

    // ───── Workflow CRUD ─────

    [Fact]
    public async Task Create_Workflow_ReturnsCreated()
    {
        var wf = await CreateWorkflow("WF-TEST", "Test Workflow");

        Assert.Equal("WF-TEST", wf.Code);
        Assert.Equal("Test Workflow", wf.Name);
        Assert.True(wf.IsActive);
        Assert.Equal(1, wf.Version);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await CreateWorkflow("WF-DUP", "First");

        var dto = new CreateWorkflowDto("WF-DUP", "Second");
        var response = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsWorkflowWithProcessesAndLinks()
    {
        var scenario = await BuildWorkflowScenario();

        var wf = await Client.GetFromJsonAsync<WorkflowResponseDto>(
            $"/api/workflows/{scenario.Workflow.Id}", JsonOptions);

        Assert.NotNull(wf);
        Assert.Equal(3, wf!.Processes!.Count);
        Assert.Equal(3, wf.Links!.Count);
    }

    [Fact]
    public async Task GetAll_WithActiveFilter_ReturnsFiltered()
    {
        var wf1 = await CreateWorkflow();
        var wf2 = await CreateWorkflow();

        // Deactivate wf2
        await Client.PutAsJsonAsync($"/api/workflows/{wf2.Id}",
            new UpdateWorkflowDto(IsActive: false), JsonOptions);

        var active = await Client.GetFromJsonAsync<PaginatedResponse<WorkflowResponseDto>>(
            "/api/workflows?active=true", JsonOptions);

        Assert.Contains(active!.Items, w => w.Id == wf1.Id);
        Assert.DoesNotContain(active!.Items, w => w.Id == wf2.Id);
    }

    [Fact]
    public async Task Update_Workflow_ChangesMetadata()
    {
        var wf = await CreateWorkflow();

        var response = await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}",
            new UpdateWorkflowDto("Updated Name", "New desc", false), JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("New desc", updated.Description);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Delete_Workflow_ReturnsNoContent()
    {
        var wf = await CreateWorkflow();

        var response = await Client.DeleteAsync($"/api/workflows/{wf.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await Client.GetAsync($"/api/workflows/{wf.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    // ───── WorkflowProcess Management ─────

    [Fact]
    public async Task AddProcess_ToWorkflow_Succeeds()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();

        var wp = await AddWorkflowProcess(wf.Id, scenario.Process.Id, isEntryPoint: true);

        Assert.Equal(scenario.Process.Id, wp.ProcessId);
        Assert.True(wp.IsEntryPoint);
        Assert.Equal(scenario.Process.Name, wp.ProcessName);
    }

    [Fact]
    public async Task AddProcess_DuplicateInWorkflow_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        await AddWorkflowProcess(wf.Id, scenario.Process.Id);

        var dto = new AddWorkflowProcessDto(scenario.Process.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/processes", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddProcess_InvalidProcess_ReturnsBadRequest()
    {
        var wf = await CreateWorkflow();

        var dto = new AddWorkflowProcessDto(Guid.NewGuid());
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/processes", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_ChangesEntryPoint()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp = await AddWorkflowProcess(wf.Id, scenario.Process.Id, isEntryPoint: false);

        var response = await Client.PutAsJsonAsync(
            $"/api/workflows/{wf.Id}/processes/{wp.Id}",
            new UpdateWorkflowProcessDto(IsEntryPoint: true), JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions);
        Assert.True(updated!.IsEntryPoint);
    }

    [Fact]
    public async Task RemoveProcess_WithLinks_ReturnsConflict()
    {
        var scenario = await BuildWorkflowScenario();

        var response = await Client.DeleteAsync(
            $"/api/workflows/{scenario.Workflow.Id}/processes/{scenario.WpFinishing.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveProcess_NoLinks_Succeeds()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp = await AddWorkflowProcess(wf.Id, scenario.Process.Id);

        var response = await Client.DeleteAsync(
            $"/api/workflows/{wf.Id}/processes/{wp.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ───── WorkflowLink Management ─────

    [Fact]
    public async Task CreateLink_Always_Succeeds()
    {
        var s1 = await BuildWidgetFinishingScenario();
        var s2 = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp1 = await AddWorkflowProcess(wf.Id, s1.Process.Id, isEntryPoint: true);
        var wp2 = await AddWorkflowProcess(wf.Id, s2.Process.Id);

        var link = await AddWorkflowLink(wf.Id, wp1.Id, wp2.Id);

        Assert.Equal(RoutingType.Always, link.RoutingType);
        Assert.Equal(wp1.Id, link.SourceWorkflowProcessId);
        Assert.Equal(wp2.Id, link.TargetWorkflowProcessId);
    }

    [Fact]
    public async Task CreateLink_GradeBased_WithConditions_Succeeds()
    {
        var scenario = await BuildWorkflowScenario();

        // The linkToPackaging was already created as GradeBased with Passed grade
        Assert.Equal(RoutingType.GradeBased, scenario.LinkToPackaging.RoutingType);
        Assert.Single(scenario.LinkToPackaging.Conditions!);
        Assert.Equal(scenario.PassedGrade.Id, scenario.LinkToPackaging.Conditions![0].GradeId);
    }

    [Fact]
    public async Task CreateLink_GradeBased_WithoutConditions_ReturnsBadRequest()
    {
        var s1 = await BuildWidgetFinishingScenario();
        var s2 = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp1 = await AddWorkflowProcess(wf.Id, s1.Process.Id, isEntryPoint: true);
        var wp2 = await AddWorkflowProcess(wf.Id, s2.Process.Id);

        var dto = new CreateWorkflowLinkDto(wp1.Id, wp2.Id, RoutingType.GradeBased);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/links", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_SelfLoop_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp = await AddWorkflowProcess(wf.Id, scenario.Process.Id, isEntryPoint: true);

        var dto = new CreateWorkflowLinkDto(wp.Id, wp.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/links", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_Duplicate_ReturnsConflict()
    {
        var s1 = await BuildWidgetFinishingScenario();
        var s2 = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        var wp1 = await AddWorkflowProcess(wf.Id, s1.Process.Id, isEntryPoint: true);
        var wp2 = await AddWorkflowProcess(wf.Id, s2.Process.Id);
        await AddWorkflowLink(wf.Id, wp1.Id, wp2.Id);

        var dto = new CreateWorkflowLinkDto(wp1.Id, wp2.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/links", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLink_ChangesNameAndSortOrder()
    {
        var scenario = await BuildWorkflowScenario();

        var response = await Client.PutAsJsonAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkBackToFinishing.Id}",
            new UpdateWorkflowLinkDto("Rework Loop", 5), JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(JsonOptions);
        Assert.Equal("Rework Loop", updated!.Name);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public async Task DeleteLink_Succeeds()
    {
        var scenario = await BuildWorkflowScenario();

        var response = await Client.DeleteAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkBackToFinishing.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify link count decreased
        var links = await Client.GetFromJsonAsync<List<WorkflowLinkResponseDto>>(
            $"/api/workflows/{scenario.Workflow.Id}/links", JsonOptions);
        Assert.Equal(2, links!.Count);
    }

    // ───── Link Conditions ─────

    [Fact]
    public async Task AddCondition_ToGradeBasedLink_Succeeds()
    {
        var scenario = await BuildWorkflowScenario();

        // Add FailedGrade as an additional condition on the packaging link
        var dto = new AddWorkflowLinkConditionDto(scenario.FailedGrade.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkToPackaging.Id}/conditions",
            dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var cond = await response.Content.ReadFromJsonAsync<WorkflowLinkConditionResponseDto>(JsonOptions);
        Assert.Equal(scenario.FailedGrade.Id, cond!.GradeId);
    }

    [Fact]
    public async Task AddCondition_ToAlwaysLink_ReturnsBadRequest()
    {
        var scenario = await BuildWorkflowScenario();

        var dto = new AddWorkflowLinkConditionDto(scenario.RawGrade.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkBackToFinishing.Id}/conditions",
            dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCondition_DuplicateGrade_ReturnsConflict()
    {
        var scenario = await BuildWorkflowScenario();

        // PassedGrade is already a condition on linkToPackaging
        var dto = new AddWorkflowLinkConditionDto(scenario.PassedGrade.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkToPackaging.Id}/conditions",
            dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCondition_Succeeds()
    {
        var scenario = await BuildWorkflowScenario();
        var condId = scenario.LinkToPackaging.Conditions![0].Id;

        var response = await Client.DeleteAsync(
            $"/api/workflows/{scenario.Workflow.Id}/links/{scenario.LinkToPackaging.Id}/conditions/{condId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ───── Validate ─────

    [Fact]
    public async Task Validate_CompleteWorkflow_IsValid()
    {
        var scenario = await BuildWorkflowScenario();

        var response = await Client.PostAsync(
            $"/api/workflows/{scenario.Workflow.Id}/validate", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions);
        Assert.True(result!.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_NoEntryPoint_ReturnsError()
    {
        var s1 = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        // Add process but NOT as entry point
        await AddWorkflowProcess(wf.Id, s1.Process.Id, isEntryPoint: false);

        var response = await Client.PostAsync($"/api/workflows/{wf.Id}/validate", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions);
        Assert.False(result!.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("entry point"));
    }

    [Fact]
    public async Task Validate_UnreachableNode_ReturnsWarning()
    {
        var s1 = await BuildWidgetFinishingScenario();
        var s2 = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow();
        await AddWorkflowProcess(wf.Id, s1.Process.Id, isEntryPoint: true);
        // s2 is not entry point and has no incoming links → unreachable
        await AddWorkflowProcess(wf.Id, s2.Process.Id, isEntryPoint: false);

        var response = await Client.PostAsync($"/api/workflows/{wf.Id}/validate", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions);
        Assert.NotEmpty(result!.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("unreachable"));
    }

    [Fact]
    public async Task Update_Workflow_IncrementsVersion()
    {
        var wf = await CreateWorkflow("WF-VER", "Version Test");
        Assert.Equal(1, wf.Version);

        var updateDto = new UpdateWorkflowDto(Name: "Updated Workflow");
        var response = await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
        Assert.Equal("Updated Workflow", updated.Name);
    }
}

// ──────────── Scenario class ────────────

public class WorkflowScenario
{
    public KindResponseDto WidgetKind { get; set; } = null!;
    public GradeResponseDto RawGrade { get; set; } = null!;
    public GradeResponseDto PassedGrade { get; set; } = null!;
    public GradeResponseDto FailedGrade { get; set; } = null!;
    public ProcessResponseDto FinishingProcess { get; set; } = null!;
    public ProcessResponseDto PackagingProcess { get; set; } = null!;
    public ProcessResponseDto ReworkProcess { get; set; } = null!;
    public WorkflowResponseDto Workflow { get; set; } = null!;
    public WorkflowProcessResponseDto WpFinishing { get; set; } = null!;
    public WorkflowProcessResponseDto WpPackaging { get; set; } = null!;
    public WorkflowProcessResponseDto WpRework { get; set; } = null!;
    public WorkflowLinkResponseDto LinkToPackaging { get; set; } = null!;
    public WorkflowLinkResponseDto LinkToRework { get; set; } = null!;
    public WorkflowLinkResponseDto LinkBackToFinishing { get; set; } = null!;
}
