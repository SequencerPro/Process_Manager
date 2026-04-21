using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 23: validates that when a Process outputs a Kind with a Bill of Materials,
/// the sum of effective Material input-port quantities across all steps covers every
/// BomLine component and quantity.
/// </summary>
public class ProcessBomValidationTests : IntegrationTestBase
{
    public ProcessBomValidationTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── Local helpers ────────────

    private async Task<KindResponseDto> CreateKindWithSource(string code, string name, KindSourceType sourceType)
    {
        var dto = new KindCreateDto(code, name, null, false, false, sourceType);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;
    }

    private static PortCreateDto MaterialInputPort(string name, Guid kindId, Guid gradeId,
        QuantityRuleMode mode, int? n = null, int? min = null, int? max = null, int sortOrder = 0) =>
        new(name, PortDirection.Input, PortType.Material, kindId, gradeId,
            mode, n, min, max, null, null, null, null, null, sortOrder);

    private static PortCreateDto MaterialOutputPort(string name, Guid kindId, Guid gradeId,
        QuantityRuleMode mode = QuantityRuleMode.Exactly, int? n = 1, int sortOrder = 0) =>
        new(name, PortDirection.Output, PortType.Material, kindId, gradeId,
            mode, n, null, null, null, null, null, null, null, sortOrder);

    private async Task<StepTemplateResponseDto> CreateAssemblyTemplate(
        string code, string name, List<PortCreateDto> ports)
    {
        var dto = new StepTemplateCreateDto(code, name, null, StepPattern.Assembly, ports);
        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    private async Task<ProcessValidationResultDto> ValidateProcess(Guid processId)
    {
        var response = await Client.GetAsync($"/api/processes/{processId}/validate");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProcessValidationResultDto>(JsonOptions))!;
    }

    /// <summary>Filters out flow/sequence warnings — returns only BOM-related errors.</summary>
    private static List<string> BomErrors(ProcessValidationResultDto result) =>
        result.Errors.Where(e =>
            e.Contains("component", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("BOM", StringComparison.OrdinalIgnoreCase)).ToList();

    private static List<string> BomWarnings(ProcessValidationResultDto result) =>
        result.Warnings.Where(w =>
            w.Contains("component", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("Bill of Materials", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("ZeroOrN", StringComparison.OrdinalIgnoreCase)).ToList();

    // ──────────── Test fixtures ────────────

    /// <summary>
    /// Creates Gear, Bolt, and Widget Kinds with grades. Widget has a BOM: 2 Gear + 4 Bolt.
    /// Returns all IDs needed for test construction.
    /// </summary>
    private async Task<BomFixture> SeedWidgetAssembly(string prefix)
    {
        var gear = await CreateKindWithSource($"GEAR-{prefix}", "Gear", KindSourceType.Buy);
        var gearGrade = await CreateGrade(gear.Id, "STD", "Standard", isDefault: true);

        var bolt = await CreateKindWithSource($"BOLT-{prefix}", "Bolt", KindSourceType.Buy);
        var boltGrade = await CreateGrade(bolt.Id, "STD", "Standard", isDefault: true);

        var widget = await CreateKindWithSource($"WDG-{prefix}", "Widget Assembly", KindSourceType.Make);
        var widgetGrade = await CreateGrade(widget.Id, "STD", "Standard", isDefault: true);

        await CreateBomLine(widget.Id, gear.Id, lineNumber: 1, quantity: 2m);
        await CreateBomLine(widget.Id, bolt.Id, lineNumber: 2, quantity: 4m);

        return new BomFixture(gear, gearGrade, bolt, boltGrade, widget, widgetGrade);
    }

    private sealed record BomFixture(
        KindResponseDto Gear, GradeResponseDto GearGrade,
        KindResponseDto Bolt, GradeResponseDto BoltGrade,
        KindResponseDto Widget, GradeResponseDto WidgetGrade);

    // ──────────── Tests ────────────

    [Fact]
    public async Task Validate_ProcessWithMatchingBomInputs_ReturnsNoBomErrors()
    {
        var f = await SeedWidgetAssembly("A1");

        var tmpl = await CreateAssemblyTemplate($"ASM-{Guid.NewGuid():N}".Substring(0, 12), "Assemble Widget",
            new List<PortCreateDto>
            {
                MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
                MaterialInputPort("Bolts In", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Exactly, n: 4, sortOrder: 1),
                MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A1-{Guid.NewGuid():N}".Substring(0, 12), "Widget Assembly");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);

        Assert.Empty(BomErrors(result));
        Assert.Empty(BomWarnings(result));
    }

    [Fact]
    public async Task Validate_ProcessMissingBomComponent_ReturnsError()
    {
        var f = await SeedWidgetAssembly("A2");

        // Consume only Gears — Bolts are missing from inputs.
        var tmpl = await CreateAssemblyTemplate($"ASM-{Guid.NewGuid():N}".Substring(0, 12), "Missing Bolts",
            new List<PortCreateDto>
            {
                MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
                // Assembly pattern requires 2+ inputs — add an unrelated input.
                MaterialInputPort("Widget Carrier", f.Widget.Id, f.WidgetGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 1),
                MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A2-{Guid.NewGuid():N}".Substring(0, 12), "Missing Bolts Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);

        Assert.Contains(BomErrors(result), e =>
            e.Contains($"BOLT-A2", StringComparison.OrdinalIgnoreCase) &&
            e.Contains("no input port consumes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_ProcessWithWrongQuantity_ReturnsError()
    {
        var f = await SeedWidgetAssembly("A3");

        // Only 2 Bolts instead of 4.
        var tmpl = await CreateAssemblyTemplate($"ASM-{Guid.NewGuid():N}".Substring(0, 12), "Wrong Bolt Qty",
            new List<PortCreateDto>
            {
                MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
                MaterialInputPort("Bolts In", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 1),
                MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A3-{Guid.NewGuid():N}".Substring(0, 12), "Wrong Qty Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);

        Assert.Contains(BomErrors(result), e =>
            e.Contains($"BOLT-A3", StringComparison.OrdinalIgnoreCase) &&
            e.Contains("does not cover required BOM quantity 4", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_InputQuantitySummedAcrossSteps_Passes()
    {
        var f = await SeedWidgetAssembly("A4");

        // Step 1: consumes 2 Gears + 2 Bolts and produces an intermediate (Widget/Draft).
        var step1 = await CreateAssemblyTemplate($"S1-{Guid.NewGuid():N}".Substring(0, 10), "Half Assemble",
            new List<PortCreateDto>
            {
                MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
                MaterialInputPort("Bolts In 1", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 1),
                MaterialOutputPort("Partial Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        // Step 2: consumes 2 more Bolts, outputs finished Widget.
        var step2 = await CreateAssemblyTemplate($"S2-{Guid.NewGuid():N}".Substring(0, 10), "Finish Assemble",
            new List<PortCreateDto>
            {
                MaterialInputPort("Partial In", f.Widget.Id, f.WidgetGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 0),
                MaterialInputPort("Bolts In 2", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 1),
                MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A4-{Guid.NewGuid():N}".Substring(0, 12), "Two Step Process");
        await AddProcessStep(process.Id, step1.Id, 1);
        await AddProcessStep(process.Id, step2.Id, 2);

        var result = await ValidateProcess(process.Id);

        // Total Bolts = 2 + 2 = 4 ✓; Total Gears = 2 ✓.
        Assert.Empty(BomErrors(result));
    }

    [Fact]
    public async Task Validate_RangePortCoversBomQuantity_Passes()
    {
        var f = await SeedWidgetAssembly("A5");

        // Override one Bolt BOM line to 3 — covered by Range [2..5].
        // (Widget default BOM has Bolt=4. Create a new parent Kind with its own smaller BOM.)
        var smallWidget = await CreateKindWithSource($"SWDG-A5", "Small Widget", KindSourceType.Make);
        var swGrade = await CreateGrade(smallWidget.Id, "STD", "Standard", isDefault: true);
        await CreateBomLine(smallWidget.Id, f.Bolt.Id, lineNumber: 1, quantity: 3m);

        var tmpl = await CreateAssemblyTemplate($"A5-{Guid.NewGuid():N}".Substring(0, 10), "Range Input",
            new List<PortCreateDto>
            {
                MaterialInputPort("Bolts In", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Range, min: 2, max: 5, sortOrder: 0),
                MaterialInputPort("Carrier", smallWidget.Id, swGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 1),
                MaterialOutputPort("SWidget Out", smallWidget.Id, swGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A5-{Guid.NewGuid():N}".Substring(0, 12), "Range Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);
        Assert.Empty(BomErrors(result));
    }

    [Fact]
    public async Task Validate_RangePortBelowBomQuantity_Errors()
    {
        var f = await SeedWidgetAssembly("A6");

        var bigWidget = await CreateKindWithSource($"BWDG-A6", "Big Widget", KindSourceType.Make);
        var bwGrade = await CreateGrade(bigWidget.Id, "STD", "Standard", isDefault: true);
        await CreateBomLine(bigWidget.Id, f.Bolt.Id, lineNumber: 1, quantity: 10m);

        var tmpl = await CreateAssemblyTemplate($"A6-{Guid.NewGuid():N}".Substring(0, 10), "Range Under",
            new List<PortCreateDto>
            {
                MaterialInputPort("Bolts In", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Range, min: 2, max: 5, sortOrder: 0),
                MaterialInputPort("Carrier", bigWidget.Id, bwGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 1),
                MaterialOutputPort("BWidget Out", bigWidget.Id, bwGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A6-{Guid.NewGuid():N}".Substring(0, 12), "Range Under Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);
        Assert.Contains(BomErrors(result), e =>
            e.Contains($"BOLT-A6", StringComparison.OrdinalIgnoreCase) &&
            e.Contains("does not cover", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_PortOverrideChangesKind_UsesOverride()
    {
        var f = await SeedWidgetAssembly("A7");

        // Template consumes Gears, but override on the process-step swaps the input to Bolts
        // to correctly match the BOM (2 Gear + 4 Bolt). Two Gear-input ports in template become
        // one Gear port and one overridden Bolt port.
        var tmpl = await CreateAssemblyTemplate($"A7-{Guid.NewGuid():N}".Substring(0, 10), "Override Test",
            new List<PortCreateDto>
            {
                MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
                // Template says Gears qty 4 — will be overridden to Bolts qty 4.
                MaterialInputPort("Flex In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 4, sortOrder: 1),
                MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            });

        var flexPort = tmpl.Ports!.First(p => p.Name == "Flex In");

        var process = await CreateProcess($"P-A7-{Guid.NewGuid():N}".Substring(0, 12), "Override Process");
        var overrideDto = new ProcessStepPortOverrideDto(
            flexPort.Id, null, null, f.Bolt.Id, f.BoltGrade.Id, null, null, null);
        var stepDto = new ProcessStepCreateDto(tmpl.Id, 1, null, null, null, new List<ProcessStepPortOverrideDto> { overrideDto });
        var stepResp = await Client.PostAsJsonAsync($"/api/processes/{process.Id}/steps", stepDto, JsonOptions);
        stepResp.EnsureSuccessStatusCode();

        var result = await ValidateProcess(process.Id);

        // After override: effective inputs are 2 Gear + 4 Bolt — matches the BOM exactly.
        Assert.Empty(BomErrors(result));
    }

    [Fact]
    public async Task Validate_MultipleOutputBoms_AllChecked()
    {
        var f = await SeedWidgetAssembly("A8");

        // Second assembly Kind: requires 1 Gear only.
        var sub = await CreateKindWithSource($"SUB-A8", "Subassembly", KindSourceType.Make);
        var subGrade = await CreateGrade(sub.Id, "STD", "Standard", isDefault: true);
        await CreateBomLine(sub.Id, f.Gear.Id, lineNumber: 1, quantity: 1m);

        // Step outputs BOTH Widget and Subassembly. Inputs cover Widget's BOM (2 Gear + 4 Bolt),
        // but Subassembly requires 1 Gear total — the 2 Gear inputs cover that too actually (2 ≥ 1).
        // To force a failure: make Subassembly require 5 Gear (more than provided 2).
        var sub2 = await CreateKindWithSource($"SUB2-A8", "Bigger Sub", KindSourceType.Make);
        var sub2Grade = await CreateGrade(sub2.Id, "STD", "Standard", isDefault: true);
        await CreateBomLine(sub2.Id, f.Gear.Id, lineNumber: 1, quantity: 5m);

        var multiPorts = new List<PortCreateDto>
        {
            MaterialInputPort("Gears In", f.Gear.Id, f.GearGrade.Id, QuantityRuleMode.Exactly, n: 2, sortOrder: 0),
            MaterialInputPort("Bolts In", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.Exactly, n: 4, sortOrder: 1),
            MaterialOutputPort("Widget Out", f.Widget.Id, f.WidgetGrade.Id, sortOrder: 2),
            MaterialOutputPort("Sub2 Out", sub2.Id, sub2Grade.Id, sortOrder: 3),
        };
        var multiDto = new StepTemplateCreateDto($"A8-{Guid.NewGuid():N}".Substring(0, 10), "Multi Output", null,
            StepPattern.General, multiPorts);
        var mr = await Client.PostAsJsonAsync("/api/steptemplates", multiDto, JsonOptions);
        mr.EnsureSuccessStatusCode();
        var tmpl = (await mr.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;

        var process = await CreateProcess($"P-A8-{Guid.NewGuid():N}".Substring(0, 12), "Multi BOM Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);

        // Widget BOM satisfied; Sub2 BOM (5 Gear) is NOT — expect one error targeting SUB2-A8.
        var errs = BomErrors(result);
        Assert.Contains(errs, e => e.Contains("SUB2-A8", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errs, e => e.Contains($"WDG-A8", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_ZeroOrNPort_EmitsWarning()
    {
        var f = await SeedWidgetAssembly("A9");

        // BOM on small widget: 1 Bolt. Input port uses ZeroOrN with N=1 — covers if triggered, not if skipped.
        var sw = await CreateKindWithSource($"SWDG-A9", "Small Widget", KindSourceType.Make);
        var swGrade = await CreateGrade(sw.Id, "STD", "Standard", isDefault: true);
        await CreateBomLine(sw.Id, f.Bolt.Id, lineNumber: 1, quantity: 1m);

        var tmpl = await CreateAssemblyTemplate($"A9-{Guid.NewGuid():N}".Substring(0, 10), "Conditional Input",
            new List<PortCreateDto>
            {
                MaterialInputPort("Bolt Opt", f.Bolt.Id, f.BoltGrade.Id, QuantityRuleMode.ZeroOrN, n: 1, sortOrder: 0),
                MaterialInputPort("Carrier", sw.Id, swGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 1),
                MaterialOutputPort("SW Out", sw.Id, swGrade.Id, sortOrder: 2),
            });

        var process = await CreateProcess($"P-A9-{Guid.NewGuid():N}".Substring(0, 12), "Conditional Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);

        Assert.Empty(BomErrors(result));
        Assert.Contains(BomWarnings(result), w =>
            w.Contains("ZeroOrN", StringComparison.OrdinalIgnoreCase) &&
            w.Contains("BOLT-A9", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_OutputKindWithoutBom_SkipsBomValidation()
    {
        // Buy-sourced Kind with no BOM — no errors, no warnings related to BOM.
        var part = await CreateKindWithSource($"PART-B1", "Bought Part", KindSourceType.Buy);
        var partGrade = await CreateGrade(part.Id, "STD", "Standard", isDefault: true);

        var raw = await CreateKindWithSource($"RAW-B1", "Raw Stock", KindSourceType.Buy);
        var rawGrade = await CreateGrade(raw.Id, "STD", "Standard", isDefault: true);

        var dto = new StepTemplateCreateDto($"T-B1-{Guid.NewGuid():N}".Substring(0, 10), "Trivial Transform", null,
            StepPattern.Transform,
            new List<PortCreateDto>
            {
                MaterialInputPort("In", raw.Id, rawGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 0),
                MaterialOutputPort("Out", part.Id, partGrade.Id, sortOrder: 1),
            });
        var tr = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        tr.EnsureSuccessStatusCode();
        var tmpl = (await tr.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;

        var process = await CreateProcess($"P-B1-{Guid.NewGuid():N}".Substring(0, 12), "No BOM Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);
        Assert.Empty(BomErrors(result));
        Assert.Empty(BomWarnings(result));
    }

    [Fact]
    public async Task Validate_MakeKindOutputWithoutBom_EmitsWarning()
    {
        var raw = await CreateKindWithSource($"RAW-B2", "Raw Stock", KindSourceType.Buy);
        var rawGrade = await CreateGrade(raw.Id, "STD", "Standard", isDefault: true);

        // Make-sourced Kind with NO BomLines — should trigger warning.
        var made = await CreateKindWithSource($"MADE-B2", "Made Part", KindSourceType.Make);
        var madeGrade = await CreateGrade(made.Id, "STD", "Standard", isDefault: true);

        var dto = new StepTemplateCreateDto($"T-B2-{Guid.NewGuid():N}".Substring(0, 10), "Make Step", null,
            StepPattern.Transform,
            new List<PortCreateDto>
            {
                MaterialInputPort("In", raw.Id, rawGrade.Id, QuantityRuleMode.Exactly, n: 1, sortOrder: 0),
                MaterialOutputPort("Out", made.Id, madeGrade.Id, sortOrder: 1),
            });
        var tr = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        tr.EnsureSuccessStatusCode();
        var tmpl = (await tr.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;

        var process = await CreateProcess($"P-B2-{Guid.NewGuid():N}".Substring(0, 12), "Make No BOM Process");
        await AddProcessStep(process.Id, tmpl.Id, 1);

        var result = await ValidateProcess(process.Id);
        Assert.Contains(BomWarnings(result), w =>
            w.Contains("MADE-B2", StringComparison.OrdinalIgnoreCase) &&
            w.Contains("Make", StringComparison.OrdinalIgnoreCase));
    }
}
