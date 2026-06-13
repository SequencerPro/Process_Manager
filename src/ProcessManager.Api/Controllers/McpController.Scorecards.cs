using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Phase 50: Balanced Scorecard MCP tools — strategy status and at-risk objectives
/// for the BYOAI surface ("how are we tracking against strategy?").
/// </summary>
public partial class McpController
{
    private async Task<string> ToolGetScorecardStatus(JsonElement args)
    {
        if (_measureResolver is null)
            return "Error: measure resolution is not available on this server.";

        var code = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("code", out var c)
            ? c.GetString()
            : null;

        var query = _db.Scorecards
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.Measures)
                        .ThenInclude(m => m.Snapshots)
            .AsQueryable();

        var scorecard = code is not null
            ? await query.FirstOrDefaultAsync(s => s.Code == code)
            : await query.Where(s => s.Status == ScorecardStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

        if (scorecard is null)
            return code is not null
                ? $"Error: no scorecard with code '{code}'."
                : "Error: no active scorecard exists. Create one under Strategy → Balanced Scorecard.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Balanced Scorecard: {scorecard.Name} (`{scorecard.Code}`) — {scorecard.Status}\n");
        if (!string.IsNullOrWhiteSpace(scorecard.MissionStatement))
            sb.AppendLine($"**Mission:** {scorecard.MissionStatement}");
        if (!string.IsNullOrWhiteSpace(scorecard.VisionStatement))
            sb.AppendLine($"**Vision:** {scorecard.VisionStatement}");
        sb.AppendLine();

        int green = 0, amber = 0, red = 0, noData = 0;
        var sections = new StringBuilder();

        foreach (var perspective in scorecard.Perspectives.OrderBy(p => p.SortOrder))
        {
            sections.AppendLine($"### {perspective.Name}\n");
            if (perspective.Objectives.Count == 0)
            {
                sections.AppendLine("_No objectives._\n");
                continue;
            }

            sections.AppendLine("| Objective | Measure | Current | Target | Status |");
            sections.AppendLine("|---|---|---|---|---|");

            foreach (var objective in perspective.Objectives.OrderBy(o => o.SortOrder))
            {
                if (objective.Measures.Count == 0)
                {
                    sections.AppendLine($"| {objective.Name} (`{objective.Code}`) | — | — | — | NoData |");
                    noData++;
                    continue;
                }

                foreach (var measure in objective.Measures)
                {
                    var value = await _measureResolver.ResolveAsync(measure);
                    var rag = MeasureValueResolver.EvaluateRag(measure, value);
                    switch (rag)
                    {
                        case "Green": green++; break;
                        case "Amber": amber++; break;
                        case "Red": red++; break;
                        default: noData++; break;
                    }

                    sections.AppendLine(
                        $"| {objective.Name} (`{objective.Code}`) | {measure.Name} " +
                        $"| {FormatMeasureValue(value, measure.Units)} " +
                        $"| {FormatMeasureValue(measure.TargetValue, measure.Units)} | **{rag}** |");
                }
            }
            sections.AppendLine();
        }

        sb.AppendLine($"**Rollup:** {green} Green · {amber} Amber · {red} Red · {noData} No data\n");
        sb.Append(sections);

        return sb.ToString();
    }

    private async Task<string> ToolListAtRiskObjectives(JsonElement args)
    {
        if (_measureResolver is null)
            return "Error: measure resolution is not available on this server.";

        var includeAmber = !(args.ValueKind == JsonValueKind.Object
            && args.TryGetProperty("include_amber", out var ia)
            && ia.GetString() == "false");

        var scorecards = await _db.Scorecards
            .Where(s => s.Status == ScorecardStatus.Active)
            .Include(s => s.Perspectives)
                .ThenInclude(p => p.Objectives)
                    .ThenInclude(o => o.Measures)
                        .ThenInclude(m => m.Snapshots)
            .ToListAsync();

        if (scorecards.Count == 0)
            return "No active scorecards exist.";

        var rows = new List<string>();

        foreach (var scorecard in scorecards)
        foreach (var perspective in scorecard.Perspectives.OrderBy(p => p.SortOrder))
        foreach (var objective in perspective.Objectives.OrderBy(o => o.SortOrder))
        foreach (var measure in objective.Measures)
        {
            var value = await _measureResolver.ResolveAsync(measure);
            var rag = MeasureValueResolver.EvaluateRag(measure, value);
            if (rag != "Red" && !(includeAmber && rag == "Amber")) continue;

            var gap = value is decimal v ? (measure.TargetValue - v).ToString("0.##") : "—";
            rows.Add(
                $"| `{scorecard.Code}` | {perspective.Name} | {objective.Name} (`{objective.Code}`) " +
                $"| {measure.Name} | {FormatMeasureValue(value, measure.Units)} " +
                $"| {FormatMeasureValue(measure.TargetValue, measure.Units)} | {gap} | **{rag}** |");
        }

        if (rows.Count == 0)
            return includeAmber
                ? "All measured objectives are Green — no objectives are at risk."
                : "No objectives are Red.";

        var sb = new StringBuilder();
        sb.AppendLine($"## At-Risk Strategic Objectives ({rows.Count} off-track measure{(rows.Count == 1 ? "" : "s")})\n");
        sb.AppendLine("| Scorecard | Perspective | Objective | Measure | Current | Target | Gap | Status |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var row in rows) sb.AppendLine(row);
        sb.AppendLine();
        sb.AppendLine("_Consider launching initiatives (action items) against the Red objectives._");

        return sb.ToString();
    }

    private static string FormatMeasureValue(decimal? value, string? units) =>
        value is decimal v ? $"{v:0.##}{units}" : "—";
}
