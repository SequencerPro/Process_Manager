using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class ItemSerialNumberTests : IntegrationTestBase
{
    public ItemSerialNumberTests(TestWebApplicationFactory factory) : base(factory) { }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankSerialOnNonSerializedKind_StoresNull(string serial)
    {
        // (KindId, SerialNumber) is unique. A blank serial stored as "" instead of null
        // makes a second blank-serial Item of the same Kind collide on Postgres.
        var pfx = Guid.NewGuid().ToString()[..6];
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, $"JOB-SN-{pfx}");
        var (kind, grade) = await CreateKindWithGrade($"BLK-{pfx}", "Blank Serial Kind");

        var first = await PostItem(new CreateItemDto(kind.Id, grade.Id, job.Id, serial));
        var second = await PostItem(new CreateItemDto(kind.Id, grade.Id, job.Id, serial));

        Assert.Null(first.SerialNumber);
        Assert.Null(second.SerialNumber);
    }

    [Fact]
    public async Task Create_NonBlankSerialOnNonSerializedKind_KeepsSerial()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, $"JOB-SN-{pfx}");
        var (kind, grade) = await CreateKindWithGrade($"NBL-{pfx}", "Optional Serial Kind");

        var item = await PostItem(new CreateItemDto(kind.Id, grade.Id, job.Id, "OPT-001"));

        Assert.Equal("OPT-001", item.SerialNumber);
    }

    private async Task<ItemResponseDto> PostItem(CreateItemDto dto)
    {
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ItemResponseDto>(JsonOptions))!;
    }
}
