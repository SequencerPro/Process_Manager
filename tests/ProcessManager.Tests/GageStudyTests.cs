using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class GageStudyTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public GageStudyTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateEquipmentAsync()
    {
        var catCode = $"CAT-{Guid.NewGuid():N}"[..10];
        var catResp = await _client.PostAsJsonAsync("/api/equipment/categories",
            new { Code = catCode, Name = $"Cat {catCode}" }, Json);
        catResp.EnsureSuccessStatusCode();
        var cat = await catResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var catId = cat.GetProperty("id").GetGuid();

        var eqCode = $"EQ-{Guid.NewGuid():N}"[..10];
        var eqResp = await _client.PostAsJsonAsync("/api/equipment",
            new { Code = eqCode, Name = $"Equipment {eqCode}", CategoryId = catId }, Json);
        eqResp.EnsureSuccessStatusCode();
        var eq = await eqResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        return eq.GetProperty("id").GetGuid();
    }

    private async Task<GageStudyResponseDto> CreateStudyAsync(Guid? equipmentId = null)
    {
        var dto = new CreateGageStudyDto
        {
            Name = $"GRR Study {Guid.NewGuid():N}"[..20],
            StudyType = "GRR_Range",
            EquipmentId = equipmentId,
            CharacteristicName = "Diameter",
            Tolerance = 0.1m,
            NumberOfParts = 5,
            NumberOfOperators = 3,
            NumberOfTrials = 2,
        };
        var resp = await _client.PostAsJsonAsync("/api/gage-studies", dto, Json);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<GageStudyResponseDto>(Json))!;
    }

    private AddGageStudyMeasurementsDto GenerateMeasurements(int parts, int operators, int trials)
    {
        var rng = new Random(42);
        var items = new List<MeasurementItemDto>();
        for (int p = 1; p <= parts; p++)
        for (int o = 1; o <= operators; o++)
        for (int t = 1; t <= trials; t++)
        {
            items.Add(new MeasurementItemDto
            {
                PartNumber = p,
                OperatorId = $"OP-{o}",
                TrialNumber = t,
                MeasuredValue = 10.0m + p * 0.01m + (decimal)(rng.NextDouble() * 0.005)
            });
        }
        return new AddGageStudyMeasurementsDto { Measurements = items };
    }

    // ── CRUD Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStudy_ReturnsCreated()
    {
        var eqId = await CreateEquipmentAsync();
        var study = await CreateStudyAsync(eqId);

        Assert.Equal("GRR_Range", study.StudyType);
        Assert.Equal("Draft", study.Status);
        Assert.Equal(eqId, study.EquipmentId);
        Assert.Equal("Diameter", study.CharacteristicName);
        Assert.Equal(5, study.NumberOfParts);
        Assert.Equal(3, study.NumberOfOperators);
        Assert.Equal(2, study.NumberOfTrials);
    }

    [Fact]
    public async Task CreateStudy_InvalidType_ReturnsBadRequest()
    {
        var dto = new CreateGageStudyDto
        {
            Name = "Bad Study",
            StudyType = "Invalid",
            NumberOfParts = 5,
            NumberOfOperators = 3,
            NumberOfTrials = 2,
        };
        var resp = await _client.PostAsJsonAsync("/api/gage-studies", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateStudy_InvalidEquipment_ReturnsBadRequest()
    {
        var dto = new CreateGageStudyDto
        {
            Name = "Bad Study",
            StudyType = "GRR_Range",
            EquipmentId = Guid.NewGuid(),
            NumberOfParts = 5,
            NumberOfOperators = 3,
            NumberOfTrials = 2,
        };
        var resp = await _client.PostAsJsonAsync("/api/gage-studies", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsStudy()
    {
        var study = await CreateStudyAsync();
        var resp = await _client.GetAsync($"/api/gage-studies/{study.Id}");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<GageStudyResponseDto>(Json);
        Assert.NotNull(result);
        Assert.Equal(study.Id, result.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/gage-studies/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateStudy_ChangesFields()
    {
        var study = await CreateStudyAsync();
        var updateDto = new UpdateGageStudyDto
        {
            Name = "Updated Study",
            CharacteristicName = "Width",
            Tolerance = 0.05m,
        };
        var resp = await _client.PutAsJsonAsync($"/api/gage-studies/{study.Id}", updateDto, Json);
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<GageStudyResponseDto>(Json);
        Assert.NotNull(updated);
        Assert.Equal("Updated Study", updated.Name);
        Assert.Equal("Width", updated.CharacteristicName);
        Assert.Equal(0.05m, updated.Tolerance);
    }

    [Fact]
    public async Task DeleteStudy_ReturnsNoContent()
    {
        var study = await CreateStudyAsync();
        var resp = await _client.DeleteAsync($"/api/gage-studies/{study.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/gage-studies/{study.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task GetAll_Paginated()
    {
        await CreateStudyAsync();
        await CreateStudyAsync();

        var resp = await _client.GetAsync("/api/gage-studies");
        resp.EnsureSuccessStatusCode();

        var page = await resp.Content.ReadFromJsonAsync<PaginatedResponse<GageStudySummaryDto>>(Json);
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
    }

    [Fact]
    public async Task GetAll_FilterByStatus()
    {
        await CreateStudyAsync();

        var resp = await _client.GetAsync("/api/gage-studies?status=Draft");
        resp.EnsureSuccessStatusCode();

        var page = await resp.Content.ReadFromJsonAsync<PaginatedResponse<GageStudySummaryDto>>(Json);
        Assert.NotNull(page);
        Assert.All(page.Items, i => Assert.Equal("Draft", i.Status));
    }

    // ── Measurement Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task AddMeasurements_TransitionsToInProgress()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);

        var resp = await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        resp.EnsureSuccessStatusCode();

        var getResp = await _client.GetAsync($"/api/gage-studies/{study.Id}");
        var updated = await getResp.Content.ReadFromJsonAsync<GageStudyResponseDto>(Json);
        Assert.Equal("InProgress", updated!.Status);
    }

    [Fact]
    public async Task AddMeasurements_InvalidPartNumber_ReturnsBadRequest()
    {
        var study = await CreateStudyAsync();
        var dto = new AddGageStudyMeasurementsDto
        {
            Measurements = new List<MeasurementItemDto>
            {
                new() { PartNumber = 99, OperatorId = "OP-1", TrialNumber = 1, MeasuredValue = 10.0m }
            }
        };
        var resp = await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AddMeasurements_InvalidTrialNumber_ReturnsBadRequest()
    {
        var study = await CreateStudyAsync();
        var dto = new AddGageStudyMeasurementsDto
        {
            Measurements = new List<MeasurementItemDto>
            {
                new() { PartNumber = 1, OperatorId = "OP-1", TrialNumber = 99, MeasuredValue = 10.0m }
            }
        };
        var resp = await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetMeasurements_ReturnsList()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);

        var resp = await _client.GetAsync($"/api/gage-studies/{study.Id}/measurements");
        resp.EnsureSuccessStatusCode();

        var list = await resp.Content.ReadFromJsonAsync<List<GageStudyMeasurementDto>>(Json);
        Assert.NotNull(list);
        Assert.Equal(30, list.Count); // 5 parts × 3 ops × 2 trials
    }

    // ── Calculation Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_WithFullData_ReturnsResults()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);

        var resp = await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<GrrCalculationResultDto>(Json);
        Assert.NotNull(result);
        Assert.True(result.RepeatabilityEV >= 0);
        Assert.True(result.ReproducibilityAV >= 0);
        Assert.True(result.GRR >= 0);
        Assert.True(result.TotalVariationTV > 0);
        Assert.True(result.Ndc >= 1);
        Assert.Contains(result.Assessment, new[] { "Acceptable", "Marginal", "Unacceptable" });
    }

    [Fact]
    public async Task Calculate_SetsStudyToComplete()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);

        var resp = await _client.GetAsync($"/api/gage-studies/{study.Id}");
        var updated = await resp.Content.ReadFromJsonAsync<GageStudyResponseDto>(Json);
        Assert.Equal("Complete", updated!.Status);
        Assert.NotNull(updated.GrrPercent);
        Assert.NotNull(updated.Ndc);
        Assert.NotNull(updated.AcceptanceDecision);
    }

    [Fact]
    public async Task Calculate_InsufficientData_ReturnsBadRequest()
    {
        var study = await CreateStudyAsync();
        // Add only 1 measurement instead of required 30
        var dto = new AddGageStudyMeasurementsDto
        {
            Measurements = new List<MeasurementItemDto>
            {
                new() { PartNumber = 1, OperatorId = "OP-1", TrialNumber = 1, MeasuredValue = 10.0m }
            }
        };
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", dto, Json);

        var resp = await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Calculate_WithTolerance_ReturnsPercentTolerance()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);

        var resp = await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<GrrCalculationResultDto>(Json);
        Assert.NotNull(result);
        Assert.NotNull(result.PercentTolerance);
    }

    [Fact]
    public async Task UpdateCompletedStudy_ReturnsBadRequest()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);

        var updateDto = new UpdateGageStudyDto { Name = "Should fail" };
        var resp = await _client.PutAsJsonAsync($"/api/gage-studies/{study.Id}", updateDto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AddMeasurementsToCompletedStudy_ReturnsBadRequest()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);

        var dto = new AddGageStudyMeasurementsDto
        {
            Measurements = new List<MeasurementItemDto>
            {
                new() { PartNumber = 1, OperatorId = "OP-1", TrialNumber = 1, MeasuredValue = 10.0m }
            }
        };
        var resp = await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", dto, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Dashboard Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsAggregates()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);

        var resp = await _client.GetAsync("/api/gage-studies/dashboard");
        resp.EnsureSuccessStatusCode();

        var dashboard = await resp.Content.ReadFromJsonAsync<GageStudyDashboardDto>(Json);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.Total >= 1);
        Assert.True(dashboard.Complete >= 1);
    }

    // ── Auth & Isolation Tests ──────────────────────────────────────────────

    [Fact]
    public async Task GageStudy_RequiresAuth()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/gage-studies");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CrossTenantIsolation_StudiesNotVisible()
    {
        var study = await CreateStudyAsync();

        var otherTenantId = _factory.CreateTenant("msa-other");
        using var otherClient = _factory.CreateTenantClient(otherTenantId);

        var resp = await otherClient.GetAsync($"/api/gage-studies/{study.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── MCP Tool Test ───────────────────────────────────────────────────────

    [Fact]
    public async Task McpGetMsaStatus_ReturnsData()
    {
        var study = await CreateStudyAsync();
        var measurements = GenerateMeasurements(5, 3, 2);
        await _client.PostAsJsonAsync($"/api/gage-studies/{study.Id}/measurements", measurements, Json);
        await _client.PostAsync($"/api/gage-studies/{study.Id}/calculate", null);

        var mcpRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "get_msa_status",
                arguments = new { }
            }
        };

        var resp = await _client.PostAsJsonAsync("/mcp", mcpRequest, Json);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("MSA/GR&R Status Summary", body);
        Assert.Contains("Acceptance Breakdown", body);
    }
}
