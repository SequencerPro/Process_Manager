using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Tests;

public class FloorPlanTests : IntegrationTestBase
{
    public FloorPlanTests(TestWebApplicationFactory factory) : base(factory) { }

    // ── CRUD Tests ──

    [Fact]
    public async Task Create_ReturnsCreatedFloorPlan()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var dto = new FloorPlanCreateDto($"FP-{pfx}", "Test Floor Plan", "A test");
        var response = await Client.PostAsJsonAsync("/api/floor-plans", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<FloorPlanSummaryDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal($"FP-{pfx}", result.Code);
        Assert.Equal("Test Floor Plan", result.Name);
        Assert.Equal(FloorPlanStatus.Draft, result.Status);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var dto = new FloorPlanCreateDto($"FP-{pfx}", "Plan 1", null);
        await Client.PostAsJsonAsync("/api/floor-plans", dto, JsonOptions);

        var response2 = await Client.PostAsJsonAsync("/api/floor-plans", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsDetailWithLayout()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var dto = new FloorPlanCreateDto($"FP-{pfx}", "Detail Plan", null);
        var createResp = await Client.PostAsJsonAsync("/api/floor-plans", dto, JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<FloorPlanSummaryDto>(JsonOptions);

        var response = await Client.GetAsync($"/api/floor-plans/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<FloorPlanDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal($"FP-{pfx}", detail.Code);
        Assert.Contains("canvasWidth", detail.LayoutJson);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/floor-plans/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Original");

        var updateDto = new FloorPlanUpdateDto("Updated Name", "New desc");
        var response = await Client.PutAsJsonAsync($"/api/floor-plans/{created.Id}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{created.Id}", JsonOptions);
        Assert.Equal("Updated Name", detail!.Name);
        Assert.Equal("New desc", detail.Description);
    }

    [Fact]
    public async Task SaveLayout_IncrementsVersion()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Layout Plan");
        Assert.Equal(1, created.Version);

        var layout = new FloorPlanLayoutSaveDto("""{"canvasWidth":60000,"canvasHeight":40000,"gridSize":1000,"elements":[]}""");
        var response = await Client.PutAsJsonAsync($"/api/floor-plans/{created.Id}/layout", layout, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{created.Id}", JsonOptions);
        Assert.Equal(2, detail!.Version);
        Assert.Contains("60000", detail.LayoutJson);
    }

    [Fact]
    public async Task Delete_SoftDeletesSetsInactive()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Delete Me");

        var response = await Client.DeleteAsync($"/api/floor-plans/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{created.Id}", JsonOptions);
        Assert.False(detail!.IsActive);
    }

    // ── Status Transitions ──

    [Fact]
    public async Task Publish_TransitionsDraftToPublished()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Publish Me");

        var response = await Client.PostAsync($"/api/floor-plans/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{created.Id}", JsonOptions);
        Assert.Equal(FloorPlanStatus.Published, detail!.Status);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Already Pub");
        await Client.PostAsync($"/api/floor-plans/{created.Id}/publish", null);

        var response = await Client.PostAsync($"/api/floor-plans/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Archive_TransitionsPublishedToArchived()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Archive Me");
        await Client.PostAsync($"/api/floor-plans/{created.Id}/publish", null);

        var response = await Client.PostAsync($"/api/floor-plans/{created.Id}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{created.Id}", JsonOptions);
        Assert.Equal(FloorPlanStatus.Archived, detail!.Status);
    }

    [Fact]
    public async Task Archive_DraftPlan_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Draft Archive");

        var response = await Client.PostAsync($"/api/floor-plans/{created.Id}/archive", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SaveLayout_ArchivedPlan_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var created = await CreateFloorPlan($"FP-{pfx}", "Archived Layout");
        await Client.PostAsync($"/api/floor-plans/{created.Id}/publish", null);
        await Client.PostAsync($"/api/floor-plans/{created.Id}/archive", null);

        var layout = new FloorPlanLayoutSaveDto("""{"canvasWidth":1,"canvasHeight":1,"gridSize":1,"elements":[]}""");
        var response = await Client.PutAsJsonAsync($"/api/floor-plans/{created.Id}/layout", layout, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Workstation Tests ──

    [Fact]
    public async Task AddWorkstation_LinksToFloorPlan()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "WS Plan");

        var wsDto = new FloorPlanWorkstationCreateDto("ws-1", null, null, null);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations", wsDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        Assert.Single(detail!.Workstations);
        Assert.Equal("ws-1", detail.Workstations[0].PlacementId);
    }

    [Fact]
    public async Task AddWorkstation_DuplicatePlacement_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Dup WS");

        var wsDto = new FloorPlanWorkstationCreateDto("ws-1", null, null, null);
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations", wsDto, JsonOptions);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations", wsDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkstation_RemovesFromPlan()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Del WS");

        var wsDto = new FloorPlanWorkstationCreateDto("ws-del", null, null, null);
        var createResp = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations", wsDto, JsonOptions);
        var wsResult = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var wsId = wsResult.GetProperty("id").GetGuid();

        var response = await Client.DeleteAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        Assert.Empty(detail!.Workstations);
    }

    // ── Workstation Process Tests ──

    [Fact]
    public async Task AddProcessToWorkstation_Success()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Proc WS");
        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-proc");

        var process = await CreateProcess($"PROC-{pfx}", "Test Process");
        var procDto = new FloorPlanWorkstationProcessCreateDto(process.Id);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/processes", procDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        var ws = detail!.Workstations.First(w => w.PlacementId == "ws-proc");
        Assert.Single(ws.Processes);
        Assert.Equal(process.Id, ws.Processes[0].ProcessId);
    }

    [Fact]
    public async Task AddDuplicateProcess_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Dup Proc");
        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-dup-proc");

        var process = await CreateProcess($"PROC-{pfx}", "Dup Process");
        var procDto = new FloorPlanWorkstationProcessCreateDto(process.Id);
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/processes", procDto, JsonOptions);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/processes", procDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Workstation Tool Tests ──

    [Fact]
    public async Task AddToolToWorkstation_Success()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Tool WS");
        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-tool");

        var kind = await CreateKind($"TOOL-{pfx}", "Torque Wrench");
        var toolDto = new FloorPlanWorkstationToolCreateDto(kind.Id, 2, "Calibrated");
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/tools", toolDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        var ws = detail!.Workstations.First(w => w.PlacementId == "ws-tool");
        Assert.Single(ws.Tools);
        Assert.Equal(2, ws.Tools[0].Quantity);
        Assert.Equal("Calibrated", ws.Tools[0].Notes);
    }

    [Fact]
    public async Task UpdateTool_ChangesQuantityAndNotes()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Upd Tool");
        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-upd-tool");

        var kind = await CreateKind($"TOOL-{pfx}", "Caliper");
        var toolDto = new FloorPlanWorkstationToolCreateDto(kind.Id, 1, null);
        var createResp = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/tools", toolDto, JsonOptions);
        var toolResult = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var toolId = toolResult.GetProperty("id").GetGuid();

        var updateDto = new FloorPlanWorkstationToolUpdateDto(5, "Updated note");
        var response = await Client.PutAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/tools/{toolId}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        var ws = detail!.Workstations.First(w => w.PlacementId == "ws-upd-tool");
        Assert.Equal(5, ws.Tools[0].Quantity);
        Assert.Equal("Updated note", ws.Tools[0].Notes);
    }

    // ── Inventory Location Tests ──

    [Fact]
    public async Task AddInventoryLocation_LinksToStorageLocation()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Inv Loc Plan");
        var loc = await CreateStorageLocation($"LOC-{pfx}", "Raw Materials Rack");

        var locDto = new FloorPlanInventoryLocationCreateDto("inv-1", loc.Id);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/inventory-locations", locDto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await Client.GetFromJsonAsync<FloorPlanDetailDto>($"/api/floor-plans/{fp.Id}", JsonOptions);
        Assert.Single(detail!.InventoryLocations);
        Assert.Equal($"LOC-{pfx}", detail.InventoryLocations[0].StorageLocationCode);
    }

    [Fact]
    public async Task AddDuplicateInventoryLocation_Returns400()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp = await CreateFloorPlan($"FP-{pfx}", "Dup Inv");
        var loc = await CreateStorageLocation($"LOC-{pfx}", "Dup Loc");

        var locDto = new FloorPlanInventoryLocationCreateDto("inv-1", loc.Id);
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/inventory-locations", locDto, JsonOptions);
        var locDto2 = new FloorPlanInventoryLocationCreateDto("inv-2", loc.Id);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/inventory-locations", locDto2, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Material Flow Analysis ──

    [Fact]
    public async Task MaterialFlowAnalysis_ReturnsFlowsAndUnresolved()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Create a Kind + Grade + Process with material input
        var (kind, grade) = await CreateKindWithGrade($"MAT-{pfx}", "Raw Material", isSerialized: true);
        var step = await CreateTransformStep($"STEP-{pfx}", "Machine Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"PROC-{pfx}", "Machining");
        await AddProcessStep(process.Id, step.Id, 1);

        // Create floor plan with workstation and inventory location
        var fp = await CreateFloorPlan($"FP-{pfx}", "Flow Plan");

        // Save layout with positioned elements
        var layoutJson = """
        {
            "canvasWidth": 50000, "canvasHeight": 30000, "gridSize": 500,
            "elements": [
                { "id": "ws-1", "type": "Workstation", "label": "Machining Cell", "x": 10000, "y": 10000, "width": 3000, "height": 2000 },
                { "id": "inv-1", "type": "InventoryLocation", "label": "Raw Rack", "x": 1000, "y": 1000, "width": 2000, "height": 1000 }
            ]
        }
        """;
        await Client.PutAsJsonAsync($"/api/floor-plans/{fp.Id}/layout", new FloorPlanLayoutSaveDto(layoutJson), JsonOptions);

        // Link workstation and add process
        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-1");
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/processes",
            new FloorPlanWorkstationProcessCreateDto(process.Id), JsonOptions);

        // Link inventory location
        var storageLoc = await CreateStorageLocation($"SL-{pfx}", "Raw Materials");
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/inventory-locations",
            new FloorPlanInventoryLocationCreateDto("inv-1", storageLoc.Id), JsonOptions);

        // Analyse — no stock, so should be unresolved
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/analyse-material-flow",
            new MaterialFlowRequestDto(), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MaterialFlowResultDto>(JsonOptions);
        Assert.NotNull(result);
        // No items in stock, so material should be unresolved
        Assert.Single(result.Unresolved);
        Assert.Equal(kind.Id, result.Unresolved[0].KindId);
    }

    [Fact]
    public async Task MaterialFlowAnalysis_WithStock_ReturnsFlows()
    {
        var pfx = Guid.NewGuid().ToString()[..6];

        // Create Kind + Grade + Process
        var (kind, grade) = await CreateKindWithGrade($"MAT-{pfx}", "Aluminium Bar", isSerialized: true);
        var step = await CreateTransformStep($"STEP-{pfx}", "Cut Step",
            kind.Id, grade.Id, kind.Id, grade.Id);
        var process = await CreateProcess($"PROC-{pfx}", "Cutting");
        await AddProcessStep(process.Id, step.Id, 1);
        await ReleaseProcess(process.Id);

        // Create a job + item at a storage location (to have stock)
        var storageLoc = await CreateStorageLocation($"SL-{pfx}", "Raw Store");
        var job = await CreateJob(process.Id, $"JOB-{pfx}", "Stock Job");
        var item = await CreateItem(job.Id, kind.Id, grade.Id, $"SN-{pfx}");

        // Move item to storage location via inventory transaction
        var txDto = new CreateInventoryTransactionDto("Receipt", item.Id, ToLocationId: storageLoc.Id);
        await Client.PostAsJsonAsync("/api/warehouse/transactions", txDto, JsonOptions);

        // Create floor plan
        var fp = await CreateFloorPlan($"FP-{pfx}", "Stock Flow Plan");
        var layoutJson = $$"""
        {
            "canvasWidth": 50000, "canvasHeight": 30000, "gridSize": 500,
            "elements": [
                { "id": "ws-1", "type": "Workstation", "label": "Cut Cell", "x": 20000, "y": 15000, "width": 3000, "height": 2000 },
                { "id": "inv-1", "type": "InventoryLocation", "label": "Raw Store", "x": 5000, "y": 5000, "width": 2000, "height": 1000 }
            ]
        }
        """;
        await Client.PutAsJsonAsync($"/api/floor-plans/{fp.Id}/layout", new FloorPlanLayoutSaveDto(layoutJson), JsonOptions);

        var wsId = await CreateFloorPlanWorkstation(fp.Id, "ws-1");
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/workstations/{wsId}/processes",
            new FloorPlanWorkstationProcessCreateDto(process.Id), JsonOptions);
        await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/inventory-locations",
            new FloorPlanInventoryLocationCreateDto("inv-1", storageLoc.Id), JsonOptions);

        // Analyse — should find the flow
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{fp.Id}/analyse-material-flow",
            new MaterialFlowRequestDto(), JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<MaterialFlowResultDto>(JsonOptions);

        Assert.NotNull(result);
        Assert.Single(result.Flows);
        Assert.Equal(kind.Id, result.Flows[0].KindId);
        Assert.True(result.Flows[0].DistanceMm > 0);
        Assert.Empty(result.Unresolved);
    }

    // ── List / Filter Tests ──

    [Fact]
    public async Task GetAll_SearchFilter_ReturnsMatches()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        await CreateFloorPlan($"FP-ALPHA-{pfx}", "Alpha Hall");
        await CreateFloorPlan($"FP-BETA-{pfx}", "Beta Hall");

        var response = await Client.GetAsync($"/api/floor-plans?search=ALPHA-{pfx}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
    }

    [Fact]
    public async Task GetAll_StatusFilter_ReturnsMatches()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var fp1 = await CreateFloorPlan($"FP-D-{pfx}", "Draft Plan");
        var fp2 = await CreateFloorPlan($"FP-P-{pfx}", "Published Plan");
        await Client.PostAsync($"/api/floor-plans/{fp2.Id}/publish", null);

        var response = await Client.GetAsync($"/api/floor-plans?search={pfx}&status=Published");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Contains("Published", items[0].GetProperty("status").GetString());
    }

    // ── Helper Methods ──

    private async Task<FloorPlanSummaryDto> CreateFloorPlan(string code, string name)
    {
        var dto = new FloorPlanCreateDto(code, name, null);
        var response = await Client.PostAsJsonAsync("/api/floor-plans", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FloorPlanSummaryDto>(JsonOptions))!;
    }

    private async Task<Guid> CreateFloorPlanWorkstation(Guid floorPlanId, string placementId)
    {
        var dto = new FloorPlanWorkstationCreateDto(placementId, null, null, null);
        var response = await Client.PostAsJsonAsync($"/api/floor-plans/{floorPlanId}/workstations", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return result.GetProperty("id").GetGuid();
    }

    private async Task<StorageLocationResponseDto> CreateStorageLocation(string code, string name)
    {
        var dto = new CreateStorageLocationDto(code, name);
        var response = await Client.PostAsJsonAsync("/api/warehouse/locations", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StorageLocationResponseDto>(JsonOptions))!;
    }
}
