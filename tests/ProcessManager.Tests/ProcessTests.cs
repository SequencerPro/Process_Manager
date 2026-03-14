using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class ProcessTests : IntegrationTestBase
{
    public ProcessTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── PROCESS CRUD ────────────

    [Fact]
    public async Task Create_ValidProcess_ReturnsCreated()
    {
        var process = await CreateProcess("PC-001", "Test Process");

        Assert.Equal("PC-001", process.Code);
        Assert.Equal("Test Process", process.Name);
        Assert.Equal(1, process.Version);
        Assert.True(process.IsActive);
        Assert.Empty(process.Steps);
        Assert.Empty(process.Flows);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await CreateProcess("PC-DUP", "First");

        var dto = new ProcessCreateDto("PC-DUP", "Second", null);
        var response = await Client.PostAsJsonAsync("/api/processes", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsProcess()
    {
        var process = await CreateProcess("PC-002", "Get Process");

        var response = await Client.GetAsync($"/api/processes/{process.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("PC-002", result.Code);
    }

    [Fact]
    public async Task Update_Process_IncrementsVersion()
    {
        var process = await CreateProcess("PC-003", "Original");

        var updateDto = new ProcessUpdateDto("Updated", "New description");
        var response = await Client.PutAsJsonAsync($"/api/processes/{process.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task Delete_Process_ReturnsNoContent()
    {
        var process = await CreateProcess("PC-004", "To Delete");

        var response = await Client.DeleteAsync($"/api/processes/{process.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ──────────── PROCESS STEPS ────────────

    [Fact]
    public async Task AddStep_ValidSequence_ReturnsCreated()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-005", "Step Kind");
        var step = await CreateTransformStep("PS-001", "Process Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-005", "Step Process");

        var ps = await AddProcessStep(process.Id, step.Id, 1);

        Assert.Equal(1, ps.Sequence);
        Assert.Equal(step.Id, ps.StepTemplateId);
        Assert.Equal("PS-001", ps.StepTemplateCode);
    }

    [Fact]
    public async Task AddStep_SkipsSequence_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-006", "Skip Kind");
        var step = await CreateTransformStep("PS-002", "Process Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-006", "Skip Process");

        // Try to add at sequence 2 without sequence 1
        var dto = new ProcessStepCreateDto(step.Id, 2, null, null);
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{process.Id}/steps", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddStep_MultipleStepsInOrder_Succeeds()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-007", "Multi Kind");
        var step1 = await CreateTransformStep("PS-003A", "Step A",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var step2 = await CreateTransformStep("PS-003B", "Step B",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-007", "Multi Process");

        var ps1 = await AddProcessStep(process.Id, step1.Id, 1);
        var ps2 = await AddProcessStep(process.Id, step2.Id, 2);

        Assert.Equal(1, ps1.Sequence);
        Assert.Equal(2, ps2.Sequence);

        // Verify full process
        var response = await Client.GetAsync($"/api/processes/{process.Id}");
        var result = await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Steps.Count);
    }

    [Fact]
    public async Task DeleteStep_Middle_ResequencesRemaining()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-008", "Reseq Kind");
        var stepA = await CreateTransformStep("PS-004A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("PS-004B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepC = await CreateTransformStep("PS-004C", "C", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-008", "Reseq Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);
        var ps3 = await AddProcessStep(process.Id, stepC.Id, 3);

        // Delete middle step
        var response = await Client.DeleteAsync($"/api/processes/{process.Id}/steps/{ps2.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify remaining steps are resequenced
        var getResponse = await Client.GetAsync($"/api/processes/{process.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(1, result.Steps[0].Sequence);
        Assert.Equal(2, result.Steps[1].Sequence);
        Assert.Equal(stepA.Id, result.Steps[0].StepTemplateId);
        Assert.Equal(stepC.Id, result.Steps[1].StepTemplateId);
    }

    // ──────────── FLOWS ────────────

    [Fact]
    public async Task AddFlow_CompatibleTypes_ReturnsCreated()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-009", "Flow Kind");
        var stepA = await CreateTransformStep("FL-001A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("FL-001B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-009", "Flow Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);

        var flow = await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        Assert.Equal("Part Out", flow.SourcePortName);
        Assert.Equal("Part In", flow.TargetPortName);
    }

    [Fact]
    public async Task AddFlow_IncompatibleTypes_ReturnsBadRequest()
    {
        var (kindA, gradeA) = await CreateKindWithGrade("PC-010A", "Kind A");
        var (kindB, gradeB) = await CreateKindWithGrade("PC-010B", "Kind B");

        // Step A outputs Kind A, Step B inputs Kind B — mismatch
        var stepA = await CreateTransformStep("FL-002A", "A", kindA.Id, gradeA.Id, kindA.Id, gradeA.Id);
        var stepB = await CreateTransformStep("FL-002B", "B", kindB.Id, gradeB.Id, kindB.Id, gradeB.Id);
        var process = await CreateProcess("PC-010", "Mismatch Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);

        var dto = new FlowCreateDto(ps1.Id, outPort.Id, ps2.Id, inPort.Id);
        var response = await Client.PostAsJsonAsync($"/api/processes/{process.Id}/flows", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("mismatch", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFlow_SameGradeDifferentKind_ReturnsBadRequest()
    {
        // Even if grade names match, different Kinds = different types
        var (kindA, gradeA) = await CreateKindWithGrade("PC-011A", "Kind A", "RAW", "Raw");
        var (kindB, gradeB) = await CreateKindWithGrade("PC-011B", "Kind B", "RAW", "Raw");

        var stepA = await CreateTransformStep("FL-003A", "A", kindA.Id, gradeA.Id, kindA.Id, gradeA.Id);
        var stepB = await CreateTransformStep("FL-003B", "B", kindB.Id, gradeB.Id, kindB.Id, gradeB.Id);
        var process = await CreateProcess("PC-011", "Cross Kind Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);

        var dto = new FlowCreateDto(ps1.Id, outPort.Id, ps2.Id, inPort.Id);
        var response = await Client.PostAsJsonAsync($"/api/processes/{process.Id}/flows", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddFlow_NonAdjacentSteps_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-012", "Non-Adj Kind");
        var stepA = await CreateTransformStep("FL-004A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("FL-004B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepC = await CreateTransformStep("FL-004C", "C", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-012", "Non-Adj Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);
        var ps3 = await AddProcessStep(process.Id, stepC.Id, 3);

        // Try to connect step 1 to step 3 (skipping step 2)
        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepC.Ports.Single(p => p.Direction == PortDirection.Input);

        var dto = new FlowCreateDto(ps1.Id, outPort.Id, ps3.Id, inPort.Id);
        var response = await Client.PostAsJsonAsync($"/api/processes/{process.Id}/flows", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddFlow_DuplicateSourcePort_ReturnsConflict()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-013", "Dup Flow Kind");
        var stepA = await CreateTransformStep("FL-005A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("FL-005B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-013", "Dup Flow Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);

        // First flow succeeds
        await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        // Second flow on same source port conflicts
        var dto = new FlowCreateDto(ps1.Id, outPort.Id, ps2.Id, inPort.Id);
        var response = await Client.PostAsJsonAsync($"/api/processes/{process.Id}/flows", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFlow_Existing_ReturnsNoContent()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-014", "Del Flow Kind");
        var stepA = await CreateTransformStep("FL-006A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("FL-006B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-014", "Del Flow Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);

        var flow = await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        var response = await Client.DeleteAsync($"/api/processes/{process.Id}/flows/{flow.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify flow is gone
        var getResponse = await Client.GetAsync($"/api/processes/{process.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Flows);
    }

    // ──────────── VALIDATION ────────────

    [Fact]
    public async Task Validate_EmptyProcess_ReturnsWarning()
    {
        var process = await CreateProcess("PC-015", "Empty Process");

        var response = await Client.GetAsync($"/api/processes/{process.Id}/validate");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProcessValidationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("no steps"));
    }

    [Fact]
    public async Task Validate_FullyConnectedProcess_NoWarnings()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-016", "Valid Kind");
        var stepA = await CreateTransformStep("VL-001A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("VL-001B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-016", "Valid Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);
        await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        var response = await Client.GetAsync($"/api/processes/{process.Id}/validate");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProcessValidationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Validate_UnconnectedPorts_ReturnsWarnings()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-017", "Unconn Kind");
        var stepA = await CreateTransformStep("VL-002A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("VL-002B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-017", "Unconnected Process");

        await AddProcessStep(process.Id, stepA.Id, 1);
        await AddProcessStep(process.Id, stepB.Id, 2);

        // No flows — should get warnings about unconnected ports
        var response = await Client.GetAsync($"/api/processes/{process.Id}/validate");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProcessValidationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("not connected"));
    }

    // ──────────── REFERENTIAL INTEGRITY ────────────

    [Fact]
    public async Task DeleteKind_UsedByPort_ReturnsConflict()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-018", "Ref Kind");
        await CreateTransformStep("RI-001", "Ref Step", kind.Id, grade.Id, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGrade_UsedByPort_ReturnsConflict()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-019", "Ref Kind 2");
        await CreateTransformStep("RI-002", "Ref Step 2", kind.Id, grade.Id, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/grades/{grade.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteStep_WithFlows_RemovesFlows()
    {
        var (kind, grade) = await CreateKindWithGrade("PC-020", "Flow Del Kind");
        var stepA = await CreateTransformStep("FD-001A", "A", kind.Id, grade.Id, kind.Id, grade.Id);
        var stepB = await CreateTransformStep("FD-001B", "B", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PC-020", "Flow Del Process");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input);
        await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        // Delete step 1 — should also remove the flow
        var response = await Client.DeleteAsync($"/api/processes/{process.Id}/steps/{ps1.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync($"/api/processes/{process.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Single(result.Steps);
        Assert.Empty(result.Flows);
    }

    [Fact]
    public async Task Update_SetIsActiveFalse_DeactivatesProcess()
    {
        var process = await CreateProcess("PC-021", "Deactivate Process");

        Assert.True(process.IsActive);

        var updateDto = new ProcessUpdateDto(process.Name, process.Description, IsActive: false);
        var response = await Client.PutAsJsonAsync($"/api/processes/{process.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task Update_OmitIsActive_PreservesCurrentValue()
    {
        var process = await CreateProcess("PC-022", "Preserve Process");

        var updateDto = new ProcessUpdateDto("Renamed", process.Description);
        var response = await Client.PutAsJsonAsync($"/api/processes/{process.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.True(updated.IsActive);
        Assert.Equal("Renamed", updated.Name);
    }
}
