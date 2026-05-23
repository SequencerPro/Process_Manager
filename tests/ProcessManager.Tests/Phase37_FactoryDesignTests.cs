using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 37 — integration tests for per-placement CAD model upload/convert and
/// designed-vs-live material flow.
/// </summary>
public class Phase37_FactoryDesignTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public Phase37_FactoryDesignTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    // ──────────── Helpers ────────────

    private async Task<Guid> CreateFloorPlan(string code, string name)
    {
        var resp = await Client.PostAsJsonAsync("/api/floor-plans", new FloorPlanCreateDto(code, name, null), JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<FloorPlanSummaryDto>(JsonOptions))!.Id;
    }

    private async Task<Guid> CreateWorkstation(Guid fpId, string placementId)
    {
        var resp = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/workstations",
            new FloorPlanWorkstationCreateDto(placementId, null, null, null), JsonOptions);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("id").GetGuid();
    }

    private async Task<StorageLocationResponseDto> CreateStorageLocation(string code, string name)
    {
        var resp = await Client.PostAsJsonAsync("/api/warehouse/locations", new CreateStorageLocationDto(code, name), JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<StorageLocationResponseDto>(JsonOptions))!;
    }

    private static MultipartFormDataContent ModelUpload(string fileName, string mime = "application/octet-stream")
    {
        var content = new MultipartFormDataContent();
        var bytes = "ISO-10303-21; fake cad content"u8.ToArray();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
        content.Add(file, "File", fileName);
        return content;
    }

    // ──────────── Model upload ────────────

    [Fact]
    public async Task UploadModel_WebReadyGlb_IsImmediatelyRenderable()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Model Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using var content = ModelUpload("machine.glb", "model/gltf-binary");
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.NotRequired, model!.ConversionStatus);
        Assert.True(model.HasRenderableModel);
    }

    [Fact]
    public async Task UploadModel_StepFile_IsPendingConversion()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Step Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using var content = ModelUpload("assembly.step", "application/step");
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.Pending, model!.ConversionStatus);
        Assert.False(model.HasRenderableModel); // not renderable until converted
    }

    [Fact]
    public async Task UploadModel_UnsupportedFormat_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Bad Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using var content = ModelUpload("notes.txt", "text/plain");
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Convert_NoConverterConfigured_MarksFailed()
    {
        // The default ExternalProcessStepConversionService has no command configured
        // in tests, so conversion should fail cleanly (not throw).
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Convert Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using var content = ModelUpload("assembly.step", "application/step");
        (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content)).EnsureSuccessStatusCode();

        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/convert", null);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.Failed, model!.ConversionStatus);
        Assert.False(string.IsNullOrEmpty(model.ConversionError));
    }

    [Fact]
    public async Task Convert_WithFakeConverter_MarksConverted()
    {
        // Swap in a fake converter that always succeeds.
        var fakeFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IStepConversionService));
                if (existing is not null) services.Remove(existing);
                services.AddScoped<IStepConversionService, FakeStepConversionService>();
            });
        });
        var client = fakeFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestWebApplicationFactory.GenerateAdminJwt());

        var pfx = Guid.NewGuid().ToString()[..6];
        var createResp = await client.PostAsJsonAsync("/api/floor-plans", new FloorPlanCreateDto($"FP-{pfx}", "OK Convert", null), JsonOptions);
        var fpId = (await createResp.Content.ReadFromJsonAsync<FloorPlanSummaryDto>(JsonOptions))!.Id;
        var wsResp = await client.PostAsJsonAsync($"/api/floor-plans/{fpId}/workstations",
            new FloorPlanWorkstationCreateDto("ws-1", null, null, null), JsonOptions);
        var wsId = (await wsResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        using var content = ModelUpload("assembly.step", "application/step");
        (await client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content)).EnsureSuccessStatusCode();

        var resp = await client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/convert", null);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.Converted, model!.ConversionStatus);
        Assert.True(model.HasRenderableModel);
    }

    // ──────────── Client-assisted conversion (model/converted) ────────────

    private static MultipartFormDataContent GlbUpload()
    {
        var content = new MultipartFormDataContent();
        // Minimal glb header bytes ("glTF" magic) — content isn't parsed server-side.
        var bytes = new byte[] { 0x67, 0x6C, 0x54, 0x46, 0x02, 0x00, 0x00, 0x00 };
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("model/gltf-binary");
        content.Add(file, "File", "converted.glb");
        return content;
    }

    [Fact]
    public async Task UploadConverted_StoresGlb_AndMarksConverted()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "ClientConv Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        // Source STEP first (Pending), then persist a browser-produced glb.
        using (var step = ModelUpload("assembly.step", "application/step"))
            (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", step)).EnsureSuccessStatusCode();

        using var glb = GlbUpload();
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/converted", glb);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.Converted, model!.ConversionStatus);
        Assert.True(model.HasRenderableModel);
    }

    [Fact]
    public async Task UploadConverted_WithoutSourceModel_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "NoSource Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using var glb = GlbUpload();
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/converted", glb);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UploadConverted_UnknownWorkstation_Returns404()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Unknown WS Plan");

        using var glb = GlbUpload();
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{Guid.NewGuid()}/model/converted", glb);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DownloadConverted_AfterClientConversion_ServesGlb()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "DownloadConv Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using (var step = ModelUpload("assembly.step", "application/step"))
            (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", step)).EnsureSuccessStatusCode();
        using (var glb = GlbUpload())
            (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/converted", glb)).EnsureSuccessStatusCode();

        var dl = await Client.GetAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/download?converted=true");
        dl.EnsureSuccessStatusCode();
        var bytes = await dl.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task UploadConverted_ReplacesPriorConvertedArtifact()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Replace Conv Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");

        using (var step = ModelUpload("assembly.step", "application/step"))
            (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", step)).EnsureSuccessStatusCode();

        using (var glb1 = GlbUpload())
            (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/converted", glb1)).EnsureSuccessStatusCode();
        // Second conversion should overwrite cleanly and remain Converted.
        using var glb2 = GlbUpload();
        var resp = await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/converted", glb2);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(ModelConversionStatus.Converted, model!.ConversionStatus);
    }

    [Fact]
    public async Task UpdateModelTransform_PersistsScaleAndOffsets()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Transform Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");
        using var content = ModelUpload("machine.glb", "model/gltf-binary");
        (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content)).EnsureSuccessStatusCode();

        var resp = await Client.PutAsJsonAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model/transform",
            new FloorPlanWorkstationModelTransformDto(Scale: 2.5, Yaw: 90, OffsetX: 10, OffsetY: 20, OffsetZ: 5), JsonOptions);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<FloorPlanWorkstationModelDto>(JsonOptions);
        Assert.Equal(2.5, model!.Scale);
        Assert.Equal(90, model.Yaw);
        Assert.Equal(10, model.OffsetX);
    }

    [Fact]
    public async Task DeleteModel_ClearsModelState()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Del Model Plan");
        var wsId = await CreateWorkstation(fpId, "ws-1");
        using var content = ModelUpload("machine.glb", "model/gltf-binary");
        (await Client.PostAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model", content)).EnsureSuccessStatusCode();

        var del = await Client.DeleteAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/model");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fpId}", JsonOptions);
        var ws = detail!.Workstations.First(w => w.PlacementId == "ws-1");
        Assert.Null(ws.Model); // model omitted once status is None
    }

    // ──────────── Designations + designed flow ────────────

    [Fact]
    public async Task Designation_AddAndAppearsInDetail()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Desig Plan");
        var loc = await CreateStorageLocation($"SL-{pfx}", "Rack");
        var (kind, _) = await CreateKindWithGrade($"K-{pfx}", "Widget");

        var addLoc = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations",
            new FloorPlanInventoryLocationCreateDto("inv-1", loc.Id), JsonOptions);
        var locId = (await addLoc.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        var resp = await Client.PostAsJsonAsync(
            $"/api/floor-plans/{fpId}/inventory-locations/{locId}/designations",
            new FloorPlanLocationDesignationCreateDto(kind.Id), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fpId}", JsonOptions);
        var invLoc = detail!.InventoryLocations.Single();
        Assert.Single(invLoc.DesignatedKinds);
        Assert.Equal(kind.Id, invLoc.DesignatedKinds[0].KindId);
    }

    [Fact]
    public async Task Designation_Duplicate_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fpId = await CreateFloorPlan($"FP-{pfx}", "Dup Desig");
        var loc = await CreateStorageLocation($"SL-{pfx}", "Rack");
        var (kind, _) = await CreateKindWithGrade($"K-{pfx}", "Widget");
        var addLoc = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations",
            new FloorPlanInventoryLocationCreateDto("inv-1", loc.Id), JsonOptions);
        var locId = (await addLoc.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations/{locId}/designations",
            new FloorPlanLocationDesignationCreateDto(kind.Id), JsonOptions);
        var dup = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations/{locId}/designations",
            new FloorPlanLocationDesignationCreateDto(kind.Id), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
    }

    [Fact]
    public async Task DesignedFlow_ReturnsRouteEvenWithoutStock_WhileLiveDoesNot()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Process consuming a material Kind.
        var (kind, grade) = await CreateKindWithGrade($"MAT-{pfx}", "Bar Stock", isSerialized: true);
        var step = await CreateTransformStep($"ST-{pfx}", "Cut", kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"PR-{pfx}", "Cutting");
        await AddProcessStep(process.Id, step.Id, 1);

        var fpId = await CreateFloorPlan($"FP-{pfx}", "Designed Flow Plan");
        var layout = """
        {
            "canvasWidth": 50000, "canvasHeight": 30000, "gridSize": 500,
            "elements": [
                { "id": "ws-1", "type": "Workstation", "label": "Cut Cell", "x": 20000, "y": 15000, "width": 3000, "height": 2000 },
                { "id": "inv-1", "type": "InventoryLocation", "label": "Bar Rack", "x": 5000, "y": 5000, "width": 2000, "height": 1000 }
            ]
        }
        """;
        await Client.PutAsJsonAsync($"/api/floor-plans/{fpId}/layout", new FloorPlanLayoutSaveDto(layout), JsonOptions);

        var wsId = await CreateWorkstation(fpId, "ws-1");
        await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/workstations/{wsId}/processes",
            new FloorPlanWorkstationProcessCreateDto(process.Id), JsonOptions);

        var loc = await CreateStorageLocation($"SL-{pfx}", "Bar Rack");
        var addLoc = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations",
            new FloorPlanInventoryLocationCreateDto("inv-1", loc.Id), JsonOptions);
        var locId = (await addLoc.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        // No stock anywhere. Live mode → unresolved.
        var liveResp = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/analyse-material-flow",
            new MaterialFlowRequestDto(MaterialFlowMode.Live), JsonOptions);
        var live = await liveResp.Content.ReadFromJsonAsync<MaterialFlowResultDto>(JsonOptions);
        Assert.Empty(live!.Flows);
        Assert.Single(live.Unresolved);

        // Designate the location to supply the Kind. Designed mode → a flow, despite no stock.
        await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/inventory-locations/{locId}/designations",
            new FloorPlanLocationDesignationCreateDto(kind.Id), JsonOptions);

        var designedResp = await Client.PostAsJsonAsync($"/api/floor-plans/{fpId}/analyse-material-flow",
            new MaterialFlowRequestDto(MaterialFlowMode.Designed), JsonOptions);
        var designed = await designedResp.Content.ReadFromJsonAsync<MaterialFlowResultDto>(JsonOptions);

        Assert.Equal(MaterialFlowMode.Designed, designed!.Mode);
        var flow = Assert.Single(designed.Flows);
        Assert.Equal(kind.Id, flow.KindId);
        Assert.Equal(0, flow.OnHandQuantity);
        Assert.True(designed.TotalTravelDistanceMm > 0);
        Assert.Empty(designed.Unresolved);
    }
}

/// <summary>Fake converter that always "succeeds" — used to test the success path.</summary>
internal sealed class FakeStepConversionService : IStepConversionService
{
    public Task<StepConversionResult> ConvertToGlbAsync(string sourceStorageKey, CancellationToken ct = default)
    {
        var converted = System.IO.Path.GetFileNameWithoutExtension(sourceStorageKey) + ".glb";
        return Task.FromResult(StepConversionResult.Ok($"floorplan-models/{converted}"));
    }
}
