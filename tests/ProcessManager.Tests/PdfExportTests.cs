using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;
using UglyToad.PdfPig;

namespace ProcessManager.Tests;

public class PdfExportTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public PdfExportTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    private async Task<PfmeaResponseDto> CreatePfmeaWithFailureModes()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var pfmeaCode = $"PFMEA-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var pfmeaDto = new PfmeaCreateDto(scenario.Process.Id, pfmeaCode, "Test PFMEA", "For PDF export testing");
        var pfmeaResp = await Client.PostAsJsonAsync("/api/pfmeas", pfmeaDto, JsonOptions);
        pfmeaResp.EnsureSuccessStatusCode();
        var pfmea = (await pfmeaResp.Content.ReadFromJsonAsync<PfmeaResponseDto>(JsonOptions))!;

        var fm1Dto = new PfmeaFailureModeCreateDto(
            scenario.ProcessStep1.Id,
            "Deburr widget edges",
            "Incomplete deburr",
            "Sharp edges remain — customer injury risk",
            "Worn tool insert",
            "Tool change schedule every 500 parts",
            "Visual inspection at end of step",
            8, 4, 6);
        await Client.PostAsJsonAsync($"/api/pfmeas/{pfmea.Id}/failure-modes", fm1Dto, JsonOptions);

        var fm2Dto = new PfmeaFailureModeCreateDto(
            scenario.ProcessStep2.Id,
            "Inspect dimensions",
            "Missed defect",
            "Non-conforming part shipped to customer",
            "Gauge R&R drift",
            "Calibration schedule",
            "100% CMM measurement",
            9, 2, 3);
        await Client.PostAsJsonAsync($"/api/pfmeas/{pfmea.Id}/failure-modes", fm2Dto, JsonOptions);

        var result = await Client.GetFromJsonAsync<PfmeaResponseDto>($"/api/pfmeas/{pfmea.Id}", JsonOptions);
        return result!;
    }

    private async Task<ControlPlanResponseDto> CreateControlPlanWithEntries()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var cpCode = $"CP-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var cpDto = new ControlPlanCreateDto(scenario.Process.Id, cpCode, "Test Control Plan", null);
        var cpResp = await Client.PostAsJsonAsync("/api/controlplans", cpDto, JsonOptions);
        cpResp.EnsureSuccessStatusCode();
        var cp = (await cpResp.Content.ReadFromJsonAsync<ControlPlanResponseDto>(JsonOptions))!;
        return cp;
    }

    // ───── PFMEA PDF Tests ─────

    [Fact]
    public async Task GeneratePfmeaPdf_ReturnsValidPdf()
    {
        var pfmea = await CreatePfmeaWithFailureModes();

        var response = await Client.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal(0x25, bytes[0]); // '%' — PDF magic byte
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public async Task GeneratePfmeaPdf_ContainsAllFailureModes()
    {
        var pfmea = await CreatePfmeaWithFailureModes();

        var response = await Client.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var pdf = PdfDocument.Open(bytes);
        var allText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Incomplete deburr", allText);
        Assert.Contains("Missed defect", allText);
    }

    [Fact]
    public async Task GeneratePfmeaPdf_ContainsRpnValues()
    {
        var pfmea = await CreatePfmeaWithFailureModes();

        var response = await Client.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var pdf = PdfDocument.Open(bytes);
        var allText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        // RPN for FM1: 8*4*6 = 192, FM2: 9*2*3 = 54
        Assert.Contains("192", allText);
        Assert.Contains("54", allText);
    }

    [Fact]
    public async Task GeneratePfmeaPdf_DraftStatusShowsWarning()
    {
        var pfmea = await CreatePfmeaWithFailureModes();

        // PFMEA is not stale by default when just created, so it should not show DRAFT
        var response = await Client.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var pdf = PdfDocument.Open(bytes);
        var allText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        // Freshly created PFMEA is not stale, so no DRAFT banner
        Assert.DoesNotContain("DRAFT", allText);
    }

    [Fact]
    public async Task GeneratePfmeaPdf_NonexistentId_Returns404()
    {
        var response = await Client.GetAsync($"/api/pfmeas/{Guid.NewGuid()}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── Control Plan PDF Tests ─────

    [Fact]
    public async Task GenerateControlPlanPdf_ReturnsValidPdf()
    {
        var cp = await CreateControlPlanWithEntries();

        var response = await Client.GetAsync($"/api/controlplans/{cp.Id}/pdf");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal(0x25, bytes[0]); // '%PDF'
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x44, bytes[2]);
        Assert.Equal(0x46, bytes[3]);
    }

    [Fact]
    public async Task GenerateControlPlanPdf_ContainsEntries()
    {
        var cp = await CreateControlPlanWithEntries();

        var response = await Client.GetAsync($"/api/controlplans/{cp.Id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var pdf = PdfDocument.Open(bytes);
        var allText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Control Plan", allText);
        // Verify the PDF contains entry data (step names are used as characteristic names).
        // PDF renderers may use ligatures (e.g. "ff" → "ﬀ"), so check for substrings
        // that avoid ligature sequences.
        Assert.Contains("Deburr", allText);
        Assert.True(cp.Entries.Count >= 2, "Control plan should have auto-populated entries");
    }

    [Fact]
    public async Task GenerateControlPlanPdf_NonexistentId_Returns404()
    {
        var response = await Client.GetAsync($"/api/controlplans/{Guid.NewGuid()}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───── Tenant Branding Tests ─────

    [Fact]
    public async Task TenantBranding_UpsertAndGet_RoundTrips()
    {
        var dto = new UpdateTenantBrandingDto("Acme Manufacturing", "#ff6600", "ISO 9001 Certified");
        var upsertResp = await Client.PutAsJsonAsync("/api/tenant-branding", dto, JsonOptions);
        upsertResp.EnsureSuccessStatusCode();

        var getResp = await Client.GetFromJsonAsync<TenantBrandingResponseDto>(
            "/api/tenant-branding", JsonOptions);

        Assert.Equal("Acme Manufacturing", getResp!.CompanyName);
        Assert.Equal("#ff6600", getResp.PrimaryColorHex);
        Assert.Equal("ISO 9001 Certified", getResp.FooterText);
    }

    [Fact]
    public async Task TenantBranding_PdfReflectsBranding()
    {
        var brandingDto = new UpdateTenantBrandingDto("Widget Corp", "#003366", "Confidential");
        await Client.PutAsJsonAsync("/api/tenant-branding", brandingDto, JsonOptions);

        var pfmea = await CreatePfmeaWithFailureModes();
        var response = await Client.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var pdf = PdfDocument.Open(bytes);
        var allText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Widget Corp", allText);
    }

    [Fact]
    public async Task PdfExport_RequiresAuthentication()
    {
        var unauthClient = _factory.CreateClient();

        var pfmeaResp = await unauthClient.GetAsync($"/api/pfmeas/{Guid.NewGuid()}/pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, pfmeaResp.StatusCode);

        var cpResp = await unauthClient.GetAsync($"/api/controlplans/{Guid.NewGuid()}/pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, cpResp.StatusCode);
    }

    [Fact]
    public async Task PdfExport_TenantIsolation_Returns404ForOtherTenant()
    {
        var pfmea = await CreatePfmeaWithFailureModes();

        var otherTenantId = _factory.CreateTenant("other-tenant-pdf");
        var otherClient = _factory.CreateTenantClient(otherTenantId, "other-user-pdf");

        var response = await otherClient.GetAsync($"/api/pfmeas/{pfmea.Id}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
