using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class StepTemplateTests : IntegrationTestBase
{
    public StepTemplateTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── CREATE ────────────

    [Fact]
    public async Task Create_TransformStep_ReturnsCreated()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-001", "Transform Kind");

        var step = await CreateTransformStep("XFORM-01", "Test Transform",
            kind.Id, grade.Id, kind.Id, grade.Id);

        Assert.Equal("XFORM-01", step.Code);
        Assert.Equal(StepPattern.Transform, step.Pattern);
        Assert.Equal(2, step.Ports.Count);
        Assert.Single(step.Ports, p => p.Direction == PortDirection.Input);
        Assert.Single(step.Ports, p => p.Direction == PortDirection.Output);
    }

    [Fact]
    public async Task Create_DivisionStep_ReturnsCreated()
    {
        var (kind, gradeRaw) = await CreateKindWithGrade("ST-002", "Division Kind", "RAW", "Raw");
        var gradePass = await CreateGrade(kind.Id, "PASS", "Passed", sortOrder: 1);
        var gradeFail = await CreateGrade(kind.Id, "FAIL", "Failed", sortOrder: 2);

        var step = await CreateDivisionStep("DIV-01", "Test Division",
            kind.Id, gradeRaw.Id,
            new()
            {
                ("Good", kind.Id, gradePass.Id),
                ("Bad", kind.Id, gradeFail.Id),
            });

        Assert.Equal(StepPattern.Division, step.Pattern);
        Assert.Equal(3, step.Ports.Count);
        Assert.Single(step.Ports, p => p.Direction == PortDirection.Input);
        Assert.Equal(2, step.Ports.Count(p => p.Direction == PortDirection.Output));
    }

    [Fact]
    public async Task Create_AssemblyStep_ReturnsCreated()
    {
        var (kindA, gradeA) = await CreateKindWithGrade("ST-003A", "Part A");
        var (kindB, gradeB) = await CreateKindWithGrade("ST-003B", "Part B");
        var (kindC, gradeC) = await CreateKindWithGrade("ST-003C", "Assembly");

        var dto = new StepTemplateCreateDto("ASM-01", "Test Assembly", null, StepPattern.Assembly, new()
        {
            new("Part A In", PortDirection.Input, PortType.Material, kindA.Id, gradeA.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Part B In", PortDirection.Input, PortType.Material, kindB.Id, gradeB.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 1),
            new("Assembly Out", PortDirection.Output, PortType.Material, kindC.Id, gradeC.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var step = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(step);
        Assert.Equal(StepPattern.Assembly, step.Pattern);
        Assert.Equal(2, step.Ports.Count(p => p.Direction == PortDirection.Input));
        Assert.Single(step.Ports, p => p.Direction == PortDirection.Output);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-004", "Dup Kind");
        await CreateTransformStep("DUP-STEP", "First", kind.Id, grade.Id, kind.Id, grade.Id);

        var dto = new StepTemplateCreateDto("DUP-STEP", "Second", null, StepPattern.Transform, new()
        {
            new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────── PATTERN VALIDATION ────────────

    [Fact]
    public async Task Create_TransformWithTwoInputs_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-005", "Bad Transform Kind");

        var dto = new StepTemplateCreateDto("BAD-XFORM", "Bad Transform", null, StepPattern.Transform, new()
        {
            new("In 1", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("In 2", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 1),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AssemblyWithOneInput_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-006", "Bad Assembly Kind");

        var dto = new StepTemplateCreateDto("BAD-ASM", "Bad Assembly", null, StepPattern.Assembly, new()
        {
            new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DivisionWithOneOutput_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-007", "Bad Division Kind");

        var dto = new StepTemplateCreateDto("BAD-DIV", "Bad Division", null, StepPattern.Division, new()
        {
            new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── PORT VALIDATION ────────────

    [Fact]
    public async Task Create_PortWithNonExistentKind_ReturnsBadRequest()
    {
        var dto = new StepTemplateCreateDto("NO-KIND", "No Kind", null, StepPattern.Transform, new()
        {
            new("In", PortDirection.Input, PortType.Material, Guid.NewGuid(), Guid.NewGuid(), QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, Guid.NewGuid(), Guid.NewGuid(), QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_PortWithWrongGradeForKind_ReturnsBadRequest()
    {
        var (kindA, gradeA) = await CreateKindWithGrade("ST-008A", "Kind A");
        var (kindB, gradeB) = await CreateKindWithGrade("ST-008B", "Kind B");

        // Use kindA with gradeB — should fail because gradeB belongs to kindB
        var dto = new StepTemplateCreateDto("BAD-GRADE", "Bad Grade", null, StepPattern.Transform, new()
        {
            new("In", PortDirection.Input, PortType.Material, kindA.Id, gradeB.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kindA.Id, gradeA.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── QUANTITY RULE VALIDATION ────────────

    [Fact]
    public async Task Create_ExactlyModeWithoutN_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-009", "Qty Kind");

        var dto = new StepTemplateCreateDto("BAD-QTY", "Bad Qty", null, StepPattern.Transform, new()
        {
            new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, null, null, null, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RangeModeMinGreaterThanMax_ReturnsBadRequest()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-010", "Range Kind");

        var dto = new StepTemplateCreateDto("BAD-RNG", "Bad Range", null, StepPattern.Transform, new()
        {
            new("In", PortDirection.Input, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Range, null, 10, 5, null, null, null, null, null, 0),
            new("Out", PortDirection.Output, PortType.Material, kind.Id, grade.Id, QuantityRuleMode.Exactly, 1, null, null, null, null, null, null, null, 0),
        });

        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── GET / UPDATE / DELETE ────────────

    [Fact]
    public async Task GetById_ExistingStep_ReturnsWithPorts()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-011", "Get Kind");
        var step = await CreateTransformStep("GET-STEP", "Get Step", kind.Id, grade.Id, kind.Id, grade.Id);

        var response = await Client.GetAsync($"/api/steptemplates/{step.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("GET-STEP", result.Code);
        Assert.Equal(2, result.Ports.Count);
        // Ports should include Kind/Grade names
        Assert.All(result.Ports, p =>
        {
            Assert.Equal("ST-011", p.KindCode);
            Assert.Equal("STD", p.GradeCode);
        });
    }

    [Fact]
    public async Task Update_Step_IncrementsVersion()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-012", "Version Kind");
        var step = await CreateTransformStep("VER-STEP", "Version Step", kind.Id, grade.Id, kind.Id, grade.Id);

        Assert.Equal(1, step.Version);

        var updateDto = new StepTemplateUpdateDto("Updated Step", "New desc", StepPattern.Transform);
        var response = await Client.PutAsJsonAsync($"/api/steptemplates/{step.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
        Assert.Equal("Updated Step", updated.Name);
    }

    [Fact]
    public async Task Delete_UnusedStep_ReturnsNoContent()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-013", "Delete Kind");
        var step = await CreateTransformStep("DEL-STEP", "Delete Step", kind.Id, grade.Id, kind.Id, grade.Id);

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UsedInProcess_ReturnsConflict()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-014", "Used Kind");
        var step = await CreateTransformStep("USED-STEP", "Used Step", kind.Id, grade.Id, kind.Id, grade.Id);

        // Create a process that uses this step
        var process = await CreateProcess("PROC-ST14", "Process Using Step");
        await AddProcessStep(process.Id, step.Id, 1);

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────── PORT CRUD ────────────

    [Fact]
    public async Task AddPort_ToExistingStep_IncreasesPortCount()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-015", "Port Add Kind");
        var step = await CreateTransformStep("PORT-ADD", "Port Add Step", kind.Id, grade.Id, kind.Id, grade.Id);

        // Step already has 2 ports. Add a third (making it a General pattern now)
        var gradeNew = await CreateGrade(kind.Id, "NEW", "New Grade", sortOrder: 1);
        var portDto = new PortCreateDto("Extra Out", PortDirection.Output, PortType.Material, kind.Id, gradeNew.Id,
            QuantityRuleMode.ZeroOrN, 1, null, null, null, null, null, null, null, 1);

        var response = await Client.PostAsJsonAsync($"/api/steptemplates/{step.Id}/ports", portDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        // Verify step now has 3 ports
        var getResponse = await Client.GetAsync($"/api/steptemplates/{step.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(3, updated.Ports.Count);
    }

    [Fact]
    public async Task DeletePort_Existing_RemovesPort()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-016", "Port Del Kind");
        var gradePass = await CreateGrade(kind.Id, "PASS", "Passed", sortOrder: 1);
        var gradeFail = await CreateGrade(kind.Id, "FAIL", "Failed", sortOrder: 2);

        var step = await CreateDivisionStep("PORT-DEL", "Port Del Step",
            kind.Id, grade.Id,
            new() { ("Good", kind.Id, gradePass.Id), ("Bad", kind.Id, gradeFail.Id) });

        var portToDelete = step.Ports.Last();

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}/ports/{portToDelete.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync($"/api/steptemplates/{step.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Ports.Count);
    }

    [Fact]
    public async Task Update_SetIsActiveFalse_DeactivatesStep()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-017", "Deactivate Kind");
        var step = await CreateTransformStep("DEACT-ST", "Deactivate Step", kind.Id, grade.Id, kind.Id, grade.Id);

        Assert.True(step.IsActive);

        var updateDto = new StepTemplateUpdateDto(step.Name, step.Description, step.Pattern, IsActive: false);
        var response = await Client.PutAsJsonAsync($"/api/steptemplates/{step.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task Update_OmitIsActive_PreservesCurrentValue()
    {
        var (kind, grade) = await CreateKindWithGrade("ST-018", "Preserve Kind");
        var step = await CreateTransformStep("PRSRV-ST", "Preserve Step", kind.Id, grade.Id, kind.Id, grade.Id);

        // Update without specifying IsActive
        var updateDto = new StepTemplateUpdateDto("Renamed", step.Description, step.Pattern);
        var response = await Client.PutAsJsonAsync($"/api/steptemplates/{step.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.True(updated.IsActive); // still active
        Assert.Equal("Renamed", updated.Name);
    }
}
