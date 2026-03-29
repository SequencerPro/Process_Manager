using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Tests;

public class KindTests : IntegrationTestBase
{
    public KindTests(TestWebApplicationFactory factory) : base(factory) { }

    // ──────────── GET ────────────

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/kinds");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<KindResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    // ──────────── CREATE ────────────

    [Fact]
    public async Task Create_ValidKind_ReturnsCreated()
    {
        var kind = await CreateKind("CRE-001", "Created Kind", "A test kind", true, false);

        Assert.Equal("CRE-001", kind.Code);
        Assert.Equal("Created Kind", kind.Name);
        Assert.Equal("A test kind", kind.Description);
        Assert.True(kind.IsSerialized);
        Assert.False(kind.IsBatchable);
        Assert.NotEqual(Guid.Empty, kind.Id);
        Assert.NotEqual(default, kind.CreatedAt);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await CreateKind("DUP-001", "First");

        var dto = new KindCreateDto("DUP-001", "Second", null, false, false);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────── GET BY ID ────────────

    [Fact]
    public async Task GetById_Existing_ReturnsKind()
    {
        var created = await CreateKind("GET-001", "Get Test");

        var response = await Client.GetAsync($"/api/kinds/{created.Id}");
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal(created.Id, kind.Id);
        Assert.Equal("GET-001", kind.Code);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/kinds/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── UPDATE ────────────

    [Fact]
    public async Task Update_ValidChanges_ReturnsUpdated()
    {
        var kind = await CreateKind("UPD-001", "Original");

        var updateDto = new KindUpdateDto("Updated Name", "New description", true, true);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{kind.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("New description", updated.Description);
        Assert.True(updated.IsSerialized);
        Assert.True(updated.IsBatchable);
        // Code should not change (not in update DTO)
        Assert.Equal("UPD-001", updated.Code);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var updateDto = new KindUpdateDto("Name", null, false, false);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{Guid.NewGuid()}", updateDto, JsonOptions);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── DELETE ────────────

    [Fact]
    public async Task Delete_Existing_ReturnsNoContent()
    {
        var kind = await CreateKind("DEL-001", "To Delete");

        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await Client.GetAsync($"/api/kinds/{kind.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/kinds/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── KIND WITH GRADES ────────────

    [Fact]
    public async Task Create_KindWithGrades_IncludesGradesInResponse()
    {
        var kind = await CreateKind("GRD-001", "Graded Kind");
        var grade1 = await CreateGrade(kind.Id, "RAW", "Raw", isDefault: true, sortOrder: 0);
        var grade2 = await CreateGrade(kind.Id, "PASS", "Passed", isDefault: false, sortOrder: 1);

        // Fetch the kind and verify grades are included
        var response = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result.Grades.Count);
        Assert.Equal("RAW", result.Grades[0].Code);
        Assert.Equal("PASS", result.Grades[1].Code);
    }

    // ──────────── EXTENDED PROPERTIES: SOURCE TYPE ────────────

    [Fact]
    public async Task Create_MakeKind_SourceTypeStored()
    {
        var dto = new KindCreateDto("SRC-001", "Make Part", null, true, false, KindSourceType.Make);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("Make", kind.SourceType);
    }

    [Fact]
    public async Task Create_BuyKind_VendorFieldsIncluded()
    {
        var dto = new KindCreateDto("SRC-002", "Purchased Part", null, false, false,
            KindSourceType.Buy, VendorName: "Acme Corp", VendorPartNumber: "ACM-99");
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("Buy", kind.SourceType);
        Assert.Equal("Acme Corp", kind.VendorName);
        Assert.Equal("ACM-99", kind.VendorPartNumber);
    }

    [Fact]
    public async Task Create_MakeKind_VendorFieldsNulledOut()
    {
        // Vendor fields should be ignored when SourceType is not Buy
        var dto = new KindCreateDto("SRC-003", "Make With Vendor", null, false, false,
            KindSourceType.Make, VendorName: "Should Be Ignored", VendorPartNumber: "IGN-001");
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("Make", kind.SourceType);
        Assert.Null(kind.VendorName);
        Assert.Null(kind.VendorPartNumber);
    }

    [Fact]
    public async Task Create_ReferenceDocumentKind_Succeeds()
    {
        var dto = new KindCreateDto("REF-001", "Assembly Drawing", "Reference spec", false, false,
            KindSourceType.ReferenceDocument);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("ReferenceDocument", kind.SourceType);
    }

    // ──────────── EXTENDED PROPERTIES: COST & PRICING ────────────

    [Fact]
    public async Task Create_WithCostAndPrice_ValuesStored()
    {
        var dto = new KindCreateDto("CST-001", "Priced Part", null, false, false,
            Cost: 12.50m, Price: 24.99m);
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal(12.50m, kind.Cost);
        Assert.Equal(24.99m, kind.Price);
    }

    // ──────────── EXTENDED PROPERTIES: ROHS ────────────

    [Fact]
    public async Task Create_WithRohsStatus_ValueStored()
    {
        var dto = new KindCreateDto("RHS-001", "RoHS Part", null, false, false,
            RohsStatus: "Compliant");
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("Compliant", kind.RohsStatus);
    }

    // ──────────── EXTENDED PROPERTIES: ALL OPTIONAL FIELDS ────────────

    [Fact]
    public async Task Create_WithAllOptionalFields_AllReturned()
    {
        var dto = new KindCreateDto("ALL-001", "Full Part", "Full description", true, true,
            KindSourceType.Buy,
            UnitOfMeasure: "Each",
            Cost: 5.00m,
            Price: 10.00m,
            VendorName: "Global Parts Inc",
            VendorPartNumber: "GP-555",
            LeadTimeDays: 14,
            Weight: 2.5m,
            WeightUnit: "kg",
            RohsStatus: "Compliant",
            CountryOfOrigin: "Germany",
            Revision: "Rev B",
            Notes: "High-reliability component");
        var response = await Client.PostAsJsonAsync("/api/kinds", dto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var kind = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(kind);
        Assert.Equal("Buy", kind.SourceType);
        Assert.Equal("Each", kind.UnitOfMeasure);
        Assert.Equal(5.00m, kind.Cost);
        Assert.Equal(10.00m, kind.Price);
        Assert.Equal("Global Parts Inc", kind.VendorName);
        Assert.Equal("GP-555", kind.VendorPartNumber);
        Assert.Equal(14, kind.LeadTimeDays);
        Assert.Equal(2.5m, kind.Weight);
        Assert.Equal("kg", kind.WeightUnit);
        Assert.Equal("Compliant", kind.RohsStatus);
        Assert.Equal("Germany", kind.CountryOfOrigin);
        Assert.Equal("Rev B", kind.Revision);
        Assert.Equal("High-reliability component", kind.Notes);
    }

    // ──────────── EXTENDED PROPERTIES: UPDATE ────────────

    [Fact]
    public async Task Update_ChangeSourceTypeBuyToMake_ClearsVendorFields()
    {
        // Create as Buy with vendor info
        var createDto = new KindCreateDto("VND-001", "Vendor Part", null, false, false,
            KindSourceType.Buy, VendorName: "Acme", VendorPartNumber: "A-100");
        var createResp = await Client.PostAsJsonAsync("/api/kinds", createDto, JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);

        // Update to Make — vendor fields should be cleared
        var updateDto = new KindUpdateDto("Vendor Part", null, false, false,
            KindSourceType.Make, VendorName: "Should Be Cleared", VendorPartNumber: "CLR-001");
        var updateResp = await Client.PutAsJsonAsync($"/api/kinds/{created!.Id}", updateDto, JsonOptions);
        updateResp.EnsureSuccessStatusCode();

        var updated = await updateResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Make", updated.SourceType);
        Assert.Null(updated.VendorName);
        Assert.Null(updated.VendorPartNumber);
    }

    [Fact]
    public async Task Update_SetCostAndPrice_ReturnsUpdated()
    {
        var kind = await CreateKind("CST-002", "Cost Update");

        var updateDto = new KindUpdateDto("Cost Update", null, false, false, Cost: 99.99m, Price: 149.99m);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{kind.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(99.99m, updated.Cost);
        Assert.Equal(149.99m, updated.Price);
    }

    // ──────────── DOCUMENTS: UPLOAD ────────────

    [Fact]
    public async Task UploadDocument_ValidFile_ReturnsDocument()
    {
        var kind = await CreateKind("DOC-001", "Doc Kind");

        // Upload a test file
        using var content = new MultipartFormDataContent();
        var fileBytes = "test file content"u8.ToArray();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "drawing.pdf");
        content.Add(new StringContent("Assembly Drawing"), "title");

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/documents", content);
        response.EnsureSuccessStatusCode();

        var doc = await response.Content.ReadFromJsonAsync<KindDocumentResponseDto>(JsonOptions);
        Assert.NotNull(doc);
        Assert.Equal("drawing.pdf", doc.OriginalFileName);
        Assert.Equal("application/pdf", doc.MimeType);
        Assert.Equal("Assembly Drawing", doc.Title);
        Assert.Equal(kind.Id, doc.KindId);
    }

    [Fact]
    public async Task UploadDocument_KindNotFound_Returns404()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("test"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "test.pdf");

        var response = await Client.PostAsync($"/api/kinds/{Guid.NewGuid()}/documents", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IncludesDocuments()
    {
        var kind = await CreateKind("DOC-002", "Doc Kind 2");

        // Upload a document
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("content"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "File", "photo.png");
        await Client.PostAsync($"/api/kinds/{kind.Id}/documents", content);

        // Fetch and verify
        var response = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Single(result.Documents);
        Assert.Equal("photo.png", result.Documents[0].OriginalFileName);
    }

    // ──────────── DOCUMENTS: DELETE ────────────

    [Fact]
    public async Task DeleteDocument_Existing_ReturnsNoContent()
    {
        var kind = await CreateKind("DOC-003", "Doc Delete Kind");

        // Upload
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("data"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "spec.pdf");
        var uploadResp = await Client.PostAsync($"/api/kinds/{kind.Id}/documents", content);
        var doc = await uploadResp.Content.ReadFromJsonAsync<KindDocumentResponseDto>(JsonOptions);

        // Delete
        var deleteResp = await Client.DeleteAsync($"/api/kinds/{kind.Id}/documents/{doc!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verify gone
        var getResp = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await getResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.Empty(result!.Documents);
    }

    [Fact]
    public async Task DeleteDocument_NonExistent_Returns404()
    {
        var kind = await CreateKind("DOC-004", "Doc 404 Kind");
        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── DEFAULT SOURCE TYPE ────────────

    [Fact]
    public async Task Create_DefaultSourceType_IsMake()
    {
        var kind = await CreateKind("DFT-001", "Default Source");
        Assert.Equal("Make", kind.SourceType);
    }

    // ──────────── DOCUMENTS INCLUDED IN RESPONSE ────────────

    [Fact]
    public async Task Create_NewKind_HasEmptyDocumentsList()
    {
        var kind = await CreateKind("DOC-005", "Empty Docs Kind");
        Assert.NotNull(kind.Documents);
        Assert.Empty(kind.Documents);
    }

    // ──────────── 3D MODEL: UPLOAD ────────────

    [Fact]
    public async Task UploadModel_ValidStl_ReturnsKindWithModel()
    {
        var kind = await CreateKind("MDL-001", "Model Kind");

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 }; // fake STL
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", "part.stl");

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("part.stl", updated.ModelOriginalFileName);
        Assert.NotNull(updated.ModelFileName);
    }

    [Fact]
    public async Task UploadModel_UnsupportedFormat_Returns400()
    {
        var kind = await CreateKind("MDL-002", "Bad Format Kind");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", "model.fbx");

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadModel_KindNotFound_Returns404()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", "part.stl");

        var response = await Client.PostAsync($"/api/kinds/{Guid.NewGuid()}/model", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadModel_GlbFormat_Succeeds()
    {
        var kind = await CreateKind("MDL-003", "GLB Kind");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("model/gltf-binary");
        content.Add(fileContent, "File", "part.glb");

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.Equal("part.glb", updated!.ModelOriginalFileName);
    }

    // ──────────── 3D MODEL: DELETE ────────────

    [Fact]
    public async Task DeleteModel_Existing_ReturnsNoContent()
    {
        var kind = await CreateKind("MDL-004", "Delete Model Kind");

        // Upload
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", "part.obj");
        await Client.PostAsync($"/api/kinds/{kind.Id}/model", content);

        // Delete
        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/model");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify cleared
        var getResp = await Client.GetAsync($"/api/kinds/{kind.Id}");
        var result = await getResp.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.Null(result!.ModelFileName);
        Assert.Null(result.ModelOriginalFileName);
    }

    [Fact]
    public async Task DeleteModel_NoModelAttached_Returns404()
    {
        var kind = await CreateKind("MDL-005", "No Model Kind");
        var response = await Client.DeleteAsync($"/api/kinds/{kind.Id}/model");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────── 3D MODEL: REPLACE ────────────

    [Fact]
    public async Task UploadModel_ReplacesExisting()
    {
        var kind = await CreateKind("MDL-006", "Replace Model Kind");

        // Upload first
        using var content1 = new MultipartFormDataContent();
        var fc1 = new ByteArrayContent(new byte[] { 0x01 });
        fc1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content1.Add(fc1, "File", "v1.stl");
        await Client.PostAsync($"/api/kinds/{kind.Id}/model", content1);

        // Upload replacement
        using var content2 = new MultipartFormDataContent();
        var fc2 = new ByteArrayContent(new byte[] { 0x02, 0x03 });
        fc2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content2.Add(fc2, "File", "v2.obj");
        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content2);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.Equal("v2.obj", result!.ModelOriginalFileName);
    }

    [Fact]
    public async Task Create_NewKind_HasNoModel()
    {
        var kind = await CreateKind("MDL-007", "No Model Default");
        Assert.Null(kind.ModelFileName);
        Assert.Null(kind.ModelOriginalFileName);
        Assert.Null(kind.ModelMimeType);
    }

    // ──────────── 3D MODEL: STEP/IGES FORMAT SUPPORT ────────────

    [Theory]
    [InlineData("assembly.step", "model/step")]
    [InlineData("bracket.stp", "model/step")]
    [InlineData("housing.iges", "model/iges")]
    [InlineData("flange.igs", "model/iges")]
    public async Task UploadModel_StepIgesFormats_Succeeds(string fileName, string mimeType)
    {
        var code = $"MDL-STEP-{fileName.Replace(".", "")}";
        var kind = await CreateKind(code, $"STEP/IGES Kind {fileName}");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x49, 0x53, 0x4F });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "File", fileName);

        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(fileName, updated.ModelOriginalFileName);
        Assert.NotNull(updated.ModelFileName);
    }

    [Fact]
    public async Task UploadModel_ReplaceStlWithStep_Succeeds()
    {
        var kind = await CreateKind("MDL-REPLACE-STEP", "Replace STL with STEP");

        // Upload STL first
        using var content1 = new MultipartFormDataContent();
        var fc1 = new ByteArrayContent(new byte[] { 0x01 });
        fc1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content1.Add(fc1, "File", "v1.stl");
        await Client.PostAsync($"/api/kinds/{kind.Id}/model", content1);

        // Replace with STEP
        using var content2 = new MultipartFormDataContent();
        var fc2 = new ByteArrayContent(new byte[] { 0x49, 0x53, 0x4F });
        fc2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("model/step");
        content2.Add(fc2, "File", "v2.step");
        var response = await Client.PostAsync($"/api/kinds/{kind.Id}/model", content2);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<KindResponseDto>(JsonOptions);
        Assert.Equal("v2.step", result!.ModelOriginalFileName);
    }

    // ──────────── BILL OF MATERIALS ────────────

    [Fact]
    public async Task CreateBomLine_ValidComponent_ReturnsCreated()
    {
        var assembly = await CreateKind("BOM-ASM-001", "Assembly");
        var component = await CreateKind("BOM-CMP-001", "Component", isSerialized: false);

        var bomLine = await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 5);

        Assert.Equal(assembly.Id, bomLine.ParentKindId);
        Assert.Equal(component.Id, bomLine.ComponentKindId);
        Assert.Equal(component.Code, bomLine.ComponentCode);
        Assert.Equal(component.Name, bomLine.ComponentName);
        Assert.Equal(1, bomLine.LineNumber);
        Assert.Equal(5m, bomLine.Quantity);
    }

    [Fact]
    public async Task CreateBomLine_SelfReference_ReturnsConflict()
    {
        var kind = await CreateKind("BOM-SELF-001", "Self Ref Kind");

        var dto = new BomLineCreateDto(kind.Id, 1, 1);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{kind.Id}/bom", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBomLine_DuplicateComponent_ReturnsConflict()
    {
        var assembly = await CreateKind("BOM-DUP-001", "Assembly Dup");
        var component = await CreateKind("BOM-DUP-002", "Component Dup");

        await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 2);

        var dto = new BomLineCreateDto(component.Id, 2, 3);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{assembly.Id}/bom", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBomLine_InvalidComponentId_ReturnsNotFound()
    {
        var assembly = await CreateKind("BOM-INV-001", "Assembly Invalid");

        var dto = new BomLineCreateDto(Guid.NewGuid(), 1, 1);
        var response = await Client.PostAsJsonAsync($"/api/kinds/{assembly.Id}/bom", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBomLine_ValidChanges_ReturnsUpdated()
    {
        var assembly = await CreateKind("BOM-UPD-001", "Assembly Upd");
        var component = await CreateKind("BOM-UPD-002", "Component Upd");

        var bomLine = await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 2);

        var updateDto = new BomLineUpdateDto(10, 25, "Kg", "Updated notes", 5);
        var response = await Client.PutAsJsonAsync($"/api/kinds/{assembly.Id}/bom/{bomLine.Id}", updateDto, JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<BomLineResponseDto>(JsonOptions);
        Assert.Equal(10, updated!.LineNumber);
        Assert.Equal(25m, updated.Quantity);
        Assert.Equal("Kg", updated.UnitOfMeasure);
        Assert.Equal("Updated notes", updated.Notes);
    }

    [Fact]
    public async Task DeleteBomLine_Existing_ReturnsNoContent()
    {
        var assembly = await CreateKind("BOM-DEL-001", "Assembly Del");
        var component = await CreateKind("BOM-DEL-002", "Component Del");

        var bomLine = await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 1);

        var response = await Client.DeleteAsync($"/api/kinds/{assembly.Id}/bom/{bomLine.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify line no longer in Kind response
        var kindResponse = await Client.GetFromJsonAsync<KindResponseDto>($"/api/kinds/{assembly.Id}", JsonOptions);
        Assert.Empty(kindResponse!.BomLines);
    }

    [Fact]
    public async Task GetKind_IncludesBomLines()
    {
        var assembly = await CreateKind("BOM-GET-001", "Assembly Get");
        var comp1 = await CreateKind("BOM-GET-002", "Component A");
        var comp2 = await CreateKind("BOM-GET-003", "Component B");

        await CreateBomLine(assembly.Id, comp1.Id, lineNumber: 1, quantity: 3);
        await CreateBomLine(assembly.Id, comp2.Id, lineNumber: 2, quantity: 7);

        var kind = await Client.GetFromJsonAsync<KindResponseDto>($"/api/kinds/{assembly.Id}", JsonOptions);
        Assert.Equal(2, kind!.BomLines.Count);
        Assert.Contains(kind.BomLines, b => b.ComponentCode == "BOM-GET-002" && b.Quantity == 3);
        Assert.Contains(kind.BomLines, b => b.ComponentCode == "BOM-GET-003" && b.Quantity == 7);
    }

    [Fact]
    public async Task DeleteKind_UsedAsComponent_ReturnsConflict()
    {
        var assembly = await CreateKind("BOM-DC-001", "Assembly DC");
        var component = await CreateKind("BOM-DC-002", "Component DC");

        await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 1);

        // Cannot delete a Kind used as component
        var response = await Client.DeleteAsync($"/api/kinds/{component.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteKind_WithBomLines_CascadesDelete()
    {
        var assembly = await CreateKind("BOM-CAS-001", "Assembly Cascade");
        var component = await CreateKind("BOM-CAS-002", "Component Cascade");

        await CreateBomLine(assembly.Id, component.Id, lineNumber: 1, quantity: 1);

        // Deleting the assembly should cascade-delete BomLines
        var response = await Client.DeleteAsync($"/api/kinds/{assembly.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Component Kind should still exist
        var compResponse = await Client.GetAsync($"/api/kinds/{component.Id}");
        Assert.Equal(HttpStatusCode.OK, compResponse.StatusCode);
    }
}
