using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Rule 15 (process interface compatibility) in POST /api/workflows/{id}/validate.
/// </summary>
public class WorkflowValidationTests : IntegrationTestBase
{
    public WorkflowValidationTests(TestWebApplicationFactory factory) : base(factory) { }

    private const string KindMismatchWarning = "source output Kinds do not match target input Kinds";

    // ──────── Helpers ────────

    /// <summary>
    /// Creates a single-step process whose step template has one Material input and one
    /// Material output of the given Kind/Grade, plus an optional Parameter port
    /// (which has no Kind) in the given direction.
    /// </summary>
    private async Task<ProcessResponseDto> CreateSingleStepProcess(
        string pfx, string name, Guid kindId, Guid gradeId, PortDirection? parameterDirection)
    {
        var ports = new List<PortCreateDto>
        {
            new("Part In", PortDirection.Input, PortType.Material, kindId, gradeId,
                QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Part Out", PortDirection.Output, PortType.Material, kindId, gradeId,
                QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 1),
        };
        if (parameterDirection is { } dir)
            ports.Add(new("Temperature", dir, PortType.Parameter, null, null,
                null, null, null, null, DataValueType.Decimal, "°C", null, null, null, 2));

        var stDto = new StepTemplateCreateDto($"ST-{name}-{pfx}", name, null, StepPattern.Transform, ports);
        var stResponse = await Client.PostAsJsonAsync("/api/steptemplates", stDto, JsonOptions);
        stResponse.EnsureSuccessStatusCode();
        var stepTemplate = (await stResponse.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;

        var process = await CreateProcess($"P-{name}-{pfx}", name);
        await AddProcessStep(process.Id, stepTemplate.Id, 1);
        return process;
    }

    /// <summary>Creates a workflow linking source (entry point) → target with an Always link and validates it.</summary>
    private async Task<WorkflowValidationResultDto> ValidateLinkedWorkflow(
        string pfx, Guid sourceProcessId, Guid targetProcessId)
    {
        var wfResponse = await Client.PostAsJsonAsync("/api/workflows",
            new CreateWorkflowDto($"WF-{pfx}", "Rule 15 Workflow"), JsonOptions);
        wfResponse.EnsureSuccessStatusCode();
        var workflow = (await wfResponse.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;

        var srcResponse = await Client.PostAsJsonAsync($"/api/workflows/{workflow.Id}/processes",
            new AddWorkflowProcessDto(sourceProcessId, true, 1), JsonOptions);
        srcResponse.EnsureSuccessStatusCode();
        var src = (await srcResponse.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;

        var tgtResponse = await Client.PostAsJsonAsync($"/api/workflows/{workflow.Id}/processes",
            new AddWorkflowProcessDto(targetProcessId, false, 2), JsonOptions);
        tgtResponse.EnsureSuccessStatusCode();
        var tgt = (await tgtResponse.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(JsonOptions))!;

        var linkResponse = await Client.PostAsJsonAsync($"/api/workflows/{workflow.Id}/links",
            new CreateWorkflowLinkDto(src.Id, tgt.Id, RoutingType.Always, null, 0, null), JsonOptions);
        linkResponse.EnsureSuccessStatusCode();

        var response = await Client.PostAsync($"/api/workflows/{workflow.Id}/validate", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions))!;
    }

    // ──────── Tests ────────

    [Fact]
    public async Task Validate_KindMismatchWithParameterPortsOnBothSides_ReturnsWarning()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kindA, gradeA) = await CreateKindWithGrade($"KA-{pfx}", "Kind A");
        var (kindB, gradeB) = await CreateKindWithGrade($"KB-{pfx}", "Kind B");

        // Both sides also carry a Parameter port, whose KindId is null
        var source = await CreateSingleStepProcess(pfx, "Source", kindA.Id, gradeA.Id, PortDirection.Output);
        var target = await CreateSingleStepProcess(pfx, "Target", kindB.Id, gradeB.Id, PortDirection.Input);

        var result = await ValidateLinkedWorkflow(pfx, source.Id, target.Id);

        Assert.Contains(result.Warnings, w => w.Contains(KindMismatchWarning));
    }

    [Fact]
    public async Task Validate_KindMismatchWithoutParameterPorts_ReturnsWarning()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kindA, gradeA) = await CreateKindWithGrade($"KA-{pfx}", "Kind A");
        var (kindB, gradeB) = await CreateKindWithGrade($"KB-{pfx}", "Kind B");

        var source = await CreateSingleStepProcess(pfx, "Source", kindA.Id, gradeA.Id, null);
        var target = await CreateSingleStepProcess(pfx, "Target", kindB.Id, gradeB.Id, null);

        var result = await ValidateLinkedWorkflow(pfx, source.Id, target.Id);

        Assert.Contains(result.Warnings, w => w.Contains(KindMismatchWarning));
    }

    [Fact]
    public async Task Validate_MatchingKindWithParameterPorts_NoKindWarning()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kind, grade) = await CreateKindWithGrade($"KA-{pfx}", "Kind A");

        var source = await CreateSingleStepProcess(pfx, "Source", kind.Id, grade.Id, PortDirection.Output);
        var target = await CreateSingleStepProcess(pfx, "Target", kind.Id, grade.Id, PortDirection.Input);

        var result = await ValidateLinkedWorkflow(pfx, source.Id, target.Id);

        Assert.DoesNotContain(result.Warnings, w => w.Contains(KindMismatchWarning));
    }
}
