using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class ItemTests2 : IntegrationTestBase
{
    public ItemTests2(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_SerializedItem_ReturnsCreated()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I1");

        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-001");

        Assert.Equal("WDG-001", item.SerialNumber);
        Assert.Equal(scenario.WidgetKind.Id, item.KindId);
        Assert.Equal(scenario.RawGrade.Id, item.GradeId);
        Assert.Equal("Available", item.Status);
    }

    [Fact]
    public async Task Create_SerializedKindWithoutSerialNumber_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I2");

        var dto = new CreateItemDto(scenario.WidgetKind.Id, scenario.RawGrade.Id, job.Id);
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSerialNumber_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I3");

        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DUP");

        var dto = new CreateItemDto(scenario.WidgetKind.Id, scenario.RawGrade.Id, job.Id, "WDG-DUP");
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidGradeForKind_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I4");

        // Create another kind with its own grade
        var (otherKind, otherGrade) = await CreateKindWithGrade("OTHER-K", "Other", "G1", "Grade1");

        // Try to create item with Widget kind but Other's grade
        var dto = new CreateItemDto(scenario.WidgetKind.Id, otherGrade.Id, job.Id, "WDG-BAD");
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_NonBatchableKindWithBatch_ReturnsBadRequest()
    {
        // Widget kind is serialized but NOT batchable
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I5");

        var dto = new CreateItemDto(scenario.WidgetKind.Id, scenario.RawGrade.Id, job.Id, "WDG-NB", Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ItemWithPortTransactions_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I6");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DEL");

        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Record port transaction
        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);

        // Try to delete
        var response = await Client.DeleteAsync($"/api/items/{item.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ItemWithoutTransactions_ReturnsNoContent()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I7");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-CLEAN");

        var response = await Client.DeleteAsync($"/api/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ───── Item Execution Data ─────

    [Fact]
    public async Task AddData_ItemLevel_Succeeds()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I8");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DATA");

        var dto = new CreateExecutionDataDto("Length", "50.02", DataValueType.Decimal, "mm");
        var response = await Client.PostAsJsonAsync($"/api/items/{item.Id}/data", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(JsonOptions);
        Assert.Equal("Length", data!.Key);
        Assert.Equal("50.02", data.Value);
        Assert.Equal(DataValueType.Decimal, data.DataType);
        Assert.Equal("mm", data.UnitOfMeasure);
        Assert.Equal(item.Id, data.ItemId);
    }

    [Fact]
    public async Task GetData_ItemLevel_ReturnsList()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-I9");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DAT2");

        await Client.PostAsJsonAsync($"/api/items/{item.Id}/data",
            new CreateExecutionDataDto("Length", "50.02", DataValueType.Decimal, "mm"), JsonOptions);
        await Client.PostAsJsonAsync($"/api/items/{item.Id}/data",
            new CreateExecutionDataDto("Width", "25.01", DataValueType.Decimal, "mm"), JsonOptions);

        var data = await Client.GetFromJsonAsync<List<ExecutionDataResponseDto>>(
            $"/api/items/{item.Id}/data", JsonOptions);

        Assert.Equal(2, data!.Count);
    }
}
