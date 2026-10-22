using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// <c>GET /api/jobs/{id}/items</c> must report each Item's Batch code, the same as
/// <c>GET /api/items/{id}</c> and <c>GET /api/batches/{id}/items</c>.
/// </summary>
public class JobItemsBatchCodeTests : IntegrationTestBase
{
    public JobItemsBatchCodeTests(TestWebApplicationFactory factory) : base(factory) { }

    private static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid().ToString()[..6]}";

    private async Task<List<ItemResponseDto>> GetJobItems(Guid jobId)
    {
        var response = await Client.GetAsync($"/api/jobs/{jobId}/items");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ItemResponseDto>>(JsonOptions))!;
    }

    [Fact]
    public async Task GetItems_ItemInBatch_ReturnsBatchCode()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("JIB"), "Batched Kind", isBatchable: true);
        var batchCode = UniqueCode("BATCH");
        var batch = await CreateBatch(job.Id, kind.Id, grade.Id, batchCode);
        var item = await CreateItem(job.Id, kind.Id, grade.Id, batchId: batch.Id);

        var items = await GetJobItems(job.Id);

        var returned = Assert.Single(items, i => i.Id == item.Id);
        Assert.Equal(batch.Id, returned.BatchId);
        Assert.Equal(batchCode, returned.BatchCode);
    }

    [Fact]
    public async Task GetItems_ItemWithoutBatch_ReturnsNullBatchCode()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("JIN"), "Loose Kind");
        var item = await CreateItem(job.Id, kind.Id, grade.Id);

        var items = await GetJobItems(job.Id);

        var returned = Assert.Single(items, i => i.Id == item.Id);
        Assert.Null(returned.BatchId);
        Assert.Null(returned.BatchCode);
    }
}
