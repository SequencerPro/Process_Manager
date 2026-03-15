using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class BatchTests : IntegrationTestBase
{
    public BatchTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<(WidgetFinishingScenario Scenario, JobResponseDto Job, KindResponseDto BatchKind, GradeResponseDto BatchGrade)> SetupBatchableScenario()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Create a batchable kind (Widget scenario uses serialized-only)
        var batchKind = await CreateKind($"PAINT-{pfx}", "Paint", isSerialized: false, isBatchable: true);
        var batchGrade = await CreateGrade(batchKind.Id, "RAW", "Raw Material", isDefault: true);

        // We still need a process for the job
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        return (scenario, job, batchKind, batchGrade);
    }

    [Fact]
    public async Task Create_Batch_ReturnsCreated()
    {
        var (_, job, batchKind, batchGrade) = await SetupBatchableScenario();

        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-001", 100);

        Assert.Equal("LOT-001", batch.Code);
        Assert.Equal(batchKind.Id, batch.KindId);
        Assert.Equal(batchGrade.Id, batch.GradeId);
        Assert.Equal(100, batch.Quantity);
        Assert.Equal("Open", batch.Status);
    }

    [Fact]
    public async Task Create_NonBatchableKind_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // Widget is serialized but NOT batchable
        var dto = new CreateBatchDto("LOT-BAD", scenario.WidgetKind.Id, scenario.RawGrade.Id, job.Id, 10);
        var response = await Client.PostAsJsonAsync("/api/batches", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var (_, job, batchKind, batchGrade) = await SetupBatchableScenario();
        await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-DUP");

        var dto = new CreateBatchDto("LOT-DUP", batchKind.Id, batchGrade.Id, job.Id);
        var response = await Client.PostAsJsonAsync("/api/batches", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Close_OpenBatch_TransitionsToClosed()
    {
        var (_, job, batchKind, batchGrade) = await SetupBatchableScenario();
        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-CLOSE");

        var response = await Client.PostAsync($"/api/batches/{batch.Id}/close", null);
        response.EnsureSuccessStatusCode();

        var closed = await response.Content.ReadFromJsonAsync<BatchResponseDto>(JsonOptions);
        Assert.Equal("Closed", closed!.Status);
    }

    [Fact]
    public async Task Update_ClosedBatch_ReturnsBadRequest()
    {
        var (_, job, batchKind, batchGrade) = await SetupBatchableScenario();
        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-LOCKED");
        await Client.PostAsync($"/api/batches/{batch.Id}/close", null);

        var dto = new UpdateBatchDto(200);
        var response = await Client.PutAsJsonAsync($"/api/batches/{batch.Id}", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_ToBatch_Succeeds()
    {
        // Need a batchable + serialized kind for this test
        var pfx1 = Guid.NewGuid().ToString()[..6];
        var batchSerKind = await CreateKind($"WAFER-{pfx1}", "Wafer", isSerialized: true, isBatchable: true);
        var batchSerGrade = await CreateGrade(batchSerKind.Id, "RAW", "Raw", isDefault: true);

        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        var batch = await CreateBatch(job.Id, batchSerKind.Id, batchSerGrade.Id);
        var item = await CreateItem(job.Id, batchSerKind.Id, batchSerGrade.Id, $"WFR-{Guid.NewGuid().ToString()[..6]}");

        var response = await Client.PostAsync($"/api/batches/{batch.Id}/items/{item.Id}", null);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ItemResponseDto>(JsonOptions);
        Assert.Equal(batch.Id, updated!.BatchId);
    }

    [Fact]
    public async Task AddItem_WrongKind_ReturnsBadRequest()
    {
        var (scenario, job, batchKind, batchGrade) = await SetupBatchableScenario();
        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-WK");

        // Widget item has different Kind than Paint batch
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-WK");

        var response = await Client.PostAsync($"/api/batches/{batch.Id}/items/{item.Id}", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_FromBatch_ClearsBatchId()
    {
        var batchSerKind = await CreateKind("WAFER-002", "Wafer", isSerialized: true, isBatchable: true);
        var batchSerGrade = await CreateGrade(batchSerKind.Id, "RAW", "Raw", isDefault: true);

        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-B7");

        var batch = await CreateBatch(job.Id, batchSerKind.Id, batchSerGrade.Id, "LOT-REM");
        var item = await CreateItem(job.Id, batchSerKind.Id, batchSerGrade.Id, "WFR-002");

        await Client.PostAsync($"/api/batches/{batch.Id}/items/{item.Id}", null);

        var response = await Client.DeleteAsync($"/api/batches/{batch.Id}/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await Client.GetFromJsonAsync<ItemResponseDto>($"/api/items/{item.Id}", JsonOptions);
        Assert.Null(updated!.BatchId);
    }

    [Fact]
    public async Task GetItems_ReturnsBatchMembers()
    {
        var batchSerKind = await CreateKind("WAFER-003", "Wafer", isSerialized: true, isBatchable: true);
        var batchSerGrade = await CreateGrade(batchSerKind.Id, "RAW", "Raw", isDefault: true);

        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-B8");

        var batch = await CreateBatch(job.Id, batchSerKind.Id, batchSerGrade.Id, "LOT-LIST");
        await CreateItem(job.Id, batchSerKind.Id, batchSerGrade.Id, "WFR-A", batch.Id);
        await CreateItem(job.Id, batchSerKind.Id, batchSerGrade.Id, "WFR-B", batch.Id);

        var items = await Client.GetFromJsonAsync<List<ItemResponseDto>>(
            $"/api/batches/{batch.Id}/items", JsonOptions);

        Assert.Equal(2, items!.Count);
    }

    [Fact]
    public async Task Delete_BatchWithTransactions_ReturnsConflict()
    {
        var (scenario, job, batchKind, batchGrade) = await SetupBatchableScenario();
        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-DEL-TX");

        // Setup: need to make batch go through a port transaction
        // We need a step template that handles this batch's kind
        var step = await CreateTransformStep("PAINT-MIX", "Paint Mix",
            batchKind.Id, batchGrade.Id, batchKind.Id, batchGrade.Id);
        var proc = await CreateProcess("PAINT-PROC", "Paint Process");
        var procStep = await AddProcessStep(proc.Id, step.Id, 1);
        await ReleaseProcess(proc.Id);

        var paintJob = await CreateJob(proc.Id, "JOB-PAINT");
        var paintBatch = await CreateBatch(paintJob.Id, batchKind.Id, batchGrade.Id, "LOT-PAINT", 50);

        await Client.PostAsync($"/api/jobs/{paintJob.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{paintJob.Id}/step-executions", JsonOptions);
        var se1 = executions!.First();
        await Client.PostAsync($"/api/step-executions/{se1.Id}/start", null);

        var inPort = step.Ports!.First(p => p.Direction == ProcessManager.Domain.Enums.PortDirection.Input);
        await Client.PostAsJsonAsync($"/api/step-executions/{se1.Id}/port-transactions",
            new CreatePortTransactionDto(inPort.Id, BatchId: paintBatch.Id), JsonOptions);

        var response = await Client.DeleteAsync($"/api/batches/{paintBatch.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ───── Batch Execution Data ─────

    [Fact]
    public async Task AddData_BatchLevel_Succeeds()
    {
        var (_, job, batchKind, batchGrade) = await SetupBatchableScenario();
        var batch = await CreateBatch(job.Id, batchKind.Id, batchGrade.Id, "LOT-DATA");

        var dto = new CreateExecutionDataDto("Temperature", "72.5", DataValueType.Decimal, "°C");
        var response = await Client.PostAsJsonAsync($"/api/batches/{batch.Id}/data", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(JsonOptions);
        Assert.Equal("Temperature", data!.Key);
        Assert.Equal("72.5", data.Value);
        Assert.Equal(batch.Id, data.BatchId);
    }
}
