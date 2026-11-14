using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// DELETE /api/jobs/{id} relies on database cascades (Job → Items, Batches, StepExecutions).
/// PortTransaction.ItemId and BatchId are ON DELETE RESTRICT, so on Postgres a Job whose
/// Items or Batches appear in port transactions can't be deleted and SaveChanges threw (500).
/// EF InMemory doesn't enforce FKs, so these tests check the API-level guard instead.
/// </summary>
public class JobDeleteTests : IntegrationTestBase
{
    public JobDeleteTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Delete_JobWhoseItemHasPortTransactions_ReturnsConflict()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-DEL-PT-I");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DEL-PT");

        var step1 = await StartJobAndFirstStep(job.Id);
        var pt = await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);
        pt.EnsureSuccessStatusCode();
        (await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null)).EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync($"/api/jobs/{job.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/jobs/{job.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/items/{item.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_JobWhoseBatchHasPortTransactions_ReturnsConflict()
    {
        var kind = await CreateKind("PAINT-DEL", "Paint", isBatchable: true);
        var grade = await CreateGrade(kind.Id, "STD", "Standard", isDefault: true);
        var template = await CreateTransformStep("MIX-DEL", "Mix", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("MIX-PROC-DEL", "Mix Process");
        await AddProcessStep(process.Id, template.Id, 1);

        var job = await CreateJob(process.Id, "JOB-DEL-PT-B");
        var batch = await CreateBatch(job.Id, kind.Id, grade.Id, "LOT-DEL-PT", 10);

        var step1 = await StartJobAndFirstStep(job.Id);
        var inPort = template.Ports!.First(p => p.Direction == PortDirection.Input);
        var pt = await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(inPort.Id, BatchId: batch.Id), JsonOptions);
        pt.EnsureSuccessStatusCode();
        (await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null)).EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync($"/api/jobs/{job.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/jobs/{job.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/batches/{batch.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_CancelledJobWithItemsButNoPortTransactions_ReturnsNoContent()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-DEL-NOPT");
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DEL-NOPT");

        await StartJobAndFirstStep(job.Id);
        (await Client.PostAsync($"/api/jobs/{job.Id}/cancel", null)).EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync($"/api/jobs/{job.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/jobs/{job.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/items/{item.Id}")).StatusCode);
    }

    private async Task<StepExecutionResponseDto> StartJobAndFirstStep(Guid jobId)
    {
        (await Client.PostAsync($"/api/jobs/{jobId}/start", null)).EnsureSuccessStatusCode();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{jobId}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        (await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null)).EnsureSuccessStatusCode();
        return step1;
    }
}
