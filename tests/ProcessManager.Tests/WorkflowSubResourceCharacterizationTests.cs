using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Characterization tests for the WorkflowProcess, WorkflowLink and link-condition
/// sub-resources of <c>/api/workflows</c>: unknown parents, ids that belong to a
/// different Workflow, partial updates, ordering, and the Validate rule for a
/// GradeBased link whose last condition was removed. They lock in current behavior;
/// no production code was changed when they were added.
/// </summary>
public class WorkflowSubResourceCharacterizationTests : IntegrationTestBase
{
    public WorkflowSubResourceCharacterizationTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────── Helpers ────────

    private static string Pfx() => Guid.NewGuid().ToString("N")[..6];

    private async Task<WorkflowResponseDto> CreateWorkflow()
    {
        var response = await Client.PostAsJsonAsync("/api/workflows",
            new CreateWorkflowDto($"WF-{Pfx()}", "Characterization Workflow"), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowProcessResponseDto> AddWorkflowProcess(
        Guid workflowId, bool isEntryPoint = false, int sortOrder = 0)
    {
        var process = await CreateProcess($"P-{Pfx()}", "Characterization Process");
        var response = await Client.PostAsJsonAsync($"/api/workflows/{workflowId}/processes",
            new AddWorkflowProcessDto(process.Id, isEntryPoint, sortOrder), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowLinkResponseDto> AddLink(
        Guid workflowId, Guid sourceWpId, Guid targetWpId,
        RoutingType routingType = RoutingType.Always, int sortOrder = 0,
        string? name = null, List<Guid>? conditionGradeIds = null)
    {
        var response = await Client.PostAsJsonAsync($"/api/workflows/{workflowId}/links",
            new CreateWorkflowLinkDto(sourceWpId, targetWpId, routingType, name, sortOrder, conditionGradeIds),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(JsonOptions))!;
    }

    private async Task<List<WorkflowProcessResponseDto>> GetProcesses(Guid workflowId) =>
        (await Client.GetFromJsonAsync<List<WorkflowProcessResponseDto>>(
            $"/api/workflows/{workflowId}/processes", JsonOptions))!;

    private async Task<List<WorkflowLinkResponseDto>> GetLinks(Guid workflowId) =>
        (await Client.GetFromJsonAsync<List<WorkflowLinkResponseDto>>(
            $"/api/workflows/{workflowId}/links", JsonOptions))!;

    /// <summary>A Workflow with an entry process A, a process B, and a GradeBased link A → B.</summary>
    private async Task<(WorkflowResponseDto Wf, WorkflowProcessResponseDto A, WorkflowProcessResponseDto B,
        WorkflowLinkResponseDto Link, GradeResponseDto Grade)> BuildLinkedWorkflow()
    {
        var (_, grade) = await CreateKindWithGrade($"K-{Pfx()}", "Characterization Kind");
        var wf = await CreateWorkflow();
        var a = await AddWorkflowProcess(wf.Id, isEntryPoint: true, sortOrder: 1);
        var b = await AddWorkflowProcess(wf.Id, sortOrder: 2);
        var link = await AddLink(wf.Id, a.Id, b.Id, RoutingType.GradeBased,
            name: "A to B", conditionGradeIds: new List<Guid> { grade.Id });
        return (wf, a, b, link, grade);
    }

    // ───── Unknown Workflow ─────

    [Theory]
    [InlineData("processes")]
    [InlineData("links")]
    public async Task GetChildren_UnknownWorkflow_ReturnsNotFound(string child)
    {
        var response = await Client.GetAsync($"/api/workflows/{Guid.NewGuid()}/{child}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProcess_UnknownWorkflow_ReturnsNotFound()
    {
        var process = await CreateProcess($"P-{Pfx()}", "Orphan");

        var response = await Client.PostAsJsonAsync($"/api/workflows/{Guid.NewGuid()}/processes",
            new AddWorkflowProcessDto(process.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Validate_UnknownWorkflow_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/api/workflows/{Guid.NewGuid()}/validate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── WorkflowProcess ─────

    [Fact]
    public async Task AddProcess_InactiveProcess_ReturnsBadRequest()
    {
        var wf = await CreateWorkflow();
        var process = await CreateProcess($"P-{Pfx()}", "Retired");
        (await Client.PutAsJsonAsync($"/api/processes/{process.Id}",
            new ProcessUpdateDto(process.Name, null, IsActive: false), JsonOptions)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/processes",
            new AddWorkflowProcessDto(process.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not active", await response.Content.ReadAsStringAsync());
        Assert.Empty(await GetProcesses(wf.Id));
    }

    [Fact]
    public async Task GetProcesses_AreOrderedBySortOrder()
    {
        var wf = await CreateWorkflow();
        var third = await AddWorkflowProcess(wf.Id, sortOrder: 3);
        var first = await AddWorkflowProcess(wf.Id, isEntryPoint: true, sortOrder: 1);
        var second = await AddWorkflowProcess(wf.Id, sortOrder: 2);

        var ids = (await GetProcesses(wf.Id)).Select(wp => wp.Id).ToList();

        Assert.Equal(new[] { first.Id, second.Id, third.Id }, ids);
    }

    [Fact]
    public async Task UpdateProcess_NullFields_LeaveValuesUnchanged()
    {
        var wf = await CreateWorkflow();
        var wp = await AddWorkflowProcess(wf.Id, isEntryPoint: true, sortOrder: 7);

        var response = await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}/processes/{wp.Id}",
            new UpdateWorkflowProcessDto(), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;

        Assert.True(updated.IsEntryPoint);
        Assert.Equal(7, updated.SortOrder);
        Assert.Equal(wp.ProcessName, updated.ProcessName);
    }

    [Fact]
    public async Task UpdateProcess_ThroughAnotherWorkflow_ReturnsNotFound_AndLeavesItUnchanged()
    {
        var (wf, a, _, _, _) = await BuildLinkedWorkflow();
        var other = await CreateWorkflow();

        var response = await Client.PutAsJsonAsync($"/api/workflows/{other.Id}/processes/{a.Id}",
            new UpdateWorkflowProcessDto(IsEntryPoint: false, SortOrder: 99), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = (await GetProcesses(wf.Id)).Single(wp => wp.Id == a.Id);
        Assert.True(stored.IsEntryPoint);
        Assert.Equal(1, stored.SortOrder);
    }

    [Fact]
    public async Task RemoveProcess_ThroughAnotherWorkflow_ReturnsNotFound()
    {
        var wf = await CreateWorkflow();
        var wp = await AddWorkflowProcess(wf.Id);
        var other = await CreateWorkflow();

        var response = await Client.DeleteAsync($"/api/workflows/{other.Id}/processes/{wp.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(await GetProcesses(wf.Id), p => p.Id == wp.Id);
    }

    [Fact]
    public async Task RemoveProcess_ThatIsOnlyALinkTarget_ReturnsConflict()
    {
        var (wf, _, b, _, _) = await BuildLinkedWorkflow();

        var response = await Client.DeleteAsync($"/api/workflows/{wf.Id}/processes/{b.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(await GetProcesses(wf.Id), p => p.Id == b.Id);
    }

    // ───── WorkflowLink ─────

    [Fact]
    public async Task CreateLink_SourceFromAnotherWorkflow_ReturnsBadRequest()
    {
        var wf = await CreateWorkflow();
        var target = await AddWorkflowProcess(wf.Id, isEntryPoint: true);
        var other = await CreateWorkflow();
        var foreignSource = await AddWorkflowProcess(other.Id, isEntryPoint: true);

        var response = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/links",
            new CreateWorkflowLinkDto(foreignSource.Id, target.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Source workflow process not found", await response.Content.ReadAsStringAsync());
        Assert.Empty(await GetLinks(wf.Id));
    }

    [Fact]
    public async Task CreateLink_TargetFromAnotherWorkflow_ReturnsBadRequest()
    {
        var wf = await CreateWorkflow();
        var source = await AddWorkflowProcess(wf.Id, isEntryPoint: true);
        var other = await CreateWorkflow();
        var foreignTarget = await AddWorkflowProcess(other.Id);

        var response = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/links",
            new CreateWorkflowLinkDto(source.Id, foreignTarget.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Target workflow process not found", await response.Content.ReadAsStringAsync());
        Assert.Empty(await GetLinks(wf.Id));
    }

    [Fact]
    public async Task CreateLink_UnknownConditionGrade_ReturnsBadRequest_AndSavesNoLink()
    {
        var wf = await CreateWorkflow();
        var a = await AddWorkflowProcess(wf.Id, isEntryPoint: true);
        var b = await AddWorkflowProcess(wf.Id);
        var missingGrade = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync($"/api/workflows/{wf.Id}/links",
            new CreateWorkflowLinkDto(a.Id, b.Id, RoutingType.GradeBased,
                ConditionGradeIds: new List<Guid> { missingGrade }), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(missingGrade.ToString(), await response.Content.ReadAsStringAsync());
        Assert.Empty(await GetLinks(wf.Id));
    }

    [Fact]
    public async Task GetLinks_AreOrderedBySortOrder_WithProcessNamesAndConditions()
    {
        var (wf, a, b, gradeLink, grade) = await BuildLinkedWorkflow();
        var c = await AddWorkflowProcess(wf.Id, sortOrder: 3);
        var early = await AddLink(wf.Id, b.Id, c.Id, sortOrder: 1);
        (await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}/links/{gradeLink.Id}",
            new UpdateWorkflowLinkDto(SortOrder: 5), JsonOptions)).EnsureSuccessStatusCode();

        var links = await GetLinks(wf.Id);

        Assert.Equal(new[] { early.Id, gradeLink.Id }, links.Select(l => l.Id).ToArray());
        var stored = links[1];
        Assert.Equal(a.ProcessName, stored.SourceProcessName);
        Assert.Equal(b.ProcessName, stored.TargetProcessName);
        var condition = Assert.Single(stored.Conditions!);
        Assert.Equal(grade.Id, condition.GradeId);
        Assert.Equal(grade.Code, condition.GradeCode);
    }

    [Fact]
    public async Task UpdateLink_NullFields_LeaveValuesUnchanged()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();

        var response = await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}/links/{link.Id}",
            new UpdateWorkflowLinkDto(), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(JsonOptions))!;

        Assert.Equal("A to B", updated.Name);
        Assert.Equal(0, updated.SortOrder);
        Assert.Equal(RoutingType.GradeBased, updated.RoutingType);
        Assert.Single(updated.Conditions!);
    }

    [Fact]
    public async Task UpdateLink_ThroughAnotherWorkflow_ReturnsNotFound()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();
        var other = await CreateWorkflow();

        var response = await Client.PutAsJsonAsync($"/api/workflows/{other.Id}/links/{link.Id}",
            new UpdateWorkflowLinkDto(Name: "Hijacked"), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("A to B", (await GetLinks(wf.Id)).Single().Name);
    }

    [Fact]
    public async Task DeleteLink_ThroughAnotherWorkflow_ReturnsNotFound()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();
        var other = await CreateWorkflow();

        var response = await Client.DeleteAsync($"/api/workflows/{other.Id}/links/{link.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(await GetLinks(wf.Id));
    }

    // ───── Link conditions ─────

    [Fact]
    public async Task AddCondition_UnknownGrade_ReturnsBadRequest()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();

        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{wf.Id}/links/{link.Id}/conditions",
            new AddWorkflowLinkConditionDto(Guid.NewGuid()), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single((await GetLinks(wf.Id)).Single().Conditions!);
    }

    [Fact]
    public async Task AddCondition_ThroughAnotherWorkflow_ReturnsNotFound()
    {
        var (_, _, _, link, _) = await BuildLinkedWorkflow();
        var other = await CreateWorkflow();
        var (_, grade) = await CreateKindWithGrade($"K-{Pfx()}", "Other Kind");

        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{other.Id}/links/{link.Id}/conditions",
            new AddWorkflowLinkConditionDto(grade.Id), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCondition_ThroughAnotherWorkflow_ReturnsNotFound_AndKeepsIt()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();
        var other = await CreateWorkflow();
        var conditionId = link.Conditions!.Single().Id;

        var response = await Client.DeleteAsync(
            $"/api/workflows/{other.Id}/links/{link.Id}/conditions/{conditionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single((await GetLinks(wf.Id)).Single().Conditions!);
    }

    [Fact]
    public async Task RemoveCondition_ThroughAnotherLink_ReturnsNotFound()
    {
        var (wf, _, b, link, _) = await BuildLinkedWorkflow();
        var c = await AddWorkflowProcess(wf.Id);
        var otherLink = await AddLink(wf.Id, b.Id, c.Id);
        var conditionId = link.Conditions!.Single().Id;

        var response = await Client.DeleteAsync(
            $"/api/workflows/{wf.Id}/links/{otherLink.Id}/conditions/{conditionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── Validate: Rule 14 ─────

    [Fact]
    public async Task Validate_GradeBasedLinkWhoseLastConditionWasRemoved_ReturnsError()
    {
        var (wf, _, _, link, _) = await BuildLinkedWorkflow();
        var conditionId = link.Conditions!.Single().Id;
        // Removing the last condition is allowed; Validate is what reports it.
        var remove = await Client.DeleteAsync(
            $"/api/workflows/{wf.Id}/links/{link.Id}/conditions/{conditionId}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var response = await Client.PostAsync($"/api/workflows/{wf.Id}/validate", null);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions))!;

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Contains("has no conditions", error);
    }
}
