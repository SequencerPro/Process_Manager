using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class StepExecutionTests : IntegrationTestBase
{
    public StepExecutionTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<(WidgetFinishingScenario Scenario, JobResponseDto Job)> SetupRunningJob()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var jobCode = $"JOB-{Guid.NewGuid().ToString()[..6]}";
        var job = await CreateJob(scenario.Process.Id, jobCode);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);
        return (scenario, job);
    }

    // ───── Lifecycle ─────

    [Fact]
    public async Task Start_FirstStep_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);
        response.EnsureSuccessStatusCode();

        var started = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("InProgress", started!.Status);
        Assert.NotNull(started.StartedAt);
    }

    [Fact]
    public async Task Start_SecondStep_BeforeFirstDone_ReturnsBadRequest()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step2 = executions!.First(se => se.Sequence == 2);
        var response = await Client.PostAsync($"/api/step-executions/{step2.Id}/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Start_SecondStep_AfterFirstCompleted_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        var step2 = executions!.First(se => se.Sequence == 2);

        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null);

        var response = await Client.PostAsync($"/api/step-executions/{step2.Id}/start", null);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Complete_Step_SetsCompletedAt()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null);
        response.EnsureSuccessStatusCode();

        var completed = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("Completed", completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task Skip_PendingStep_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/skip", null);
        response.EnsureSuccessStatusCode();

        var skipped = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("Skipped", skipped!.Status);
    }

    [Fact]
    public async Task Fail_InProgressStep_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/fail", null);
        response.EnsureSuccessStatusCode();

        var failed = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("Failed", failed!.Status);
    }

    [Fact]
    public async Task UpdateNotes_AddsOperatorNotes()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        var dto = new UpdateStepExecutionNotesDto("Operator: J. Smith. Machine: CNC-4");
        var response = await Client.PutAsJsonAsync($"/api/step-executions/{step1.Id}/notes", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<StepExecutionResponseDto>(JsonOptions);
        Assert.Equal("Operator: J. Smith. Machine: CNC-4", updated!.Notes);
    }

    [Fact]
    public async Task Start_WhenJobNotInProgress_ReturnsBadRequest()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id, "JOB-NOT-STARTED");
        // Job is still Created, not started
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        var step1 = executions!.First(se => se.Sequence == 1);
        var response = await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ───── Port Transactions ─────

    [Fact]
    public async Task AddPortTransaction_InputPort_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-001");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var ptDto = new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions", ptDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var pt = await response.Content.ReadFromJsonAsync<PortTransactionResponseDto>(JsonOptions);
        Assert.Equal(item.Id, pt!.ItemId);
        Assert.Equal(scenario.DeburrInPort.Id, pt.PortId);
    }

    [Fact]
    public async Task AddPortTransaction_OutputPort_UpdatesItemGrade()
    {
        var (scenario, job) = await SetupRunningJob();
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-GRADE");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);

        // Complete step 1 first
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/complete", null);

        // Start step 2 (Inspection)
        var step2 = executions!.First(se => se.Sequence == 2);
        await Client.PostAsync($"/api/step-executions/{step2.Id}/start", null);

        // Record item through "Good Part" output port (Widget/Passed)
        var ptDto = new CreatePortTransactionDto(scenario.InspGoodPort.Id, item.Id);
        await Client.PostAsJsonAsync($"/api/step-executions/{step2.Id}/port-transactions", ptDto, JsonOptions);

        // Verify item grade changed to Passed
        var updatedItem = await Client.GetFromJsonAsync<ItemResponseDto>($"/api/items/{item.Id}", JsonOptions);
        Assert.Equal(scenario.PassedGrade.Id, updatedItem!.GradeId);
    }

    [Fact]
    public async Task AddPortTransaction_WrongKind_ReturnsBadRequest()
    {
        var (scenario, job) = await SetupRunningJob();

        // Create item of a different Kind
        var (otherKind, otherGrade) = await CreateKindWithGrade("OTHER-001", "Other Thing", "STD", "Standard");
        var otherItem = await CreateItem(job.Id, otherKind.Id, otherGrade.Id);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Try to record wrong-kind item through Widget port
        var ptDto = new CreatePortTransactionDto(scenario.DeburrInPort.Id, otherItem.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions", ptDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddPortTransaction_NotInProgress_ReturnsBadRequest()
    {
        var (scenario, job) = await SetupRunningJob();
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-NIP");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        // Step is still Pending — not started

        var ptDto = new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/port-transactions", ptDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPortTransactions_ReturnsRecordedTransactions()
    {
        var (scenario, job) = await SetupRunningJob();
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-LIST");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        // Record input and output
        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);
        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrOutPort.Id, item.Id), JsonOptions);

        var transactions = await Client.GetFromJsonAsync<List<PortTransactionResponseDto>>(
            $"/api/step-executions/{step1.Id}/port-transactions", JsonOptions);

        Assert.Equal(2, transactions!.Count);
    }

    // ───── Execution Data ─────

    [Fact]
    public async Task AddData_StepLevel_Succeeds()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);

        var dto = new CreateExecutionDataDto("Operator", "J. Smith", DataValueType.String);
        var response = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/data", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(JsonOptions);
        Assert.Equal("Operator", data!.Key);
        Assert.Equal("J. Smith", data.Value);
        Assert.Equal(DataValueType.String, data.DataType);
        Assert.Equal(step1.Id, data.StepExecutionId);
    }

    [Fact]
    public async Task GetData_StepLevel_ReturnsRecordedData()
    {
        var (scenario, job) = await SetupRunningJob();
        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);

        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/data",
            new CreateExecutionDataDto("Operator", "J. Smith", DataValueType.String), JsonOptions);
        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/data",
            new CreateExecutionDataDto("MachineId", "CNC-4", DataValueType.String), JsonOptions);

        var data = await Client.GetFromJsonAsync<List<ExecutionDataResponseDto>>(
            $"/api/step-executions/{step1.Id}/data", JsonOptions);

        Assert.Equal(2, data!.Count);
    }

    [Fact]
    public async Task GetById_IncludesPortTransactions()
    {
        var (scenario, job) = await SetupRunningJob();
        var item = await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, "WDG-DETAIL");

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/port-transactions",
            new CreatePortTransactionDto(scenario.DeburrInPort.Id, item.Id), JsonOptions);

        var detail = await Client.GetFromJsonAsync<StepExecutionResponseDto>(
            $"/api/step-executions/{step1.Id}", JsonOptions);

        Assert.NotNull(detail!.PortTransactions);
        Assert.Single(detail.PortTransactions!);
    }
}
