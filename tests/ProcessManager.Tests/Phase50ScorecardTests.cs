using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 50: Strategy Management — Balanced Scorecard integration tests.
/// Covers scorecard CRUD with default perspective seeding, objective/measure CRUD,
/// cause-link and process-link constraints, manual snapshot RAG evaluation,
/// live measure resolution (ProcessYield, ProcessMaturity), strategy map,
/// auth, and tenant isolation.
/// </summary>
public class Phase50ScorecardTests : IntegrationTestBase
{
    private readonly TestWebApplicationFactory _factory;

    public Phase50ScorecardTests(TestWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    // ──────────── Helpers ────────────

    private async Task<ScorecardResponseDto> CreateScorecard(string name = "Test Scorecard")
    {
        var response = await Client.PostAsJsonAsync("/api/scorecards",
            new CreateScorecardDto(name, "Mission", "Vision", null, null), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;
    }

    private async Task<ObjectiveResponseDto> CreateObjective(Guid perspectiveId, string name = "Test Objective")
    {
        var response = await Client.PostAsJsonAsync($"/api/scorecards/perspectives/{perspectiveId}/objectives",
            new CreateObjectiveDto(name, null, null, null, 0), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ObjectiveResponseDto>(JsonOptions))!;
    }

    private async Task<MeasureResponseDto> CreateMeasure(Guid objectiveId, CreateMeasureDto dto)
    {
        var response = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objectiveId}/measures", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MeasureResponseDto>(JsonOptions))!;
    }

    private async Task<ScorecardStatusDto> GetStatus(Guid scorecardId)
    {
        var response = await Client.GetAsync($"/api/scorecards/{scorecardId}/status");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScorecardStatusDto>(JsonOptions))!;
    }

    // ──────────── Scorecard CRUD ────────────

    [Fact]
    public async Task CreateScorecard_SeedsFourDefaultPerspectives()
    {
        var scorecard = await CreateScorecard("Strategy 2026");

        Assert.Matches(new Regex(@"^BSC-\d{3,}$"), scorecard.Code);
        Assert.Equal("Draft", scorecard.Status);
        Assert.Equal(4, scorecard.Perspectives.Count);
        Assert.Equal(
            new[] { "Financial", "Customer", "Internal Business Process", "Learning & Growth" },
            scorecard.Perspectives.OrderBy(p => p.SortOrder).Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetScorecard_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/scorecards/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateScorecard_ChangesFieldsAndStatus()
    {
        var scorecard = await CreateScorecard();

        var response = await Client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto("Renamed", "New mission", null, null, "Active", null), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        Assert.Equal("Renamed", updated.Name);
        Assert.Equal("New mission", updated.MissionStatement);
        Assert.Equal("Active", updated.Status);
    }

    [Fact]
    public async Task UpdateScorecard_InvalidStatus_Returns400()
    {
        var scorecard = await CreateScorecard();

        var response = await Client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto(null, null, null, null, "Bogus", null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScorecard_ActiveBlocked_ArchivedAllowed()
    {
        var scorecard = await CreateScorecard();

        await Client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto(null, null, null, null, "Active", null), JsonOptions);

        var blocked = await Client.DeleteAsync($"/api/scorecards/{scorecard.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);

        await Client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto(null, null, null, null, "Archived", null), JsonOptions);

        var allowed = await Client.DeleteAsync($"/api/scorecards/{scorecard.Id}");
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);

        var gone = await Client.GetAsync($"/api/scorecards/{scorecard.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task ListScorecards_FiltersByStatus()
    {
        var scorecard = await CreateScorecard("Filterable");
        await Client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto(null, null, null, null, "Active", null), JsonOptions);

        var response = await Client.GetAsync("/api/scorecards?status=Active");
        response.EnsureSuccessStatusCode();
        var page = (await response.Content.ReadFromJsonAsync<PaginatedResponse<ScorecardSummaryDto>>(JsonOptions))!;

        Assert.Contains(page.Items, s => s.Id == scorecard.Id);
        Assert.All(page.Items, s => Assert.Equal("Active", s.Status));
    }

    // ──────────── Perspectives & objectives ────────────

    [Fact]
    public async Task AddPerspectiveAndObjective_ObjectiveGetsCode()
    {
        var scorecard = await CreateScorecard();

        var pResponse = await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/perspectives",
            new CreatePerspectiveDto("Regulatory", "Compliance lens", 4), JsonOptions);
        pResponse.EnsureSuccessStatusCode();
        var perspective = (await pResponse.Content.ReadFromJsonAsync<PerspectiveResponseDto>(JsonOptions))!;
        Assert.Equal("Regulatory", perspective.Name);

        var objective = await CreateObjective(perspective.Id, "Pass every audit");
        Assert.Matches(new Regex(@"^OBJ-\d{3,}$"), objective.Code);
        Assert.Equal("Proposed", objective.Status);

        var tree = await Client.GetFromJsonAsync<ScorecardResponseDto>($"/api/scorecards/{scorecard.Id}", JsonOptions);
        Assert.Equal(5, tree!.Perspectives.Count);
        Assert.Contains(tree.Perspectives, p => p.Objectives.Any(o => o.Id == objective.Id));
    }

    [Fact]
    public async Task AddObjective_NonexistentOwnerOrgUnit_Returns400()
    {
        var scorecard = await CreateScorecard();
        var perspective = scorecard.Perspectives[0];

        var response = await Client.PostAsJsonAsync($"/api/scorecards/perspectives/{perspective.Id}/objectives",
            new CreateObjectiveDto("Bad owner", null, Guid.NewGuid(), null, 0), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── Measures & validation ────────────

    [Fact]
    public async Task AddMeasure_InvalidDirectionOrSource_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var badDirection = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("M", "%", "Sideways", null, 90, null, null, "Manual", null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, badDirection.StatusCode);

        var badSource = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("M", "%", "HigherIsBetter", null, 90, null, null, "Telepathy", null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, badSource.StatusCode);
    }

    [Fact]
    public async Task AddMeasure_ProcessYieldWithoutProcess_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var missing = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Yield", "%", "HigherIsBetter", null, 95, null, null, "ProcessYield", null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var nonexistent = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Yield", "%", "HigherIsBetter", null, 95, null, null, "ProcessYield", Guid.NewGuid(), null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, nonexistent.StatusCode);
    }

    // ──────────── Manual snapshots & RAG ────────────

    [Fact]
    public async Task ManualMeasure_LatestSnapshotDrivesValueAndRag()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        // Higher-is-better: >= 90 Green, <= 70 Red
        var measure = await CreateMeasure(objective.Id,
            new CreateMeasureDto("On-time delivery", "%", "HigherIsBetter", 60, 95, 90, 70, "Manual", null, null));

        var s1 = await Client.PostAsJsonAsync($"/api/scorecards/measures/{measure.Id}/snapshots",
            new CreateMeasureSnapshotDto(95, DateTime.UtcNow.AddDays(-2), "good week"), JsonOptions);
        s1.EnsureSuccessStatusCode();

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(95m, resolved.CurrentValue);
        Assert.Equal("Green", resolved.Rag);
        Assert.Equal(1, status.GreenCount);

        // A newer, worse reading flips the measure to Red
        await Client.PostAsJsonAsync($"/api/scorecards/measures/{measure.Id}/snapshots",
            new CreateMeasureSnapshotDto(60, DateTime.UtcNow, "bad week"), JsonOptions);

        status = await GetStatus(scorecard.Id);
        resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(60m, resolved.CurrentValue);
        Assert.Equal("Red", resolved.Rag);
        Assert.Equal(1, status.RedCount);

        var snapshots = await Client.GetFromJsonAsync<List<MeasureSnapshotResponseDto>>(
            $"/api/scorecards/measures/{measure.Id}/snapshots", JsonOptions);
        Assert.Equal(2, snapshots!.Count);
    }

    [Fact]
    public async Task LowerIsBetterMeasure_EvaluatesRagCorrectly()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        // Lower-is-better: <= 5 Green, >= 10 Red
        var measure = await CreateMeasure(objective.Id,
            new CreateMeasureDto("Open NCs", "count", "LowerIsBetter", null, 5, 5, 10, "Manual", null, null));

        await Client.PostAsJsonAsync($"/api/scorecards/measures/{measure.Id}/snapshots",
            new CreateMeasureSnapshotDto(12, null, null), JsonOptions);

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal("Red", resolved.Rag);
    }

    [Fact]
    public async Task MeasureWithNoData_ReportsNoData()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);
        await CreateMeasure(objective.Id,
            new CreateMeasureDto("Unfed", "%", "HigherIsBetter", null, 90, null, null, "Manual", null, null));

        var status = await GetStatus(scorecard.Id);
        Assert.Equal(1, status.NoDataCount);
    }

    // ──────────── Live source resolution ────────────

    [Fact]
    public async Task ProcessYieldMeasure_ComputesYieldFromItems()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // 3 passed, 1 failed
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.FailedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id, "Improve first-pass yield");

        // Good grade configured via SourceParameter → 3/4 = 75%
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Finishing yield", "%", "HigherIsBetter", null, 95, 90, 50,
            "ProcessYield", scenario.Process.Id, scenario.PassedGrade.Id.ToString()));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(75m, resolved.CurrentValue);
        Assert.Equal("Amber", resolved.Rag); // between red(50) and green(90)
    }

    [Fact]
    public async Task ProcessYieldMeasure_WithoutGoodGrade_CountsNonScrapped()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.RawGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Yield", "%", "HigherIsBetter", null, 95, null, null,
            "ProcessYield", scenario.Process.Id, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(100m, resolved.CurrentValue);
    }

    [Fact]
    public async Task ProcessMaturityMeasure_ResolvesMeanStepScore()
    {
        var scenario = await BuildWidgetFinishingScenario();

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id, "Mature the finishing process");
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Process maturity", "score", "HigherIsBetter", null, 100, null, null,
            "ProcessMaturity", scenario.Process.Id, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.NotNull(resolved.CurrentValue);
        Assert.InRange(resolved.CurrentValue!.Value, 0m, 100m);
    }

    // ──────────── Cause links & strategy map ────────────

    [Fact]
    public async Task CauseLink_SelfLinkAndDuplicateRejected()
    {
        var scorecard = await CreateScorecard();
        var o1 = await CreateObjective(scorecard.Perspectives[0].Id, "Objective A");
        var o2 = await CreateObjective(scorecard.Perspectives[1].Id, "Objective B");

        var self = await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/cause-links",
            new CreateCauseLinkDto(o1.Id, o1.Id, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);

        var valid = await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/cause-links",
            new CreateCauseLinkDto(o1.Id, o2.Id, "A drives B"), JsonOptions);
        valid.EnsureSuccessStatusCode();

        var duplicate = await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/cause-links",
            new CreateCauseLinkDto(o1.Id, o2.Id, null), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CauseLink_ObjectiveFromAnotherScorecard_Rejected()
    {
        var scorecardA = await CreateScorecard("A");
        var scorecardB = await CreateScorecard("B");
        var oa = await CreateObjective(scorecardA.Perspectives[0].Id);
        var ob = await CreateObjective(scorecardB.Perspectives[0].Id);

        var response = await Client.PostAsJsonAsync($"/api/scorecards/{scorecardA.Id}/cause-links",
            new CreateCauseLinkDto(oa.Id, ob.Id, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StrategyMap_ReturnsNodesAndEdges()
    {
        var scorecard = await CreateScorecard();
        var learning = await CreateObjective(scorecard.Perspectives[3].Id, "Train the team");
        var process = await CreateObjective(scorecard.Perspectives[2].Id, "Reduce rework");

        await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/cause-links",
            new CreateCauseLinkDto(learning.Id, process.Id, "training reduces rework"), JsonOptions);

        var map = await Client.GetFromJsonAsync<StrategyMapDto>($"/api/scorecards/{scorecard.Id}/strategy-map", JsonOptions);

        Assert.Equal(2, map!.Nodes.Count);
        var edge = Assert.Single(map.Edges);
        Assert.Equal(learning.Id, edge.SourceObjectiveId);
        Assert.Equal(process.Id, edge.TargetObjectiveId);
        Assert.Contains(map.Nodes, n => n.PerspectiveName == "Learning & Growth");
    }

    [Fact]
    public async Task DeleteObjective_RemovesItsCauseLinks()
    {
        var scorecard = await CreateScorecard();
        var o1 = await CreateObjective(scorecard.Perspectives[0].Id);
        var o2 = await CreateObjective(scorecard.Perspectives[1].Id);

        await Client.PostAsJsonAsync($"/api/scorecards/{scorecard.Id}/cause-links",
            new CreateCauseLinkDto(o1.Id, o2.Id, null), JsonOptions);

        var delete = await Client.DeleteAsync($"/api/scorecards/objectives/{o2.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var map = await Client.GetFromJsonAsync<StrategyMapDto>($"/api/scorecards/{scorecard.Id}/strategy-map", JsonOptions);
        Assert.Empty(map!.Edges);
        Assert.Single(map.Nodes);
    }

    // ──────────── Process links ────────────

    [Fact]
    public async Task ProcessLink_RequiresExactlyOneTarget()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[2].Id);

        var neither = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/process-links",
            new CreateProcessLinkDto(null, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);

        var both = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/process-links",
            new CreateProcessLinkDto(scenario.Process.Id, Guid.NewGuid(), null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);

        var valid = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/process-links",
            new CreateProcessLinkDto(scenario.Process.Id, null, "realizes this objective"), JsonOptions);
        valid.EnsureSuccessStatusCode();
        var link = (await valid.Content.ReadFromJsonAsync<ProcessLinkResponseDto>(JsonOptions))!;
        Assert.Equal(scenario.Process.Id, link.ProcessId);
        Assert.Equal(scenario.Process.Name, link.ProcessName);

        var duplicate = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/process-links",
            new CreateProcessLinkDto(scenario.Process.Id, null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task ProcessLink_NonexistentProcess_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var response = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/process-links",
            new CreateProcessLinkDto(Guid.NewGuid(), null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── Live source resolution (Phase 50b completion) ────────────

    [Fact]
    public async Task QualityCostMeasure_SumsCostsInWindow()
    {
        await PostQualityCost(150.00m);
        await PostQualityCost(50.00m);

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id, "Reduce cost of quality");
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "CoQ 30-day", "$", "LowerIsBetter", null, 100, 100, 500, "QualityCost", null, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        // The cost window is org-wide, so other tests in this class (e.g. the seeded demo)
        // may contribute — assert our 200 is included rather than an exact total.
        Assert.True(resolved.CurrentValue >= 200.00m);
        Assert.NotEqual("NoData", resolved.Rag);
    }

    [Fact]
    public async Task TrainingComplianceMeasure_ResolvesPercentCurrent()
    {
        // A Training-role process with two current competency records → 100 %
        var createResp = await Client.PostAsJsonAsync("/api/processes",
            new ProcessCreateDto($"TRN-{Guid.NewGuid().ToString()[..6]}", "Forklift Training", null, "Training"), JsonOptions);
        createResp.EnsureSuccessStatusCode();
        var trainingProcess = (await createResp.Content.ReadFromJsonAsync<ProcessResponseDto>(JsonOptions))!;

        foreach (var user in new[] { "user-a", "user-b" })
        {
            var resp = await Client.PostAsJsonAsync("/api/competency",
                new CreateCompetencyRecordDto(user, user, trainingProcess.Id, null, null, null, DateTime.UtcNow, null),
                JsonOptions);
            resp.EnsureSuccessStatusCode();
        }

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[3].Id, "Keep the team trained");
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Training compliance", "%", "HigherIsBetter", null, 100, 95, 80,
            "TrainingCompliance", trainingProcess.Id, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(100m, resolved.CurrentValue);
        Assert.Equal("Green", resolved.Rag);
    }

    [Fact]
    public async Task SpcCapabilityMeasure_ResolvesCpk()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);

        // Step execution to attach data points to
        var jobJson = await Client.GetFromJsonAsync<JsonElement>($"/api/jobs/{job.Id}", JsonOptions);
        var stepExecutionId = jobJson.GetProperty("stepExecutions")[0].GetProperty("id").GetGuid();

        var chartResp = await Client.PostAsJsonAsync("/api/spc", new CreateSpcChartDto(
            scenario.Process.Id, Guid.NewGuid(), $"Cpk Chart {Guid.NewGuid().ToString()[..6]}",
            "XbarR", 2, "Calculated",
            null, null, null, null, null, null, null, 0m, 10m), JsonOptions);
        chartResp.EnsureSuccessStatusCode();
        var chart = (await chartResp.Content.ReadFromJsonAsync<SpcChartDto>(JsonOptions))!;

        foreach (var value in new[] { 4m, 5m, 6m, 5m })
        {
            var dpResp = await Client.PostAsJsonAsync($"/api/spc/{chart.Id}/data-points",
                new AddSpcDataPointDto(stepExecutionId, value), JsonOptions);
            dpResp.EnsureSuccessStatusCode();
        }

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[2].Id, "Stabilize the process");
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Cpk", null, "HigherIsBetter", null, 1.33m, 1.33m, 1.0m,
            "SpcCapability", chart.Id, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.NotNull(resolved.CurrentValue);
        Assert.True(resolved.CurrentValue > 0);
    }

    [Fact]
    public async Task SpcCapabilityMeasure_NonexistentChart_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var response = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Cpk", null, "HigherIsBetter", null, 1.33m, null, null,
                "SpcCapability", Guid.NewGuid(), null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OeeMeasure_NoEquipmentData_ReportsNoData()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[2].Id);
        await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Plant OEE", "%", "HigherIsBetter", null, 85, 85, 60, "Oee", null, null));

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Null(resolved.CurrentValue);
        Assert.Equal("NoData", resolved.Rag);
    }

    [Fact]
    public async Task PromptMetricMeasure_NonexistentContentBlock_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var response = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Avg temp", "°C", "TargetIsBest", null, 180, 5, 15,
                "PromptMetric", Guid.NewGuid(), null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────── Snapshot service (Phase 50b) ────────────

    [Fact]
    public async Task SnapshotService_CapturesAutoSnapshots_WithoutDuplicates()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.PassedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");
        await CreateItem(job.Id, scenario.WidgetKind.Id, scenario.FailedGrade.Id, serialNumber: $"SN-{Guid.NewGuid().ToString()[..8]}");

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);
        var measure = await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Finishing yield", "%", "HigherIsBetter", null, 95, 90, 50,
            "ProcessYield", scenario.Process.Id, scenario.PassedGrade.Id.ToString()));

        var service = CreateSnapshotService();
        await service.CaptureSnapshotsAsync(CancellationToken.None);

        var snapshots = await Client.GetFromJsonAsync<List<MeasureSnapshotResponseDto>>(
            $"/api/scorecards/measures/{measure.Id}/snapshots", JsonOptions);
        var auto = Assert.Single(snapshots!, s => s.CaptureSource == "Auto");
        Assert.Equal(75m, auto.Value);

        // A second run within the duplicate-guard window must not add another snapshot
        await service.CaptureSnapshotsAsync(CancellationToken.None);

        snapshots = await Client.GetFromJsonAsync<List<MeasureSnapshotResponseDto>>(
            $"/api/scorecards/measures/{measure.Id}/snapshots", JsonOptions);
        Assert.Single(snapshots!, s => s.CaptureSource == "Auto");
    }

    [Fact]
    public async Task SnapshotService_SkipsManualMeasures()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);
        var measure = await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Manual only", "%", "HigherIsBetter", null, 90, null, null, "Manual", null, null));

        var service = CreateSnapshotService();
        await service.CaptureSnapshotsAsync(CancellationToken.None);

        var snapshots = await Client.GetFromJsonAsync<List<MeasureSnapshotResponseDto>>(
            $"/api/scorecards/measures/{measure.Id}/snapshots", JsonOptions);
        Assert.DoesNotContain(snapshots!, s => s.CaptureSource == "Auto");
    }

    private ScorecardSnapshotService CreateSnapshotService()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var logger = _factory.Services.GetRequiredService<ILogger<ScorecardSnapshotService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ScorecardSnapshots:IntervalHours"] = "24" })
            .Build();
        return new ScorecardSnapshotService(scopeFactory, logger, config);
    }

    private async Task PostQualityCost(decimal amount)
    {
        var resp = await Client.PostAsJsonAsync("/api/quality-costs", new CreateQualityCostDto
        {
            SourceType = "Scrap",
            Amount = amount,
            Currency = "USD",
            CostCategory = "InternalFailure",
            Description = $"Test cost {Guid.NewGuid():N}",
            RecordedByUserId = "test-user-id",
            RecordedByDisplayName = "Test User",
        }, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    // ──────────── Full-capability example (seed-demo) ────────────

    [Fact]
    public async Task SeedDemo_CreatesFullCapabilityScorecard()
    {
        var resp = await Client.PostAsync("/api/scorecards/seed-demo", null);
        resp.EnsureSuccessStatusCode();
        var scorecard = (await resp.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        Assert.StartsWith("BSC-DEMO-", scorecard.Code);
        Assert.Equal("Active", scorecard.Status);
        Assert.Equal(4, scorecard.Perspectives.Count);
        Assert.Equal(7, scorecard.Perspectives.Sum(p => p.Objectives.Count));
        Assert.Equal(8, scorecard.Perspectives.Sum(p => p.Objectives.Sum(o => o.Measures.Count)));
        Assert.Equal(7, scorecard.CauseLinks.Count);

        // Both internal-process objectives are linked to the demo process
        var processLinks = scorecard.Perspectives
            .SelectMany(p => p.Objectives)
            .SelectMany(o => o.ProcessLinks)
            .ToList();
        Assert.Equal(2, processLinks.Count);
        Assert.All(processLinks, l => Assert.Equal("Precision Widget Finishing", l.ProcessName));
    }

    [Fact]
    public async Task SeedDemo_LiveAndManualMeasuresResolve()
    {
        var resp = await Client.PostAsync("/api/scorecards/seed-demo", null);
        resp.EnsureSuccessStatusCode();
        var scorecard = (await resp.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        var status = await GetStatus(scorecard.Id);
        var measures = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).ToList();

        // ProcessYield: 18 of 20 demo items carry the PASS grade → 90 %, at the Green threshold
        var yield = measures.Single(m => m.SourceType == "ProcessYield");
        Assert.Equal(90m, yield.CurrentValue);
        Assert.Equal("Green", yield.Rag);

        // ProcessMaturity: seeded step has setup/safety/inspection content → a real score
        var maturity = measures.Single(m => m.SourceType == "ProcessMaturity");
        Assert.NotNull(maturity.CurrentValue);
        Assert.InRange(maturity.CurrentValue!.Value, 50m, 100m);

        // ActionCloseRate: 4 of 5 seeded initiatives are complete → 80 %
        var closeRate = measures.Single(m => m.SourceType == "ActionCloseRate");
        Assert.Equal(80m, closeRate.CurrentValue);

        // QualityCost: at least the demo's 450 in the window (other tests may add more)
        var coq = measures.Single(m => m.SourceType == "QualityCost");
        Assert.True(coq.CurrentValue >= 450m);

        // Manual measures read their latest snapshot
        var revenue = measures.Single(m => m.Name == "Revenue per employee");
        Assert.Equal(212m, revenue.CurrentValue);
        Assert.Equal("Green", revenue.Rag);

        // Every measure on the demo scorecard resolves to a value
        Assert.All(measures, m => Assert.NotNull(m.CurrentValue));
    }

    [Fact]
    public async Task SeedDemo_StrategyMapHasFullCauseChain()
    {
        var resp = await Client.PostAsync("/api/scorecards/seed-demo", null);
        resp.EnsureSuccessStatusCode();
        var scorecard = (await resp.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        var map = await Client.GetFromJsonAsync<StrategyMapDto>($"/api/scorecards/{scorecard.Id}/strategy-map", JsonOptions);

        Assert.Equal(7, map!.Nodes.Count);
        Assert.Equal(7, map.Edges.Count);
        Assert.Equal(4, map.Nodes.Select(n => n.PerspectiveName).Distinct().Count());

        // The chain reaches from Learning & Growth all the way to Financial
        var byPerspective = map.Nodes.ToDictionary(n => n.ObjectiveId, n => n.PerspectiveName);
        Assert.Contains(map.Edges, e => byPerspective[e.SourceObjectiveId] == "Learning & Growth"
                                      && byPerspective[e.TargetObjectiveId] == "Internal Business Process");
        Assert.Contains(map.Edges, e => byPerspective[e.SourceObjectiveId] == "Internal Business Process"
                                      && byPerspective[e.TargetObjectiveId] == "Customer");
        Assert.Contains(map.Edges, e => byPerspective[e.SourceObjectiveId] == "Customer"
                                      && byPerspective[e.TargetObjectiveId] == "Financial");
    }

    [Fact]
    public async Task SeedDemo_IsIdempotent()
    {
        var first = await Client.PostAsync("/api/scorecards/seed-demo", null);
        first.EnsureSuccessStatusCode();
        var a = (await first.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        var second = await Client.PostAsync("/api/scorecards/seed-demo", null);
        second.EnsureSuccessStatusCode();
        var b = (await second.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;

        Assert.Equal(a.Id, b.Id);

        var list = await Client.GetFromJsonAsync<PaginatedResponse<ScorecardSummaryDto>>(
            "/api/scorecards?search=BSC-DEMO&pageSize=100", JsonOptions);
        Assert.Single(list!.Items);
    }

    [Fact]
    public async Task SeedDemo_CreatesInitiativesAsActionItems()
    {
        var resp = await Client.PostAsync("/api/scorecards/seed-demo", null);
        resp.EnsureSuccessStatusCode();

        var actions = await Client.GetFromJsonAsync<JsonElement>("/api/action-items?pageSize=200", JsonOptions);
        var initiatives = actions.GetProperty("items").EnumerateArray()
            .Where(a => a.GetProperty("sourceType").GetString() == "StrategicObjective")
            .ToList();

        Assert.Equal(5, initiatives.Count);
        Assert.Contains(initiatives, a => a.GetProperty("status").GetString() == "Open");
    }

    // ──────────── MCP tools ────────────

    [Fact]
    public async Task McpGetScorecardStatus_ReturnsStrategyReport()
    {
        var seed = await Client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new { name = "get_scorecard_status", arguments = new { } }
        };

        var resp = await Client.PostAsJsonAsync("/mcp", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Contains("Balanced Scorecard", body);
        Assert.Contains("Demo: Widget Co. Strategy", body);
        Assert.Contains("Rollup", body);
        Assert.Contains("Finishing first-pass yield", body);
    }

    [Fact]
    public async Task McpListAtRiskObjectives_ReportsNoRedWhenDemoIsGreen()
    {
        var seed = await Client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();

        var request = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new { name = "list_at_risk_objectives", arguments = new { include_amber = "false" } }
        };

        var resp = await Client.PostAsJsonAsync("/mcp", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();

        // The demo scorecard resolves all-Green, and it is the only Active scorecard with measures.
        Assert.Contains("No objectives are Red", body);
    }

    // ──────────── Feature flag & source validation ────────────

    [Fact]
    public async Task FeatureFlags_RoundTripShowStrategyTools()
    {
        var get = await Client.GetFromJsonAsync<TenantFeatureFlagsDto>("/api/onboarding/feature-flags", JsonOptions);
        Assert.True(get!.ShowStrategyTools); // legacy tenants default to everything on

        var put = await Client.PutAsJsonAsync("/api/onboarding/feature-flags",
            get with { ShowStrategyTools = false }, JsonOptions);
        put.EnsureSuccessStatusCode();
        var updated = (await put.Content.ReadFromJsonAsync<TenantFeatureFlagsDto>(JsonOptions))!;
        Assert.False(updated.ShowStrategyTools);

        // Restore so other tests in this class see the default surface
        await Client.PutAsJsonAsync("/api/onboarding/feature-flags",
            updated with { ShowStrategyTools = true }, JsonOptions);
    }

    [Fact]
    public async Task AddMeasure_SpcCapabilityWithoutChart_Returns400()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var missing = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Cpk", null, "HigherIsBetter", null, 1.33m, null, null, "SpcCapability", null, null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var nonexistent = await Client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("Cpk", null, "HigherIsBetter", null, 1.33m, null, null, "SpcCapability", Guid.NewGuid(), null), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, nonexistent.StatusCode);
    }

    // ──────────── Prompt metrics, trends, perspectives, management review ────────────

    [Fact]
    public async Task PromptMetricOptions_ListsSeededNumericPrompt()
    {
        // The demo seeder creates a NumericEntry inspection prompt on its step template
        var seed = await Client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();

        var options = await Client.GetFromJsonAsync<List<PromptMetricOptionDto>>("/api/scorecards/prompt-metrics", JsonOptions);

        Assert.NotNull(options);
        Assert.Contains(options!, o => o.Label == "Outside diameter (mm)"
                                    && o.StepTemplateName == "Final Precision Inspection"
                                    && o.Units == "mm");
    }

    [Fact]
    public async Task PromptMetricMeasure_ValidSourceCreates_NoResponsesIsNoData()
    {
        var seed = await Client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();
        var options = await Client.GetFromJsonAsync<List<PromptMetricOptionDto>>("/api/scorecards/prompt-metrics", JsonOptions);
        var prompt = options!.First(o => o.Label == "Outside diameter (mm)");

        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);

        var created = await CreateMeasure(objective.Id, new CreateMeasureDto(
            "Mean OD", "mm", "TargetIsBest", null, 25.00m, 0.02m, 0.05m,
            "PromptMetric", prompt.Id, null));
        Assert.Equal("PromptMetric", created.SourceType);

        // No prompt responses exist yet → resolves to no data
        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Null(resolved.CurrentValue);
        Assert.Equal("NoData", resolved.Rag);
    }

    [Fact]
    public async Task MeasureTrend_ReturnsSnapshotsChronologically()
    {
        var scorecard = await CreateScorecard();
        var objective = await CreateObjective(scorecard.Perspectives[0].Id);
        var measure = await CreateMeasure(objective.Id,
            new CreateMeasureDto("Trending", "%", "HigherIsBetter", null, 100, null, null, "Manual", null, null));

        foreach (var (value, daysAgo) in new[] { (70m, 3), (80m, 2), (90m, 1) })
        {
            var resp = await Client.PostAsJsonAsync($"/api/scorecards/measures/{measure.Id}/snapshots",
                new CreateMeasureSnapshotDto(value, DateTime.UtcNow.AddDays(-daysAgo), null), JsonOptions);
            resp.EnsureSuccessStatusCode();
        }

        var status = await GetStatus(scorecard.Id);
        var resolved = status.Perspectives.SelectMany(p => p.Objectives).SelectMany(o => o.Measures).Single();
        Assert.Equal(new List<decimal> { 70m, 80m, 90m }, resolved.Trend);
    }

    [Fact]
    public async Task Perspective_RenameAndReorder_RoundTrips()
    {
        var scorecard = await CreateScorecard();
        var first = scorecard.Perspectives.OrderBy(p => p.SortOrder).First();

        var rename = await Client.PutAsJsonAsync($"/api/scorecards/perspectives/{first.Id}",
            new UpdatePerspectiveDto("Stakeholders", "Renamed lens", null), JsonOptions);
        rename.EnsureSuccessStatusCode();

        var reorder = await Client.PutAsJsonAsync($"/api/scorecards/perspectives/{first.Id}",
            new UpdatePerspectiveDto(null, null, 99), JsonOptions);
        reorder.EnsureSuccessStatusCode();

        var tree = await Client.GetFromJsonAsync<ScorecardResponseDto>($"/api/scorecards/{scorecard.Id}", JsonOptions);
        var moved = tree!.Perspectives.Single(p => p.Id == first.Id);
        Assert.Equal("Stakeholders", moved.Name);
        Assert.Equal(99, moved.SortOrder);
        Assert.Equal(moved.Id, tree.Perspectives.Last().Id); // ordered by SortOrder
    }

    [Fact]
    public async Task ManagementReviewStart_PopulatesScorecardSummary()
    {
        var seed = await Client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();

        var create = await Client.PostAsJsonAsync("/api/management-reviews",
            new { Title = "Q2 Strategy Review", ReviewType = "Quarterly", ScheduledDate = DateTime.UtcNow }, JsonOptions);
        create.EnsureSuccessStatusCode();
        var review = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var reviewId = review.GetProperty("id").GetGuid();

        var start = await Client.PostAsync($"/api/management-reviews/{reviewId}/start", null);
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var summary = started.GetProperty("scorecardSummary").GetString();
        Assert.NotNull(summary);
        Assert.Contains("Active scorecards", summary);
        Assert.Contains("BSC-DEMO", summary);
        Assert.Contains("No objectives are Red", summary);
    }

    // ──────────── Dashboard widget ────────────
    // The dashboard endpoint is org-wide per tenant, so these run in fresh,
    // isolated tenants to stay order-independent of the rest of the suite.

    private HttpClient FreshTenantClient() =>
        _factory.CreateTenantClient(_factory.CreateTenant($"bsc-dash-{Guid.NewGuid().ToString()[..6]}"));

    [Fact]
    public async Task Dashboard_NoActiveScorecard_ReturnsEmpty()
    {
        using var client = FreshTenantClient();

        // A Draft scorecard exists but no Active one
        var create = await client.PostAsJsonAsync("/api/scorecards",
            new CreateScorecardDto("Draft only", null, null, null, null), JsonOptions);
        create.EnsureSuccessStatusCode();

        var dash = await client.GetFromJsonAsync<ScorecardDashboardDto>("/api/scorecards/dashboard", JsonOptions);

        Assert.NotNull(dash);
        Assert.Null(dash!.ScorecardId);
        Assert.Equal(0, dash.ActiveScorecardCount);
        Assert.Empty(dash.AtRiskObjectives);
    }

    [Fact]
    public async Task Dashboard_PicksActiveScorecardAndRollsUpRag()
    {
        using var client = FreshTenantClient();

        var seed = await client.PostAsync("/api/scorecards/seed-demo", null);
        seed.EnsureSuccessStatusCode();

        var dash = await client.GetFromJsonAsync<ScorecardDashboardDto>("/api/scorecards/dashboard", JsonOptions);

        Assert.NotNull(dash);
        Assert.StartsWith("BSC-DEMO", dash!.ScorecardCode);
        Assert.Equal(1, dash.ActiveScorecardCount);
        // The demo resolves all-Green, so nothing is at risk.
        Assert.True(dash.GreenCount > 0);
        Assert.Empty(dash.AtRiskObjectives);
    }

    [Fact]
    public async Task Dashboard_SurfacesRedObjective()
    {
        using var client = FreshTenantClient();

        var create = await client.PostAsJsonAsync("/api/scorecards",
            new CreateScorecardDto("Live strategy", null, null, null, null), JsonOptions);
        var scorecard = (await create.Content.ReadFromJsonAsync<ScorecardResponseDto>(JsonOptions))!;
        await client.PutAsJsonAsync($"/api/scorecards/{scorecard.Id}",
            new UpdateScorecardDto(null, null, null, null, "Active", null), JsonOptions);

        var objResp = await client.PostAsJsonAsync($"/api/scorecards/perspectives/{scorecard.Perspectives[0].Id}/objectives",
            new CreateObjectiveDto("Hit the number", null, null, null, 0), JsonOptions);
        var objective = (await objResp.Content.ReadFromJsonAsync<ObjectiveResponseDto>(JsonOptions))!;

        // Higher-is-better, Red at/below 70; record a reading of 40 → Red
        var measResp = await client.PostAsJsonAsync($"/api/scorecards/objectives/{objective.Id}/measures",
            new CreateMeasureDto("KPI", "%", "HigherIsBetter", null, 95, 90, 70, "Manual", null, null), JsonOptions);
        var measure = (await measResp.Content.ReadFromJsonAsync<MeasureResponseDto>(JsonOptions))!;
        await client.PostAsJsonAsync($"/api/scorecards/measures/{measure.Id}/snapshots",
            new CreateMeasureSnapshotDto(40, null, null), JsonOptions);

        var dash = await client.GetFromJsonAsync<ScorecardDashboardDto>("/api/scorecards/dashboard", JsonOptions);

        Assert.Equal(scorecard.Id, dash!.ScorecardId);
        Assert.Equal(1, dash.RedCount);
        var atRisk = Assert.Single(dash.AtRiskObjectives);
        Assert.Equal("Hit the number", atRisk.Name);
        Assert.Equal("Red", atRisk.Rag);
    }

    // ──────────── Auth & tenancy ────────────

    [Fact]
    public async Task Scorecards_RequireAuthentication()
    {
        using var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/scorecards");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Scorecards_AreTenantIsolated()
    {
        var scorecard = await CreateScorecard("Tenant A strategy");

        var tenantB = _factory.CreateTenant($"tenant-b-{Guid.NewGuid().ToString()[..6]}");
        using var clientB = _factory.CreateTenantClient(tenantB);

        var listB = await clientB.GetFromJsonAsync<PaginatedResponse<ScorecardSummaryDto>>("/api/scorecards", JsonOptions);
        Assert.DoesNotContain(listB!.Items, s => s.Id == scorecard.Id);

        var getB = await clientB.GetAsync($"/api/scorecards/{scorecard.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getB.StatusCode);
    }
}
