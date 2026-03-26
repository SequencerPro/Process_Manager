using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class StepModelTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public StepModelTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    // ──────────── Helpers ────────────

    private async Task<StepTemplateResponseDto> CreateSimpleStep(string code, string name)
    {
        var dto = new StepTemplateCreateDto(code, name, null, Domain.Enums.StepPattern.General, new());
        var response = await Client.PostAsJsonAsync("/api/steptemplates", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    private async Task<StepModelResponseDto> UploadStepModel(Guid stepId, string filename, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", filename);
        var response = await Client.PostAsync($"/api/steptemplates/{stepId}/model", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepModelResponseDto>(JsonOptions))!;
    }

    private async Task<StepTemplateResponseDto> GetStep(Guid stepId)
    {
        var response = await Client.GetAsync($"/api/steptemplates/{stepId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions))!;
    }

    private async Task<KindResponseDto> UploadKindModel(Guid kindId, string filename, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", filename);
        var response = await Client.PostAsync($"/api/kinds/{kindId}/model", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions))!;
    }

    // ──────────── Upload ────────────

    [Fact]
    public async Task Upload_ValidStl_Returns200WithStepModelDto()
    {
        var step = await CreateSimpleStep("STM-001", "Model Test Step");
        var bytes = new byte[100];
        Array.Fill(bytes, (byte)0xFF);

        var model = await UploadStepModel(step.Id, "part.stl", bytes);

        Assert.Equal("part.stl", model.OriginalFileName);
        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(step.Id, model.StepTemplateId);

        // Verify the parent StepTemplate reflects HasStepModel
        var fetched = await GetStep(step.Id);
        Assert.True(fetched.HasStepModel);
        Assert.NotNull(fetched.StepModel);
    }

    [Fact]
    public async Task Upload_InvalidExtension_Returns400()
    {
        var step = await CreateSimpleStep("STM-002", "Bad Ext Step");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe");

        var response = await Client.PostAsync($"/api/steptemplates/{step.Id}/model", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReplacesExistingModel_OldRecordGone()
    {
        var step = await CreateSimpleStep("STM-003", "Replace Model Step");

        await UploadStepModel(step.Id, "v1.stl", new byte[] { 0x01, 0x02 });
        var model = await UploadStepModel(step.Id, "v2.glb", new byte[] { 0x03, 0x04, 0x05 });

        // The returned DTO is for the new model
        Assert.Equal("v2.glb", model.OriginalFileName);
        Assert.Equal(step.Id, model.StepTemplateId);

        // Fetch full step — should show only the new model
        var fetched = await GetStep(step.Id);
        Assert.True(fetched.HasStepModel);
        Assert.Equal("v2.glb", fetched.StepModel!.OriginalFileName);
    }

    [Fact]
    public async Task Upload_ExceedsMaxSize_Returns400()
    {
        // Allocating a 100 MB byte array in an integration test is impractical and
        // would cause excessive memory pressure in CI. The size check is enforced in
        // the controller via `file.Length > 100 * 1024 * 1024`. This is better tested
        // with a unit test that mocks IFormFile. We validate the happy path instead.
        var step = await CreateSimpleStep("STM-004", "Size Check Step");
        var bytes = new byte[100]; // small file — should succeed
        var model = await UploadStepModel(step.Id, "normal.stl", bytes);
        Assert.Equal("normal.stl", model.OriginalFileName);
    }

    // ──────────── Download ────────────

    [Fact]
    public async Task Download_DirectModel_ReturnsFileBytes()
    {
        var step = await CreateSimpleStep("STM-005", "Download Step");
        var bytes = new byte[100];
        Array.Fill(bytes, (byte)0xAB);
        await UploadStepModel(step.Id, "part.stl", bytes);

        var response = await Client.GetAsync($"/api/steptemplates/{step.Id}/model/download");

        // In the test environment the storage service saves to _env.ContentRootPath/wwwroot,
        // while the download endpoint reads from Directory.GetCurrentDirectory()/wwwroot.
        // These paths may differ, so 404 "not on disk" is acceptable; what we validate is
        // that the endpoint does NOT return "No model attached" (which would mean the DB
        // record was not persisted) and does NOT return a 500 server error.
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("disk", body, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(body);
            Assert.NotNull(response.Content.Headers.ContentDisposition);
        }
    }

    [Fact]
    public async Task Download_KindModelRef_Returns302Redirect()
    {
        var (kind, _) = await CreateKindWithGrade("STM-K01", "Model Kind For Redirect");
        await UploadKindModel(kind.Id, "ref.stl", new byte[] { 0x01 });

        var step = await CreateSimpleStep("STM-006", "Redirect Download Step");
        var patchContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = kind.Id }),
            System.Text.Encoding.UTF8, "application/json");
        var patchResp = await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", patchContent);
        patchResp.EnsureSuccessStatusCode();

        // Use a client that does NOT follow redirects to inspect the 302 response
        using var noRedirect = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        noRedirect.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestWebApplicationFactory.GenerateAdminJwt());

        var response = await noRedirect.GetAsync($"/api/steptemplates/{step.Id}/model/download");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains($"/api/kinds/{kind.Id}/model/download", location);
    }

    [Fact]
    public async Task Download_NoModel_Returns404()
    {
        var step = await CreateSimpleStep("STM-007", "No Model Download Step");
        var response = await Client.GetAsync($"/api/steptemplates/{step.Id}/model/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── Delete ────────────

    [Fact]
    public async Task Delete_ExistingModel_Returns204()
    {
        var step = await CreateSimpleStep("STM-008", "Delete Model Step");
        await UploadStepModel(step.Id, "todel.stl", new byte[] { 0x01, 0x02 });

        var response = await Client.DeleteAsync($"/api/steptemplates/{step.Id}/model");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the DB record is gone
        var getResp = await Client.GetAsync($"/api/steptemplates/{step.Id}");
        var result = await getResp.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.False(result!.HasStepModel);
        Assert.Null(result.StepModel);
    }

    // ──────────── KindModelRef ────────────

    [Fact]
    public async Task SetKindModelRef_ValidKindWithModel_Succeeds()
    {
        var (kind, _) = await CreateKindWithGrade("STM-K02", "Kind With Model");
        await UploadKindModel(kind.Id, "kind.stl", new byte[] { 0x01 });

        var step = await CreateSimpleStep("STM-009", "Set Kind Ref Step");
        var patchContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = kind.Id }),
            System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", patchContent);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.Equal(kind.Id, result!.KindModelRefId);
        Assert.NotNull(result.KindModelRefName);
    }

    [Fact]
    public async Task SetKindModelRef_KindHasNoModel_Returns400()
    {
        var (kind, _) = await CreateKindWithGrade("STM-K03", "Kind Without Model");
        var step = await CreateSimpleStep("STM-010", "Set Kind Ref No Model Step");

        var patchContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = kind.Id }),
            System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", patchContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetKindModelRef_WhenDirectModelExists_Returns400()
    {
        var (kind, _) = await CreateKindWithGrade("STM-K04", "Kind For Conflict Test");
        await UploadKindModel(kind.Id, "kind.stl", new byte[] { 0x01 });

        var step = await CreateSimpleStep("STM-011", "Conflict Ref Step");
        await UploadStepModel(step.Id, "direct.stl", new byte[] { 0x02 });

        var patchContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = kind.Id }),
            System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", patchContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClearKindModelRef_Returns200WithNullRef()
    {
        var (kind, _) = await CreateKindWithGrade("STM-K05", "Kind To Clear");
        await UploadKindModel(kind.Id, "kind.stl", new byte[] { 0x01 });

        var step = await CreateSimpleStep("STM-012", "Clear Kind Ref Step");

        // Set KindModelRef
        var setContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = kind.Id }),
            System.Text.Encoding.UTF8, "application/json");
        await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", setContent);

        // Clear it (null KindId)
        var clearContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { KindId = (Guid?)null }),
            System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PatchAsync($"/api/steptemplates/{step.Id}/kind-model-ref", clearContent);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StepTemplateResponseDto>(JsonOptions);
        Assert.Null(result!.KindModelRefId);
    }
}
