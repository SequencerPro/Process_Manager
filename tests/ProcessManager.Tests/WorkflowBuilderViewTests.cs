using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Integration tests validating the API data contracts that the
/// Workflow Builder Slide and Document views depend on.
/// </summary>
public class WorkflowBuilderViewTests : IntegrationTestBase
{
    public WorkflowBuilderViewTests(TestWebApplicationFactory factory) : base(factory) { }

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

    // ──────── Tests ────────

    [Fact]
    public async Task Process_detail_loads_steps_and_ports_for_slide_view()
    {
        // Arrange: create a process with steps that have ports
        var pfx = Guid.NewGuid().ToString()[..6];
        var kind = await CreateKind($"K-{pfx}", "SlideKind", isSerialized: true);
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);

        var step = await CreateTransformStep($"ST-{pfx}", "Transform Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"P-{pfx}", "Slide Test Process");
        await AddProcessStep(process.Id, step.Id, 1);

        // Act: fetch process detail (same call the Slide view makes)
        var detail = await Client.GetFromJsonAsync<ProcessResponseDto>(
            $"/api/processes/{process.Id}", JsonOptions);

        // Assert: the response has the step with port information accessible
        Assert.NotNull(detail);
        Assert.Single(detail!.Steps);
        Assert.Equal(1, detail.Steps[0].Sequence);
        Assert.Equal("Transform Step", detail.Steps[0].StepTemplateName);
        Assert.Equal(step.Id, detail.Steps[0].StepTemplateId);

        // Also verify the step template has ports
        var template = await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{step.Id}", JsonOptions);
        Assert.NotNull(template);
        Assert.Equal(2, template!.Ports.Count); // 1 input + 1 output
        Assert.Contains(template.Ports, p => p.Direction == PortDirection.Input);
        Assert.Contains(template.Ports, p => p.Direction == PortDirection.Output);
    }

    [Fact]
    public async Task Update_sort_order_reorders_processes()
    {
        // Arrange: create workflow with two processes at sort orders 1 and 2
        var pfx = Guid.NewGuid().ToString()[..6];
        var procA = await CreateProcess($"PA-{pfx}", "Process A");
        var procB = await CreateProcess($"PB-{pfx}", "Process B");
        var workflow = await CreateWorkflow($"WF-{pfx}", "Sort Order Test");
        var wpA = await AddWorkflowProcess(workflow.Id, procA.Id, sortOrder: 1);
        var wpB = await AddWorkflowProcess(workflow.Id, procB.Id, sortOrder: 2);

        // Act: swap sort orders (simulating MoveSlideProcess)
        var respA = await Client.PutAsJsonAsync(
            $"/api/workflows/{workflow.Id}/processes/{wpA.Id}",
            new UpdateWorkflowProcessDto(SortOrder: 2), JsonOptions);
        respA.EnsureSuccessStatusCode();

        var respB = await Client.PutAsJsonAsync(
            $"/api/workflows/{workflow.Id}/processes/{wpB.Id}",
            new UpdateWorkflowProcessDto(SortOrder: 1), JsonOptions);
        respB.EnsureSuccessStatusCode();

        // Assert: reload workflow and verify new order
        var updated = await Client.GetFromJsonAsync<WorkflowResponseDto>(
            $"/api/workflows/{workflow.Id}", JsonOptions);

        Assert.NotNull(updated);
        var orderedProcesses = updated!.Processes!.OrderBy(p => p.SortOrder).ToList();
        Assert.Equal(wpB.Id, orderedProcesses[0].Id); // B is now first (sort order 1)
        Assert.Equal(wpA.Id, orderedProcesses[1].Id); // A is now second (sort order 2)
    }

    [Fact]
    public async Task Workflow_response_includes_all_data_for_document_view()
    {
        // Arrange: build a full workflow scenario
        var pfx = Guid.NewGuid().ToString()[..6];

        var kind = await CreateKind($"K-{pfx}", "DocKind", isSerialized: true);
        var rawGrade = await CreateGrade(kind.Id, "RAW", "Raw", isDefault: true);
        var passedGrade = await CreateGrade(kind.Id, "PASS", "Passed");

        var step1 = await CreateTransformStep($"S1-{pfx}", "Step One",
            kind.Id, rawGrade.Id, kind.Id, rawGrade.Id);
        var step2 = await CreateTransformStep($"S2-{pfx}", "Step Two",
            kind.Id, rawGrade.Id, kind.Id, passedGrade.Id);

        var processA = await CreateProcess($"PA-{pfx}", "Process Alpha");
        await AddProcessStep(processA.Id, step1.Id, 1);

        var processB = await CreateProcess($"PB-{pfx}", "Process Beta");
        await AddProcessStep(processB.Id, step2.Id, 1);

        var workflow = await CreateWorkflow($"WF-{pfx}", "Document View Test");
        var wpA = await AddWorkflowProcess(workflow.Id, processA.Id, isEntryPoint: true, sortOrder: 1);
        var wpB = await AddWorkflowProcess(workflow.Id, processB.Id, sortOrder: 2);

        await AddWorkflowLink(workflow.Id, wpA.Id, wpB.Id,
            RoutingType.GradeBased, "Grade Link", new List<Guid> { passedGrade.Id });

        // Act: fetch workflow (same call the Document view depends on)
        var wf = await Client.GetFromJsonAsync<WorkflowResponseDto>(
            $"/api/workflows/{workflow.Id}", JsonOptions);

        // Assert: all data needed for document view is present
        Assert.NotNull(wf);
        Assert.Equal(2, wf!.Processes!.Count);
        Assert.Single(wf.Links!);

        // Verify process metadata
        var entryProcs = wf.Processes.Where(p => p.IsEntryPoint).ToList();
        Assert.Single(entryProcs);
        Assert.Equal("Process Alpha", entryProcs[0].ProcessName);
        Assert.Equal($"PA-{pfx}", entryProcs[0].ProcessCode);

        // Verify link data includes routing type and conditions
        var link = wf.Links![0];
        Assert.Equal(RoutingType.GradeBased, link.RoutingType);
        Assert.Equal("Grade Link", link.Name);
        Assert.NotNull(link.Conditions);
        Assert.Single(link.Conditions!);
        Assert.Equal("PASS", link.Conditions![0].GradeCode);

        // Verify source/target names are populated
        Assert.Equal("Process Alpha", link.SourceProcessName);
        Assert.Equal("Process Beta", link.TargetProcessName);
    }

    [Fact]
    public async Task Multiple_process_details_load_in_parallel()
    {
        // Arrange: create multiple processes (simulating EnsureAllProcessDetailsLoaded)
        var pfx = Guid.NewGuid().ToString()[..6];
        var kind = await CreateKind($"K-{pfx}", "ParallelKind", isSerialized: true);
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);

        var processes = new List<ProcessResponseDto>();
        for (int i = 0; i < 3; i++)
        {
            var step = await CreateTransformStep($"S{i}-{pfx}", $"Step {i}",
                kind.Id, grade.Id, kind.Id, grade.Id);
            var proc = await CreateProcess($"P{i}-{pfx}", $"Process {i}");
            await AddProcessStep(proc.Id, step.Id, 1);
            processes.Add(proc);
        }

        // Act: fetch all process details in parallel (same pattern as EnsureAllProcessDetailsLoaded)
        var tasks = processes.Select(p =>
            Client.GetFromJsonAsync<ProcessResponseDto>($"/api/processes/{p.Id}", JsonOptions));
        var results = await Task.WhenAll(tasks);

        // Assert: all details loaded successfully
        Assert.Equal(3, results.Length);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Single(result!.Steps);
        }

        // Verify each process has the correct data
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(processes[i].Id, results[i]!.Id);
            Assert.Equal($"Process {i}", results[i]!.Name);
        }
    }
}
