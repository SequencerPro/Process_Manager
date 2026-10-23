using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Deleting a Port must not orphan PortTransactions. <c>PortTransaction.Port</c> is
/// <c>Restrict</c>, and a port with no Flow (a first-step input or last-step output)
/// can still have transactions recorded against it.
/// </summary>
public class PortDeleteReferenceTests : IntegrationTestBase
{
    public PortDeleteReferenceTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeletePort_WithPortTransactions_ReturnsConflictAndKeepsPort()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id,
            $"WDG-{Guid.NewGuid().ToString()[..6]}");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // DeburrInPort is the first step's input, so no Flow references it.
        var ptResponse = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);
        ptResponse.EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync(
            $"/api/steptemplates/{scenario.DeburrStep.Id}/ports/{scenario.DeburrInPort.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var template = await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{scenario.DeburrStep.Id}", JsonOptions);
        Assert.Contains(template!.Ports!, p => p.Id == scenario.DeburrInPort.Id);
    }

    [Fact]
    public async Task DeletePort_Unreferenced_ReturnsNoContent()
    {
        var scenario = await BuildWidgetFinishingScenario();

        // InspFailPort is the last step's output: no Flow and no transactions.
        var response = await Client.DeleteAsync(
            $"/api/steptemplates/{scenario.InspectionStep.Id}/ports/{scenario.InspFailPort.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
