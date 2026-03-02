using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Tests for cross-cutting improvements: pagination, search/filtering,
/// DTO validation, and top-level list endpoints.
/// </summary>
public class CrossCuttingTests : IntegrationTestBase
{
    public CrossCuttingTests(TestWebApplicationFactory factory) : base(factory) { }

    // ══════════════════════ Pagination ══════════════════════

    [Fact]
    public async Task Kinds_GetAll_ReturnsPaginatedResponse()
    {
        await CreateKind("PG-K1", "Kind Alpha");
        await CreateKind("PG-K2", "Kind Beta");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.True(result.Items.Count >= 2);
    }

    [Fact]
    public async Task Kinds_GetAll_PaginationLimitsResults()
    {
        for (int i = 0; i < 3; i++)
            await CreateKind($"PG-L{i}", $"Limit Kind {i}");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds?page=1&pageSize=2", JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Items.Count);
        Assert.True(result.TotalCount >= 3);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task Kinds_GetAll_Page2_ReturnsRemainingItems()
    {
        for (int i = 0; i < 3; i++)
            await CreateKind($"PG-P{i}", $"Page Kind {i}");

        var page1 = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds?page=1&pageSize=2", JsonOptions);
        var page2 = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds?page=2&pageSize=2", JsonOptions);

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(2, page1!.Items.Count);
        Assert.True(page2!.Items.Count >= 1);
        Assert.True(page2.HasPreviousPage);
    }

    // ══════════════════════ Search ══════════════════════

    [Fact]
    public async Task Kinds_GetAll_SearchByCode_FiltersResults()
    {
        await CreateKind("SRCH-001", "Alpha Widget");
        await CreateKind("SRCH-002", "Beta Gadget");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds?search=SRCH-001", JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("SRCH-001", result.Items[0].Code);
    }

    [Fact]
    public async Task Kinds_GetAll_SearchByName_FiltersResults()
    {
        await CreateKind("SRCH-003", "Unique Flange");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            "/api/kinds?search=Flange", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, k => k.Name == "Unique Flange");
    }

    [Fact]
    public async Task Jobs_GetAll_SearchByCode_FiltersResults()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job1 = await CreateJob(scenario.Process.Id, "JSRCH-AAA");
        var job2 = await CreateJob(scenario.Process.Id, "JSRCH-BBB");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<JobResponseDto>>(
            "/api/jobs?search=JSRCH-AAA", JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("JSRCH-AAA", result.Items[0].Code);
    }

    [Fact]
    public async Task Jobs_GetAll_FilterByProcessId()
    {
        var scenario1 = await BuildWidgetFinishingScenario();
        var scenario2 = await BuildWidgetFinishingScenario();
        var job1 = await CreateJob(scenario1.Process.Id, "JPID-1");
        var job2 = await CreateJob(scenario2.Process.Id, "JPID-2");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<JobResponseDto>>(
            $"/api/jobs?processId={scenario1.Process.Id}", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, j => j.Code == "JPID-1");
        Assert.DoesNotContain(result.Items, j => j.Code == "JPID-2");
    }

    // ══════════════════════ Active Filter ══════════════════════

    [Fact]
    public async Task StepTemplates_GetAll_ActiveFilter()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var allResult = await Client.GetFromJsonAsync<PaginatedResponse<StepTemplateResponseDto>>(
            "/api/steptemplates", JsonOptions);

        var activeResult = await Client.GetFromJsonAsync<PaginatedResponse<StepTemplateResponseDto>>(
            "/api/steptemplates?active=true", JsonOptions);

        Assert.NotNull(allResult);
        Assert.NotNull(activeResult);
        // All templates are active by default
        Assert.Equal(allResult!.TotalCount, activeResult!.TotalCount);
    }

    [Fact]
    public async Task Processes_GetAll_ReturnsSummaryDto()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            "/api/processes", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);

        var proc = result.Items.First(p => p.Id == scenario.Process.Id);
        Assert.Equal(2, proc.StepCount); // 2 steps in scenario
        Assert.True(proc.IsActive);
    }

    [Fact]
    public async Task Processes_GetAll_SearchByName()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var process = await CreateProcess($"PRC-{pfx}", "Special Process");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            "/api/processes?search=Special", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Name == "Special Process");
    }

    // ══════════════════════ Top-Level List Endpoints ══════════════════════

    [Fact]
    public async Task Items_GetAll_ReturnsPaginatedResponse()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "ITEM-TLE-1");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ItemResponseDto>>(
            "/api/items", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
        Assert.Contains(result.Items, i => i.SerialNumber == "ITEM-TLE-1");
    }

    [Fact]
    public async Task Items_GetAll_FilterByJobId()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job1 = await CreateJob(scenario.Process.Id, "IFJ-1");
        var job2 = await CreateJob(scenario.Process.Id, "IFJ-2");
        await CreateItem(job1.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "ITEM-IFJ-1");
        await CreateItem(job2.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "ITEM-IFJ-2");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ItemResponseDto>>(
            $"/api/items?jobId={job1.Id}", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, i => i.SerialNumber == "ITEM-IFJ-1");
        Assert.DoesNotContain(result.Items, i => i.SerialNumber == "ITEM-IFJ-2");
    }

    [Fact]
    public async Task Items_GetAll_FilterByKindId()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "ITEM-FKI-1");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ItemResponseDto>>(
            $"/api/items?kindId={scenario.WidgetKind.Id}", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, i => i.SerialNumber == "ITEM-FKI-1");
    }

    [Fact]
    public async Task Items_GetAll_SearchBySerialNumber()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "UNIQ-SN-99");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<ItemResponseDto>>(
            "/api/items?search=UNIQ-SN", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, i => i.SerialNumber == "UNIQ-SN-99");
    }

    [Fact]
    public async Task Batches_GetAll_ReturnsPaginatedResponse()
    {
        var scenario = await BuildWidgetFinishingScenario();
        // Need batchable kind
        var kind = await CreateKind($"BK-{Guid.NewGuid().ToString()[..4]}", "Batch Kind",
            isSerialized: false, isBatchable: true);
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);
        var job = await CreateJob(scenario.Process.Id);
        var batch = await CreateBatch(job.Id, kind.Id, grade.Id, "BATCH-TLE-1", 10);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<BatchResponseDto>>(
            "/api/batches", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
        Assert.Contains(result.Items, b => b.Code == "BATCH-TLE-1");
    }

    [Fact]
    public async Task Batches_GetAll_FilterByJobId()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var kind = await CreateKind($"BFJ-{Guid.NewGuid().ToString()[..4]}", "Batch Filter Kind",
            isSerialized: false, isBatchable: true);
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);
        var job1 = await CreateJob(scenario.Process.Id, "BFJ-1");
        var job2 = await CreateJob(scenario.Process.Id, "BFJ-2");
        await CreateBatch(job1.Id, kind.Id, grade.Id, "BATCH-BFJ-1", 5);
        await CreateBatch(job2.Id, kind.Id, grade.Id, "BATCH-BFJ-2", 5);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<BatchResponseDto>>(
            $"/api/batches?jobId={job1.Id}", JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, b => b.Code == "BATCH-BFJ-1");
        Assert.DoesNotContain(result.Items, b => b.Code == "BATCH-BFJ-2");
    }

    [Fact]
    public async Task StepExecutions_GetAll_ReturnsPaginatedResponse()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<StepExecutionResponseDto>>(
            "/api/step-executions", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2); // 2 steps auto-created
    }

    [Fact]
    public async Task StepExecutions_GetAll_FilterByJobId()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job1 = await CreateJob(scenario.Process.Id, "SEFJ-1");
        var job2 = await CreateJob(scenario.Process.Id, "SEFJ-2");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<StepExecutionResponseDto>>(
            $"/api/step-executions?jobId={job1.Id}", JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalCount); // Exactly 2 steps for this job
    }

    [Fact]
    public async Task StepExecutions_GetAll_FilterByStatus()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "SEFS-1");

        var result = await Client.GetFromJsonAsync<PaginatedResponse<StepExecutionResponseDto>>(
            "/api/step-executions?status=Pending", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2); // At least the 2 from this job
        Assert.All(result.Items, se => Assert.Equal("Pending", se.Status));
    }

    // ══════════════════════ DomainVocabulary Pagination ══════════════════════

    [Fact]
    public async Task DomainVocabularies_GetAll_ReturnsPaginatedResponse()
    {
        var dto = new DomainVocabularyCreateDto("Test Vocab",
            "Kind", "KindCode", "Grade", "Item", "ItemId",
            "Batch", "BatchId", "Job", "Workflow", "Process", "Step");
        await Client.PostAsJsonAsync("/api/domainvocabularies", dto, JsonOptions);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<DomainVocabularyResponseDto>>(
            "/api/domainvocabularies", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
    }

    [Fact]
    public async Task DomainVocabularies_GetAll_SearchByName()
    {
        var dto = new DomainVocabularyCreateDto("Unique Vocab XYZ",
            "Kind", "KindCode", "Grade", "Item", "ItemId",
            "Batch", "BatchId", "Job", "Workflow", "Process", "Step");
        await Client.PostAsJsonAsync("/api/domainvocabularies", dto, JsonOptions);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<DomainVocabularyResponseDto>>(
            "/api/domainvocabularies?search=Unique Vocab XYZ", JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("Unique Vocab XYZ", result.Items[0].Name);
    }

    // ══════════════════════ Workflow Pagination ══════════════════════

    [Fact]
    public async Task Workflows_GetAll_ReturnsPaginatedResponse()
    {
        var dto = new CreateWorkflowDto($"WF-PG-{Guid.NewGuid().ToString()[..4]}", "Paginated WF", null);
        await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<WorkflowResponseDto>>(
            "/api/workflows", JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Workflows_GetAll_SearchByCode()
    {
        var code = $"WF-SRCH-{Guid.NewGuid().ToString()[..4]}";
        var dto = new CreateWorkflowDto(code, "Searchable WF", null);
        await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);

        var result = await Client.GetFromJsonAsync<PaginatedResponse<WorkflowResponseDto>>(
            $"/api/workflows?search={code}", JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal(code, result.Items[0].Code);
    }

    // ══════════════════════ DTO Validation ══════════════════════

    [Fact]
    public async Task Create_Kind_EmptyCode_Returns400()
    {
        var dto = new KindCreateDto("", "A Name", null, false, false);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Kind_CodeTooLong_Returns400()
    {
        var longCode = new string('X', 51);
        var dto = new KindCreateDto(longCode, "A Name", null, false, false);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Job_EmptyCode_Returns400()
    {
        var dto = new CreateJobDto("", "A Name", null, Guid.NewGuid(), 0);
        var response = await Client.PostAsJsonAsync("/api/jobs", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Process_EmptyCode_Returns400()
    {
        var dto = new ProcessCreateDto("", "A Name", null);
        var response = await Client.PostAsJsonAsync("/api/processes", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Workflow_EmptyCode_Returns400()
    {
        var dto = new CreateWorkflowDto("", "A Name", null);
        var response = await Client.PostAsJsonAsync("/api/workflows", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
