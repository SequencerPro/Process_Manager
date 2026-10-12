using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class PromptBlockTests : IntegrationTestBase
{
    public PromptBlockTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── StepTemplate prompt blocks ────────────

    [Theory]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("NotAType")]
    public async Task StepTemplate_AddPromptBlock_UndefinedPromptType_ReturnsBadRequest(string promptType)
    {
        var (kind, grade) = await CreateKindWithGrade($"PB-ST-{promptType}", "Prompt Kind");
        var step = await CreateTransformStep($"PB-ST-{promptType}", "Prompt Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var dto = new AddStepTemplatePromptBlockDto("Torque", promptType);
        var response = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/content/prompt", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await Client.GetFromJsonAsync<List<StepTemplateContentResponseDto>>(
            $"/api/steptemplates/{step.Id}/content", JsonOptions);
        Assert.NotNull(content);
        Assert.Empty(content);
    }

    [Theory]
    [InlineData("NumericEntry", "NumericEntry")]
    [InlineData("passfail", "PassFail")]
    public async Task StepTemplate_AddPromptBlock_NamedPromptType_ReturnsCreated(
        string promptType, string expected)
    {
        var (kind, grade) = await CreateKindWithGrade($"PB-STOK-{promptType}", "Prompt Kind");
        var step = await CreateTransformStep($"PB-STOK-{promptType}", "Prompt Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        var dto = new AddStepTemplatePromptBlockDto("Torque", promptType);
        var response = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/content/prompt", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var block = await response.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(JsonOptions);
        Assert.NotNull(block);
        Assert.Equal(expected, block.PromptType);
    }

    // ──────────── ProcessStep prompt blocks ────────────

    [Theory]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("NotAType")]
    public async Task ProcessStep_AddPromptBlock_UndefinedPromptType_ReturnsBadRequest(string promptType)
    {
        var (kind, grade) = await CreateKindWithGrade($"PB-PS-{promptType}", "Prompt Kind");
        var step = await CreateTransformStep($"PB-PS-{promptType}", "Prompt Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"PB-PS-{promptType}", "Prompt Process");
        var ps = await AddProcessStep(process.Id, step.Id, 1);

        var dto = new AddPromptBlockDto("Torque", promptType);
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{process.Id}/steps/{ps.Id}/content/prompt", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await Client.GetFromJsonAsync<List<ProcessStepContentResponseDto>>(
            $"/api/processes/{process.Id}/steps/{ps.Id}/content", JsonOptions);
        Assert.NotNull(content);
        Assert.Empty(content);
    }

    [Fact]
    public async Task ProcessStep_AddPromptBlock_NamedPromptType_ReturnsCreated()
    {
        var (kind, grade) = await CreateKindWithGrade("PB-PSOK", "Prompt Kind");
        var step = await CreateTransformStep("PB-PSOK", "Prompt Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess("PB-PSOK", "Prompt Process");
        var ps = await AddProcessStep(process.Id, step.Id, 1);

        var dto = new AddPromptBlockDto("Coolant filled", "Checkbox");
        var response = await Client.PostAsJsonAsync(
            $"/api/processes/{process.Id}/steps/{ps.Id}/content/prompt", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var block = await response.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions);
        Assert.NotNull(block);
        Assert.Equal("Checkbox", block.PromptType);
    }
}
