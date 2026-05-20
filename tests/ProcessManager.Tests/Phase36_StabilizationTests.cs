using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

/// <summary>
/// Integration tests for Phase 36.1 stabilization changes:
///   T1.3 — [Required] on ImageUploadRequest.File rejects missing file with 400.
///   T1.7 — /approve enforces server-side maturity gate (returns 422 when
///           blocking errors exist).
///   T1.8 — /validate endpoint still produces the expected errors after being
///           refactored to delegate to the Domain validator.
/// </summary>
public class Phase36_StabilizationTests : IntegrationTestBase
{
    public Phase36_StabilizationTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────────── T1.3: ImageUploadRequest [Required] ────────────────

    [Fact]
    public async Task UploadDocument_WithoutFile_Returns400()
    {
        var kind = await CreateKind($"REQ-{Guid.NewGuid().ToString()[..6]}", "Required Test Kind");

        // Multipart with no "File" field — should be rejected by [Required] model validation.
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Some title"), "title");

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("File", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadStepTemplateImage_WithoutFile_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kind, grade) = await CreateKindWithGrade($"K-{pfx}", "ImgGate Kind");
        var step = await CreateTransformStep($"ST-{pfx}", "ImgGate Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        using var content = new MultipartFormDataContent();
        var response = await Client.PostAsync($"/api/steptemplates/{step.Id}/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────────── T1.7: Maturity gate on approve ────────────────

    [Fact]
    public async Task Approve_BlocksWhenMaturityHasErrors()
    {
        // Build a brand-new step template with no content blocks. R01 (Fail) will trip.
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kind, grade) = await CreateKindWithGrade($"K-{pfx}", "MatGate Kind");
        var step = await CreateTransformStep($"ST-{pfx}", "MatGate Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        // Submit for approval (Draft → PendingApproval)
        var submitResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/submit",
            new SubmitForApprovalDto("test-engineer"), JsonOptions);
        submitResp.EnsureSuccessStatusCode();

        // Try to approve — should be blocked by the maturity gate (R01: no content blocks).
        var approveResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/approve",
            new ApproveDto("test-admin"), JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, approveResp.StatusCode);

        var body = await approveResp.Content.ReadAsStringAsync();
        Assert.Contains("Maturity", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("R01", body); // The specific failing rule should be surfaced.

        // And the template should still be PendingApproval (transition did not happen).
        var reloaded = await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{step.Id}", JsonOptions);
        Assert.NotNull(reloaded);
        Assert.Equal("PendingApproval", reloaded!.Status);
    }

    [Fact]
    public async Task Approve_AllowsWhenMaturityHasNoBlockingErrors()
    {
        // Build a template with a content block so R01 passes.
        var pfx = Guid.NewGuid().ToString()[..6];
        var (kind, grade) = await CreateKindWithGrade($"K-{pfx}", "MatGate2 Kind");
        var step = await CreateTransformStep($"ST-{pfx}", "MatGate2 Step",
            kind.Id, grade.Id, kind.Id, grade.Id);

        // Add a Setup text block — satisfies R01 (≥1 block). R04 only fires
        // for Safety blocks, R05 for Inspection — neither apply here.
        var addBlock = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/content/text",
            new AddStepTemplateTextBlockDto("Prepare the workstation.", "Setup"),
            JsonOptions);
        addBlock.EnsureSuccessStatusCode();

        var submitResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/submit",
            new SubmitForApprovalDto("test-engineer"), JsonOptions);
        submitResp.EnsureSuccessStatusCode();

        var approveResp = await Client.PostAsJsonAsync(
            $"/api/steptemplates/{step.Id}/approve",
            new ApproveDto("test-admin"), JsonOptions);

        Assert.True(approveResp.IsSuccessStatusCode,
            $"Expected approve to succeed, got {approveResp.StatusCode}: " +
            await approveResp.Content.ReadAsStringAsync());

        var reloaded = await Client.GetFromJsonAsync<StepTemplateResponseDto>(
            $"/api/steptemplates/{step.Id}", JsonOptions);
        Assert.NotNull(reloaded);
        Assert.Equal("Released", reloaded!.Status);
    }

    // ──────────────── T1.8: /validate endpoint parity after refactor ────────────────

    [Fact]
    public async Task ValidateEndpoint_FlagsMissingEntryPoint()
    {
        // Build a workflow with one process node that is NOT marked as entry.
        var pfx = Guid.NewGuid().ToString()[..6];
        var process = await CreateProcess($"P-{pfx}", "ValidateEntry Process");

        var workflowCreate = await Client.PostAsJsonAsync(
            "/api/workflows",
            new CreateWorkflowDto($"WF-{pfx}", "Validate Entry Test"),
            JsonOptions);
        workflowCreate.EnsureSuccessStatusCode();
        var workflow = (await workflowCreate.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;

        var wpResp = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflow.Id}/processes",
            new AddWorkflowProcessDto(process.Id, IsEntryPoint: false, SortOrder: 0),
            JsonOptions);
        wpResp.EnsureSuccessStatusCode();

        var validateResp = await Client.PostAsync($"/api/workflows/{workflow.Id}/validate", null);
        validateResp.EnsureSuccessStatusCode();

        var result = await validateResp.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.False(result!.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("entry point", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateEndpoint_PassesForWellFormedWorkflow()
    {
        // Build a minimal valid workflow: entry node only.
        var pfx = Guid.NewGuid().ToString()[..6];
        var process = await CreateProcess($"P-{pfx}", "ValidateOk Process");

        var workflowCreate = await Client.PostAsJsonAsync(
            "/api/workflows",
            new CreateWorkflowDto($"WF-{pfx}", "Validate Ok Test"),
            JsonOptions);
        workflowCreate.EnsureSuccessStatusCode();
        var workflow = (await workflowCreate.Content.ReadFromJsonAsync<WorkflowResponseDto>(JsonOptions))!;

        var wpResp = await Client.PostAsJsonAsync(
            $"/api/workflows/{workflow.Id}/processes",
            new AddWorkflowProcessDto(process.Id, IsEntryPoint: true, SortOrder: 0),
            JsonOptions);
        wpResp.EnsureSuccessStatusCode();

        var validateResp = await Client.PostAsync($"/api/workflows/{workflow.Id}/validate", null);
        validateResp.EnsureSuccessStatusCode();

        var result = await validateResp.Content.ReadFromJsonAsync<WorkflowValidationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.IsValid, "Workflow with a single entry node should be valid; errors: " +
            string.Join("; ", result.Errors));
    }
}
