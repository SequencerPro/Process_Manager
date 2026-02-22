using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Base class for integration tests. Provides a fresh HttpClient per test class
/// and helper methods for common API operations.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    protected readonly HttpClient Client;
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Client = factory.CreateClient();
    }

    // ──────────── Kind helpers ────────────

    protected async Task<KindResponseDto> CreateKind(
        string code = "TST-001",
        string name = "Test Kind",
        string? description = null,
        bool isSerialized = false,
        bool isBatchable = false)
    {
        var dto = new KindCreateDto(code, name, description, isSerialized, isBatchable);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;
    }

    protected async Task<GradeResponseDto> CreateGrade(
        Guid kindId,
        string code = "STD",
        string name = "Standard",
        string? description = null,
        bool isDefault = false,
        int sortOrder = 0)
    {
        var dto = new GradeCreateDto(code, name, description, isDefault, sortOrder);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{kindId}/grades", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GradeResponseDto>(JsonOptions))!;
    }

    /// <summary>
    /// Creates a Kind with a default Grade and returns both.
    /// Convenience method for tests that need a complete Item Type.
    /// </summary>
    protected async Task<(KindResponseDto Kind, GradeResponseDto Grade)> CreateKindWithGrade(
        string kindCode = "TST-001",
        string kindName = "Test Kind",
        string gradeCode = "STD",
        string gradeName = "Standard",
        bool isSerialized = false,
        bool isBatchable = false)
    {
        var kind = await CreateKind(kindCode, kindName, isSerialized: isSerialized, isBatchable: isBatchable);
        var grade = await CreateGrade(kind.Id, gradeCode, gradeName, isDefault: true);
        return (kind, grade);
    }

    // ──────────── StepTemplate helpers ────────────

    protected async Task<StepTemplateResponseDto> CreateTransformStep(
        string code,
        string name,
        Guid inputKindId, Guid inputGradeId,
        Guid outputKindId, Guid outputGradeId)
    {
        var dto = new StepTemplateCreateDto(code, name, null, Domain.Enums.StepPattern.Transform, new()
        {
            new("Part In", Domain.Enums.PortDirection.Input, inputKindId, inputGradeId,
                Domain.Enums.QuantityRuleMode.Exactly, 1, null, null, 0),
            new("Part Out", Domain.Enums.PortDirection.Output, outputKindId, outputGradeId,
                Domain.Enums.QuantityRuleMode.Exactly, 1, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    protected async Task<StepTemplateResponseDto> CreateDivisionStep(
        string code,
        string name,
        Guid inputKindId, Guid inputGradeId,
        List<(string portName, Guid kindId, Guid gradeId)> outputs)
    {
        var ports = new List<PortCreateDto>
        {
            new("Part In", Domain.Enums.PortDirection.Input, inputKindId, inputGradeId,
                Domain.Enums.QuantityRuleMode.Exactly, 1, null, null, 0)
        };

        for (int i = 0; i < outputs.Count; i++)
        {
            var (portName, kindId, gradeId) = outputs[i];
            ports.Add(new PortCreateDto(portName, Domain.Enums.PortDirection.Output,
                kindId, gradeId, Domain.Enums.QuantityRuleMode.ZeroOrN, 1, null, null, i));
        }

        var dto = new StepTemplateCreateDto(code, name, null, Domain.Enums.StepPattern.Division, ports);
        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    // ──────────── Process helpers ────────────

    protected async Task<ProcessResponseDto> CreateProcess(
        string code = "PROC-01",
        string name = "Test Process")
    {
        var dto = new ProcessCreateDto(code, name, null);
        var response = await Client.PostAsJsonAsync("/api/processes", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions))!;
    }

    protected async Task<ProcessStepResponseDto> AddProcessStep(
        Guid processId, Guid stepTemplateId, int sequence)
    {
        var dto = new ProcessStepCreateDto(stepTemplateId, sequence, null, null);
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{processId}/steps", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcessStepResponseDto>(JsonOptions))!;
    }

    protected async Task<FlowResponseDto> AddFlow(
        Guid processId,
        Guid sourceStepId, Guid sourcePortId,
        Guid targetStepId, Guid targetPortId)
    {
        var dto = new FlowCreateDto(sourceStepId, sourcePortId, targetStepId, targetPortId);
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{processId}/flows", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FlowResponseDto>(JsonOptions))!;
    }

    // ──────────── Job helpers ────────────

    protected async Task<JobResponseDto> CreateJob(
        Guid processId,
        string? code = null,
        string name = "Test Job",
        int priority = 0)
    {
        code ??= $"JOB-{Guid.NewGuid().ToString()[..6]}";
        var dto = new CreateJobDto(code, name, null, processId, priority);
        var response = await Client.PostAsJsonAsync("/api/jobs", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobResponseDto>(JsonOptions))!;
    }

    protected async Task<ItemResponseDto> CreateItem(
        Guid jobId, Guid kindId, Guid gradeId,
        string? serialNumber = null, Guid? batchId = null)
    {
        var dto = new CreateItemDto(kindId, gradeId, jobId, serialNumber, batchId);
        var response = await Client.PostAsJsonAsync("/api/items", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ItemResponseDto>(JsonOptions))!;
    }

    protected async Task<BatchResponseDto> CreateBatch(
        Guid jobId, Guid kindId, Guid gradeId,
        string? code = null, int quantity = 0)
    {
        code ??= $"BATCH-{Guid.NewGuid().ToString()[..6]}";
        var dto = new CreateBatchDto(code, kindId, gradeId, jobId, quantity);
        var response = await Client.PostAsJsonAsync("/api/batches", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BatchResponseDto>(JsonOptions))!;
    }

    /// <summary>
    /// Builds a complete Widget Finishing scenario: Kind with grades, 
    /// Transform + Division steps, Process with 2 steps and a flow.
    /// Returns all IDs needed for execution tests.
    /// Each call uses a unique prefix to avoid collisions within a shared DB.
    /// </summary>
    protected async Task<WidgetFinishingScenario> BuildWidgetFinishingScenario()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Kind + Grades
        var widget = await CreateKind($"WDG-{pfx}", "Widget", isSerialized: true);
        var rawGrade = await CreateGrade(widget.Id, "RAW", "Raw", isDefault: true);
        var passedGrade = await CreateGrade(widget.Id, "PASS", "Passed");
        var failedGrade = await CreateGrade(widget.Id, "FAIL-DIM", "Failed-Dimensional");

        // Deburr step (Transform: Widget/Raw → Widget/Raw)
        var deburr = await CreateTransformStep($"DEBURR-{pfx}", "Deburr",
            widget.Id, rawGrade.Id, widget.Id, rawGrade.Id);

        // Inspection step (Division: Widget/Raw → Widget/Passed + Widget/Failed-Dimensional)
        var inspection = await CreateDivisionStep($"INSP-{pfx}", "Dimensional Inspection",
            widget.Id, rawGrade.Id,
            new List<(string, Guid, Guid)>
            {
                ("Good Part", widget.Id, passedGrade.Id),
                ("Failed Part", widget.Id, failedGrade.Id)
            });

        // Process with 2 steps + flow
        var process = await CreateProcess($"PROC-{pfx}", "Widget Finishing");
        var step1 = await AddProcessStep(process.Id, deburr.Id, 1);
        var step2 = await AddProcessStep(process.Id, inspection.Id, 2);

        var deburOutPort = deburr.Ports!.First(p => p.Direction == Domain.Enums.PortDirection.Output);
        var inspInPort = inspection.Ports!.First(p => p.Direction == Domain.Enums.PortDirection.Input);
        var flow = await AddFlow(process.Id, step1.Id, deburOutPort.Id, step2.Id, inspInPort.Id);

        return new WidgetFinishingScenario
        {
            WidgetKind = widget,
            RawGrade = rawGrade,
            PassedGrade = passedGrade,
            FailedGrade = failedGrade,
            DeburrStep = deburr,
            InspectionStep = inspection,
            Process = process,
            ProcessStep1 = step1,
            ProcessStep2 = step2,
            Flow = flow,
            DeburrInPort = deburr.Ports!.First(p => p.Direction == Domain.Enums.PortDirection.Input),
            DeburrOutPort = deburOutPort,
            InspInPort = inspInPort,
            InspGoodPort = inspection.Ports!.First(p => p.Name == "Good Part"),
            InspFailPort = inspection.Ports!.First(p => p.Name == "Failed Part"),
        };
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}

/// <summary>
/// Contains all IDs from a Widget Finishing scenario for convenient test use.
/// </summary>
public class WidgetFinishingScenario
{
    public KindResponseDto WidgetKind { get; set; } = null!;
    public GradeResponseDto RawGrade { get; set; } = null!;
    public GradeResponseDto PassedGrade { get; set; } = null!;
    public GradeResponseDto FailedGrade { get; set; } = null!;
    public StepTemplateResponseDto DeburrStep { get; set; } = null!;
    public StepTemplateResponseDto InspectionStep { get; set; } = null!;
    public ProcessResponseDto Process { get; set; } = null!;
    public ProcessStepResponseDto ProcessStep1 { get; set; } = null!;
    public ProcessStepResponseDto ProcessStep2 { get; set; } = null!;
    public FlowResponseDto Flow { get; set; } = null!;
    public PortResponseDto DeburrInPort { get; set; } = null!;
    public PortResponseDto DeburrOutPort { get; set; } = null!;
    public PortResponseDto InspInPort { get; set; } = null!;
    public PortResponseDto InspGoodPort { get; set; } = null!;
    public PortResponseDto InspFailPort { get; set; } = null!;
}
