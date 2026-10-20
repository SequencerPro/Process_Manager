using System.Net;

namespace ProcessManager.Tests;

/// <summary>
/// Deleting a Kind or Grade that Items or Batches still reference. Item and Batch
/// both have Restrict FKs to Kind and Grade, so these deletes must be rejected
/// with 409 rather than failing on save (Postgres) or orphaning rows (InMemory).
/// </summary>
public class KindDeleteReferenceTests : IntegrationTestBase
{
    public KindDeleteReferenceTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> CreateJobId()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        return job.Id;
    }

    private static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid().ToString()[..6]}";

    [Fact]
    public async Task Delete_KindReferencedByItem_ReturnsConflictAndKeepsKind()
    {
        var jobId = await CreateJobId();
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("KDI"), "Item Kind");
        await CreateItem(jobId, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_KindReferencedByBatch_ReturnsConflictAndKeepsKind()
    {
        var jobId = await CreateJobId();
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("KDB"), "Batch Kind", isBatchable: true);
        await CreateBatch(jobId, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGrade_ReferencedByItem_ReturnsConflict()
    {
        var jobId = await CreateJobId();
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("GDI"), "Item Grade Kind");
        await CreateItem(jobId, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{grade.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGrade_ReferencedByBatch_ReturnsConflict()
    {
        var jobId = await CreateJobId();
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("GDB"), "Batch Grade Kind", isBatchable: true);
        await CreateBatch(jobId, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{grade.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_KindWithUnreferencedGrade_ReturnsNoContent()
    {
        var (kind, grade) = await CreateKindWithGrade(UniqueCode("KDU"), "Unused Kind");

        var deleteGrade = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{grade.Id}");
        var deleteKind = await Client.DeleteAsync($"/api/kinds/{kind.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteGrade.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteKind.StatusCode);
    }
}
