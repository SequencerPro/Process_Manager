using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Tests for GET /api/processes/{id}/validate flow-coverage warnings (data-model Rule 4).
/// Flows connect Material ports only (Rule 3), so coverage applies to Material ports only.
/// </summary>
public class ProcessValidationTests : IntegrationTestBase
{
    public ProcessValidationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Validate_NonMaterialPortsWithoutFlows_NoWarnings()
    {
        var (kind, grade) = await CreateKindWithGrade("PV-001", "PV Kind");
        var stepA = await CreateTransformStepWithNonMaterialPorts("PV-001A", "A", kind.Id, grade.Id);
        var stepB = await CreateTransformStepWithNonMaterialPorts("PV-001B", "B", kind.Id, grade.Id);
        var process = await CreateProcess("PV-001", "PV Non-Material");

        var ps1 = await AddProcessStep(process.Id, stepA.Id, 1);
        var ps2 = await AddProcessStep(process.Id, stepB.Id, 2);

        var outPort = stepA.Ports.Single(p => p.Direction == PortDirection.Output && p.PortType == PortType.Material);
        var inPort = stepB.Ports.Single(p => p.Direction == PortDirection.Input && p.PortType == PortType.Material);
        await AddFlow(process.Id, ps1.Id, outPort.Id, ps2.Id, inPort.Id);

        var result = await GetValidation(process.Id);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Validate_UnconnectedMaterialPortsBesideNonMaterialPorts_WarnsForMaterialOnly()
    {
        var (kind, grade) = await CreateKindWithGrade("PV-002", "PV Kind 2");
        var stepA = await CreateTransformStepWithNonMaterialPorts("PV-002A", "A", kind.Id, grade.Id);
        var stepB = await CreateTransformStepWithNonMaterialPorts("PV-002B", "B", kind.Id, grade.Id);
        var process = await CreateProcess("PV-002", "PV Unconnected");

        await AddProcessStep(process.Id, stepA.Id, 1);
        await AddProcessStep(process.Id, stepB.Id, 2);

        var result = await GetValidation(process.Id);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("output port 'Part Out'"));
        Assert.Contains(result.Warnings, w => w.Contains("input port 'Part In'"));
    }

    private async Task<ProcessValidationResultDto> GetValidation(Guid processId)
    {
        var response = await Client.GetAsync($"/api/processes/{processId}/validate");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcessValidationResultDto>(JsonOptions))!;
    }

    /// <summary>
    /// A Transform step with one Material input and output, plus a Parameter and a
    /// Condition input and a Characteristic output.
    /// </summary>
    private async Task<StepTemplateResponseDto> CreateTransformStepWithNonMaterialPorts(
        string code, string name, Guid kindId, Guid gradeId)
    {
        var dto = new StepTemplateCreateDto(code, name, null, StepPattern.Transform, new()
        {
            new("Part In", PortDirection.Input, PortType.Material, kindId, gradeId,
                QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Spindle Speed", PortDirection.Input, PortType.Parameter, null, null,
                null, null, null, null, DataValueType.Decimal, "RPM", "1200", "50", "50", 1),
            new("Fixture Seated", PortDirection.Input, PortType.Condition, null, null,
                null, null, null, null, null, null, null, null, null, 2),
            new("Part Out", PortDirection.Output, PortType.Material, kindId, gradeId,
                QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Surface Finish", PortDirection.Output, PortType.Characteristic, null, null,
                null, null, null, null, DataValueType.Decimal, "Ra", "0.8", "0.1", "0.1", 1),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }
}
