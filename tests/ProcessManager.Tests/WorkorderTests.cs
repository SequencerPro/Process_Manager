using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class WorkorderTests : IntegrationTestBase
{
    public WorkorderTests(TestWebApplicationFactory factory) : base(factory) { }

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
        Guid workflowId, Guid? processId, bool isEntryPoint = false, bool isTerminalNode = false, int sortOrder = 0)
    {
        var dto = new AddWorkflowProcessDto(processId, isEntryPoint, sortOrder, IsTerminalNode: isTerminalNode);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/processes", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowLinkResponseDto> AddWorkflowLink(
        Guid workflowId, Guid sourceWpId, Guid targetWpId,
        RoutingType routingType = RoutingType.Always, string? name = null)
    {
        var dto = new CreateWorkflowLinkDto(sourceWpId, targetWpId, routingType, name);
        var response = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/links", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(JsonOptions))!;
    }

    private async Task<WorkorderResponseDto> CreateWorkorder(
        Guid workflowId, string? code = null, string name = "Test Workorder", int priority = 0)
    {
        code ??= $"WO-{Guid.NewGuid().ToString()[..6]}";
        var dto = new CreateWorkorderDto(code, name, null, workflowId, priority);
        var response = await Client.PostAsJsonAsync("/api/workorders", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkorderResponseDto>(JsonOptions))!;
    }

    private async Task<WorkorderResponseDto> GetWorkorder(Guid id)
    {
        return (await Client.GetFromJsonAsync<WorkorderResponseDto>(
            $"/api/workorders/{id}", JsonOptions))!;
    }

    /// <summary>
    /// Builds a linear workflow: ProcessA (entry) → ProcessB → End
    /// Both processes are released and ready for jobs.
    /// </summary>
    private async Task<LinearWorkflowScenario> BuildLinearWorkflowScenario()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Kind + Grade
        var kind = await CreateKind($"K-{pfx}", "Part");
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);

        // Step template (simple transform)
        var step = await CreateTransformStep($"ST-{pfx}", "Work",
            kind.Id, grade.Id, kind.Id, grade.Id);

        // Process A
        var procA = await CreateProcess($"PA-{pfx}", "Process A");
        await AddProcessStep(procA.Id, step.Id, 1);
        await ReleaseProcess(procA.Id);

        // Process B — need a different step template due to unique code constraint
        var step2 = await CreateTransformStep($"ST2-{pfx}", "Work 2",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var procB = await CreateProcess($"PB-{pfx}", "Process B");
        await AddProcessStep(procB.Id, step2.Id, 1);
        await ReleaseProcess(procB.Id);

        // Workflow: A (entry) → B → End
        var wf = await CreateWorkflow($"WF-{pfx}", "Linear Flow");
        var wpA = await AddWorkflowProcess(wf.Id, procA.Id, isEntryPoint: true, sortOrder: 1);
        var wpB = await AddWorkflowProcess(wf.Id, procB.Id, sortOrder: 2);
        var wpEnd = await AddWorkflowProcess(wf.Id, null, isTerminalNode: true, sortOrder: 3);
        var linkAB = await AddWorkflowLink(wf.Id, wpA.Id, wpB.Id);
        var linkBEnd = await AddWorkflowLink(wf.Id, wpB.Id, wpEnd.Id);

        return new LinearWorkflowScenario
        {
            ProcessA = procA,
            ProcessB = procB,
            Workflow = wf,
            WpA = wpA,
            WpB = wpB,
            WpEnd = wpEnd,
            LinkAB = linkAB,
            LinkBEnd = linkBEnd
        };
    }

    /// <summary>
    /// Builds a diamond/merge workflow: A (entry) and B (entry) → C → End
    /// Both entry points must complete before C can start.
    /// </summary>
    private async Task<MergeWorkflowScenario> BuildMergeWorkflowScenario()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        var kind = await CreateKind($"K-{pfx}", "Part");
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);

        var stepA = await CreateTransformStep($"SA-{pfx}", "Step A",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep($"SB-{pfx}", "Step B",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var stepC = await CreateTransformStep($"SC-{pfx}", "Step C",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var procA = await CreateProcess($"PA-{pfx}", "Process A");
        await AddProcessStep(procA.Id, stepA.Id, 1);
        await ReleaseProcess(procA.Id);

        var procB = await CreateProcess($"PB-{pfx}", "Process B");
        await AddProcessStep(procB.Id, stepB.Id, 1);
        await ReleaseProcess(procB.Id);

        var procC = await CreateProcess($"PC-{pfx}", "Process C");
        await AddProcessStep(procC.Id, stepC.Id, 1);
        await ReleaseProcess(procC.Id);

        // Workflow: A (entry) ─┐
        //                      ├──→ C → End
        // B (entry) ───────────┘
        var wf = await CreateWorkflow($"WF-{pfx}", "Merge Flow");
        var wpA = await AddWorkflowProcess(wf.Id, procA.Id, isEntryPoint: true, sortOrder: 1);
        var wpB = await AddWorkflowProcess(wf.Id, procB.Id, isEntryPoint: true, sortOrder: 2);
        var wpC = await AddWorkflowProcess(wf.Id, procC.Id, sortOrder: 3);
        var wpEnd = await AddWorkflowProcess(wf.Id, null, isTerminalNode: true, sortOrder: 4);
        await AddWorkflowLink(wf.Id, wpA.Id, wpC.Id);
        await AddWorkflowLink(wf.Id, wpB.Id, wpC.Id);
        await AddWorkflowLink(wf.Id, wpC.Id, wpEnd.Id);

        return new MergeWorkflowScenario
        {
            ProcessA = procA,
            ProcessB = procB,
            ProcessC = procC,
            Workflow = wf,
            WpA = wpA,
            WpB = wpB,
            WpC = wpC,
            WpEnd = wpEnd
        };
    }

    private async Task<JobResponseDto> StartJob(Guid jobId)
    {
        var resp = await Client.PostAsync($"/api/jobs/{jobId}/start", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
    }

    private async Task<JobResponseDto> CompleteJobStepsAndJob(Guid jobId)
    {
        // Get the job with step executions
        var job = (await Client.GetFromJsonAsync<JobResponseDto>($"/api/jobs/{jobId}", JsonOptions))!;

        // Complete all step executions
        foreach (var se in job.StepExecutions!.OrderBy(s => s.Sequence))
        {
            await Client.PostAsync($"/api/step-executions/{se.Id}/start", null);
            await Client.PostAsync($"/api/step-executions/{se.Id}/complete", null);
        }

        // Complete the job
        var resp = await Client.PostAsync($"/api/jobs/{jobId}/complete", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
    }

    // ──────── Tests ────────

    [Fact]
    public async Task Create_Workorder_CreatesEntryPointJobs()
    {
        var scenario = await BuildMergeWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Test WO");
        var detail = await GetWorkorder(wo.Id);

        Assert.Equal("Created", detail.Status);
        Assert.NotNull(detail.Jobs);
        Assert.Equal(2, detail.Jobs.Count); // Two entry points → two jobs
        Assert.All(detail.Jobs, j => Assert.Equal("Created", j.JobStatus));
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var code = $"WO-DUP-{Guid.NewGuid().ToString()[..6]}";
        await CreateWorkorder(scenario.Workflow.Id, code: code);

        var dto = new CreateWorkorderDto(code, "Second", null, scenario.Workflow.Id);
        var response = await Client.PostAsJsonAsync("/api/workorders", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveWorkflow_ReturnsBadRequest()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var wf = await CreateWorkflow($"WF-{pfx}", "Inactive WF");

        // Deactivate workflow
        var updateDto = new UpdateWorkflowDto(IsActive: false);
        await Client.PutAsJsonAsync($"/api/workflows/{wf.Id}", updateDto, JsonOptions);

        var dto = new CreateWorkorderDto($"WO-{pfx}", "Test", null, wf.Id);
        var response = await Client.PostAsJsonAsync("/api/workorders", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Start_Workorder_StartsEntryPointJobs()
    {
        var scenario = await BuildMergeWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Start Test");

        // Start the workorder
        var resp = await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        resp.EnsureSuccessStatusCode();

        var detail = await GetWorkorder(wo.Id);
        Assert.Equal("InProgress", detail.Status);
        Assert.NotNull(detail.StartedAt);
        Assert.All(detail.Jobs!, j => Assert.Equal("InProgress", j.JobStatus));
    }

    [Fact]
    public async Task Start_Job_BlockedByPredecessor_ReturnsBadRequest()
    {
        // Use merge workflow: A (entry) and B (entry) → C → End
        var scenario = await BuildMergeWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Block Test");

        // Start workorder — both entry-point jobs start
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        var detail = await GetWorkorder(wo.Id);

        // Complete only Process A
        var jobA = detail.Jobs!.First(j => j.ProcessName == "Process A");
        await CompleteJobStepsAndJob(jobA.JobId);

        // Process C should NOT be created yet (B is still in progress)
        detail = await GetWorkorder(wo.Id);
        Assert.DoesNotContain(detail.Jobs!, j => j.ProcessName == "Process C");

        // Now complete Process B → Process C gets auto-created
        var jobB = detail.Jobs!.First(j => j.ProcessName == "Process B");
        await CompleteJobStepsAndJob(jobB.JobId);

        // Reload — Process C job should exist and be startable
        detail = await GetWorkorder(wo.Id);
        var jobC = detail.Jobs!.FirstOrDefault(j => j.ProcessName == "Process C");
        Assert.NotNull(jobC);
        Assert.True(jobC.CanStart);
    }

    [Fact]
    public async Task AutoCreatedJob_CanStartAfterPredecessorComplete()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "CanStart After Complete");

        // Start workorder
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        var detail = await GetWorkorder(wo.Id);

        // Complete Process A → triggers auto-creation of Process B job
        var jobA = detail.Jobs!.First();
        await CompleteJobStepsAndJob(jobA.JobId);

        // Reload — Process B should be startable
        detail = await GetWorkorder(wo.Id);
        var jobB = detail.Jobs!.First(j => j.ProcessName == "Process B");
        Assert.True(jobB.CanStart);

        // Actually start it — should succeed
        var started = await StartJob(jobB.JobId);
        Assert.Equal("InProgress", started.Status);
    }

    [Fact]
    public async Task Complete_EntryPointJob_AutoCreatesNextJobs()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "AutoCreate Test");

        // Start workorder
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        var detail = await GetWorkorder(wo.Id);

        // Should only have 1 job (entry point)
        Assert.Single(detail.Jobs!);

        // Complete the entry point job
        var jobA = detail.Jobs!.First();
        await CompleteJobStepsAndJob(jobA.JobId);

        // Reload — Process B job should be auto-created
        detail = await GetWorkorder(wo.Id);
        Assert.Equal(2, detail.Jobs!.Count);
        var jobB = detail.Jobs!.First(j => j.ProcessName == "Process B");
        Assert.Equal("Created", jobB.JobStatus);
    }

    [Fact]
    public async Task MergePoint_AllPredecessorsMustComplete()
    {
        var scenario = await BuildMergeWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Merge Test");

        // Start workorder
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        var detail = await GetWorkorder(wo.Id);

        // Both entry-point jobs exist
        Assert.Equal(2, detail.Jobs!.Count);

        var jobA = detail.Jobs!.First(j => j.ProcessName == "Process A");
        var jobB = detail.Jobs!.First(j => j.ProcessName == "Process B");

        // Complete only Process A
        await CompleteJobStepsAndJob(jobA.JobId);

        // Reload — Process C should NOT be created yet (B is not complete)
        detail = await GetWorkorder(wo.Id);
        Assert.Equal(2, detail.Jobs!.Count); // Still only 2 jobs
        Assert.DoesNotContain(detail.Jobs!, j => j.ProcessName == "Process C");

        // Now complete Process B
        await CompleteJobStepsAndJob(jobB.JobId);

        // Reload — Process C should now be auto-created
        detail = await GetWorkorder(wo.Id);
        Assert.Equal(3, detail.Jobs!.Count);
        var jobC = detail.Jobs!.First(j => j.ProcessName == "Process C");
        Assert.Equal("Created", jobC.JobStatus);
    }

    [Fact]
    public async Task Complete_AllTerminalJobs_CompletesWorkorder()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Completion Test");

        // Start and complete entire chain: A → B → End
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);
        var detail = await GetWorkorder(wo.Id);

        // Complete A → triggers auto-creation of B
        await CompleteJobStepsAndJob(detail.Jobs!.First().JobId);

        // Complete B → triggers workorder completion check
        detail = await GetWorkorder(wo.Id);
        var jobB = detail.Jobs!.First(j => j.ProcessName == "Process B");
        await StartJob(jobB.JobId);
        await CompleteJobStepsAndJob(jobB.JobId);

        // Workorder should now be Completed
        detail = await GetWorkorder(wo.Id);
        Assert.Equal("Completed", detail.Status);
        Assert.NotNull(detail.CompletedAt);
    }

    [Fact]
    public async Task Cancel_Workorder_CancelsAllJobs()
    {
        var scenario = await BuildMergeWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Cancel Test");
        await Client.PostAsync($"/api/workorders/{wo.Id}/start", null);

        // Cancel workorder
        var resp = await Client.PostAsync($"/api/workorders/{wo.Id}/cancel", null);
        resp.EnsureSuccessStatusCode();

        var detail = await GetWorkorder(wo.Id);
        Assert.Equal("Cancelled", detail.Status);
        Assert.All(detail.Jobs!, j => Assert.Equal("Cancelled", j.JobStatus));
    }

    [Fact]
    public async Task GetById_ReturnsWorkorderWithJobs_CanStart()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "CanStart Test");
        var detail = await GetWorkorder(wo.Id);

        Assert.NotNull(detail.Jobs);
        Assert.Single(detail.Jobs);

        // Entry point job should have CanStart = true (it's in Created status and is an entry point)
        var job = detail.Jobs.First();
        Assert.True(job.CanStart);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFiltered()
    {
        var scenario = await BuildLinearWorkflowScenario();

        var wo = await CreateWorkorder(scenario.Workflow.Id, name: "Filter Test");

        // Filter for Created — should find our workorder
        var resp = await Client.GetFromJsonAsync<PaginatedResponse<WorkorderResponseDto>>(
            "/api/workorders?status=Created", JsonOptions);
        Assert.NotNull(resp);
        Assert.Contains(resp.Items, w => w.Id == wo.Id);

        // Filter for Completed — should NOT find it
        resp = await Client.GetFromJsonAsync<PaginatedResponse<WorkorderResponseDto>>(
            "/api/workorders?status=Completed", JsonOptions);
        Assert.NotNull(resp);
        Assert.DoesNotContain(resp.Items, w => w.Id == wo.Id);
    }
}

// ──────── Scenario Records ────────

public class LinearWorkflowScenario
{
    public ProcessResponseDto ProcessA { get; set; } = null!;
    public ProcessResponseDto ProcessB { get; set; } = null!;
    public WorkflowResponseDto Workflow { get; set; } = null!;
    public WorkflowProcessResponseDto WpA { get; set; } = null!;
    public WorkflowProcessResponseDto WpB { get; set; } = null!;
    public WorkflowProcessResponseDto WpEnd { get; set; } = null!;
    public WorkflowLinkResponseDto LinkAB { get; set; } = null!;
    public WorkflowLinkResponseDto LinkBEnd { get; set; } = null!;
}

public class MergeWorkflowScenario
{
    public ProcessResponseDto ProcessA { get; set; } = null!;
    public ProcessResponseDto ProcessB { get; set; } = null!;
    public ProcessResponseDto ProcessC { get; set; } = null!;
    public WorkflowResponseDto Workflow { get; set; } = null!;
    public WorkflowProcessResponseDto WpA { get; set; } = null!;
    public WorkflowProcessResponseDto WpB { get; set; } = null!;
    public WorkflowProcessResponseDto WpC { get; set; } = null!;
    public WorkflowProcessResponseDto WpEnd { get; set; } = null!;
}
