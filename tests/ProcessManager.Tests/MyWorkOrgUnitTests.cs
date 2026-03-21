using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Integration tests for Phase 12 Step 2: MyWork OrgUnit-based job filtering.
/// Verifies that GET /api/step-executions?myWork=true returns step executions for:
///   - Jobs where the WorkflowProcess.AssigneeId points to an OrgUnit the current user belongs to
///   - Step executions directly assigned to the current user (AssignedToUserId)
/// And excludes jobs/steps not meeting either criterion.
/// </summary>
public class MyWorkOrgUnitTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public MyWorkOrgUnitTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<OrgUnitResponseDto> CreateOrgUnit(string code, string name)
    {
        var dto = new OrgUnitCreateDto(code, name);
        var resp = await Client.PostAsJsonAsync("/api/orgunits", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrgUnitResponseDto>(JsonOptions))!;
    }

    private string InsertUser(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();

        // Skip if user already exists (idempotent)
        if (db.Users.Find(userId) is not null)
            return userId;

        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            NormalizedUserName = userId.ToUpperInvariant(),
            Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        });
        db.SaveChanges();
        return userId;
    }

    private async Task AddMember(Guid orgUnitId, string userId)
    {
        var dto = new OrgUnitMemberAddDto(userId);
        var resp = await Client.PostAsJsonAsync($"/api/orgunits/{orgUnitId}/members", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<WorkflowResponseDto> CreateWorkflow(string code, string name)
    {
        var dto = new CreateWorkflowDto(code, name);
        var resp = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;
    }

    private async Task<WorkflowProcessResponseDto> AddWorkflowProcess(
        Guid workflowId, Guid? processId, bool isEntryPoint = false,
        bool isTerminalNode = false, Guid? assigneeId = null)
    {
        var dto = new AddWorkflowProcessDto(
            ProcessId: processId,
            IsEntryPoint: isEntryPoint,
            IsTerminalNode: isTerminalNode,
            AssigneeId: assigneeId);
        var resp = await Client.PostAsJsonAsync($"/api/workflows/{workflowId}/processes", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;
    }

    private async Task AddWorkflowLink(Guid workflowId, Guid sourceWpId, Guid targetWpId)
    {
        var dto = new CreateWorkflowLinkDto(sourceWpId, targetWpId);
        var resp = await Client.PostAsJsonAsync($"/api/workflows/{workflowId}/links", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<WorkorderResponseDto> CreateWorkorder(Guid workflowId, string code)
    {
        var dto = new CreateWorkorderDto(code, $"Workorder {code}", null, workflowId, 0);
        var resp = await Client.PostAsJsonAsync("/api/workorders", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkorderResponseDto>(JsonOptions))!;
    }

    private async Task<WorkorderResponseDto> GetWorkorder(Guid id)
        => (await Client.GetFromJsonAsync<WorkorderResponseDto>($"/api/workorders/{id}", JsonOptions))!;

    /// <summary>
    /// Builds a single-step workorder whose entry WorkflowProcess is assigned to the given OrgUnit.
    /// Returns the workorder (which already has WorkorderJobs + Jobs + StepExecutions created).
    /// </summary>
    private async Task<WorkorderResponseDto> BuildOrgUnitWorkorder(Guid orgUnitId, string prefix)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var wf = await CreateWorkflow($"WF-{prefix}", $"Flow {prefix}");
        var entryWp = await AddWorkflowProcess(wf.Id, scenario.Process.Id,
            isEntryPoint: true, assigneeId: orgUnitId);
        var terminalWp = await AddWorkflowProcess(wf.Id, null, isTerminalNode: true);
        await AddWorkflowLink(wf.Id, entryWp.Id, terminalWp.Id);

        var pfx2 = Guid.NewGuid().ToString()[..6];
        return await CreateWorkorder(wf.Id, $"WO-{pfx2}");
    }

    private async Task<PaginatedResponse<StepExecutionResponseDto>> GetMyWork(
        HttpClient client, string status = "Pending", int pageSize = 200)
        => (await client.GetFromJsonAsync<PaginatedResponse<StepExecutionResponseDto>>(
            $"/api/step-executions?status={status}&myWork=true&pageSize={pageSize}", JsonOptions))!;

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MyWork_UserInOrgUnit_SeesOrgUnitAssignedJobs()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var userId = InsertUser($"u1-{pfx}");
        var ou = await CreateOrgUnit($"OU1-{pfx}", "Unit One");
        await AddMember(ou.Id, userId);

        var workorder = await BuildOrgUnitWorkorder(ou.Id, pfx);
        var detail = await GetWorkorder(workorder.Id);
        var jobId = detail.Jobs!.First().JobId;

        using var userClient = _factory.CreateAuthenticatedClient(userId);
        var result = await GetMyWork(userClient);

        Assert.NotNull(result);
        Assert.Contains(result.Items, se => se.JobId == jobId);
    }

    [Fact]
    public async Task MyWork_UserNotInOrgUnit_DoesNotSeeOrgUnitAssignedJobs()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Create OrgUnit with a *different* user as member
        var memberUserId = InsertUser($"u-member-{pfx}");
        var ou = await CreateOrgUnit($"OU2-{pfx}", "Unit Two");
        await AddMember(ou.Id, memberUserId);

        var workorder = await BuildOrgUnitWorkorder(ou.Id, pfx);
        var detail = await GetWorkorder(workorder.Id);
        var jobId = detail.Jobs!.First().JobId;

        // The "not in OrgUnit" user has no memberships and no direct step assignments
        var outsiderUserId = InsertUser($"u-out-{pfx}");
        using var outsiderClient = _factory.CreateAuthenticatedClient(outsiderUserId);
        var result = await GetMyWork(outsiderClient);

        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, se => se.JobId == jobId);
    }

    [Fact]
    public async Task MyWork_DirectlyAssignedStep_AppearsRegardlessOfOrgUnit()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var userId = InsertUser($"u-direct-{pfx}");

        // Create a standalone job (no workorder, no OrgUnit)
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // Directly assign the first step execution to the user in the DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            var se = db.StepExecutions.First(s => s.JobId == job.Id);
            se.AssignedToUserId = userId;
            db.SaveChanges();
        }

        using var userClient = _factory.CreateAuthenticatedClient(userId);
        var result = await GetMyWork(userClient);

        Assert.NotNull(result);
        Assert.Contains(result.Items, se => se.JobId == job.Id);
    }

    [Fact]
    public async Task MyWork_NoDuplicates_WhenBothDirectAndOrgUnitAssigned()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var userId = InsertUser($"u-dual-{pfx}");

        // Add user to an OrgUnit and create a workorder job for it
        var ou = await CreateOrgUnit($"OU3-{pfx}", "Dual Unit");
        await AddMember(ou.Id, userId);
        var workorder = await BuildOrgUnitWorkorder(ou.Id, pfx);
        var detail = await GetWorkorder(workorder.Id);
        var jobId = detail.Jobs!.First().JobId;

        // Also set AssignedToUserId on the same step executions (both paths would match)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            foreach (var se in db.StepExecutions.Where(s => s.JobId == jobId))
                se.AssignedToUserId = userId;
            db.SaveChanges();
        }

        using var userClient = _factory.CreateAuthenticatedClient(userId);
        var result = await GetMyWork(userClient);

        Assert.NotNull(result);
        var forThisJob = result.Items.Where(se => se.JobId == jobId).ToList();
        Assert.True(forThisJob.Count > 0, "Expected step executions for the dual-assigned job.");
        // No duplicates: each step execution ID appears exactly once
        Assert.Equal(forThisJob.Count, forThisJob.Select(se => se.Id).Distinct().Count());
    }

    [Fact]
    public async Task MyWork_UserInMultipleOrgUnits_SeesJobsFromAll()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var userId = InsertUser($"u-multi-{pfx}");

        var ou1 = await CreateOrgUnit($"OU4a-{pfx}", "Multi Unit A");
        var ou2 = await CreateOrgUnit($"OU4b-{pfx}", "Multi Unit B");
        await AddMember(ou1.Id, userId);
        await AddMember(ou2.Id, userId);

        var wo1 = await BuildOrgUnitWorkorder(ou1.Id, $"ma-{pfx}");
        var wo2 = await BuildOrgUnitWorkorder(ou2.Id, $"mb-{pfx}");

        var wo1Detail = await GetWorkorder(wo1.Id);
        var wo2Detail = await GetWorkorder(wo2.Id);
        var job1Id = wo1Detail.Jobs!.First().JobId;
        var job2Id = wo2Detail.Jobs!.First().JobId;

        using var userClient = _factory.CreateAuthenticatedClient(userId);
        var result = await GetMyWork(userClient);

        Assert.NotNull(result);
        var visibleJobIds = result.Items.Select(se => se.JobId).ToHashSet();
        Assert.Contains(job1Id, visibleJobIds);
        Assert.Contains(job2Id, visibleJobIds);
    }
}
