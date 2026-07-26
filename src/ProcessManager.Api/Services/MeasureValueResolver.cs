using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

public interface IMeasureValueResolver
{
    /// <summary>
    /// Resolves the current value of a measure from its configured source, or null
    /// when no data is available (or the source is not yet implemented).
    /// </summary>
    Task<decimal?> ResolveAsync(ObjectiveMeasure measure);
}

/// <summary>
/// Phase 50b: resolves ObjectiveMeasure current values from live operational data.
/// Manual measures read their latest snapshot; auto sources compute from the modules
/// that already capture the underlying data (execution traceability, maturity scoring,
/// accountability, workorders).
/// </summary>
public class MeasureValueResolver : IMeasureValueResolver
{
    private readonly ProcessManagerDbContext _db;
    private readonly ISpcCalculationService _spc;
    private readonly IOeeCalculationService _oee;

    public MeasureValueResolver(
        ProcessManagerDbContext db,
        ISpcCalculationService spc,
        IOeeCalculationService oee)
    {
        _db = db;
        _spc = spc;
        _oee = oee;
    }

    public async Task<decimal?> ResolveAsync(ObjectiveMeasure measure) => measure.SourceType switch
    {
        MeasureSourceType.Manual => await ResolveManualAsync(measure),
        MeasureSourceType.ProcessYield => await ResolveProcessYieldAsync(measure),
        MeasureSourceType.ProcessMaturity => await ResolveProcessMaturityAsync(measure),
        MeasureSourceType.WorkflowThroughput => await ResolveWorkflowThroughputAsync(measure),
        MeasureSourceType.ActionCloseRate => await ResolveActionCloseRateAsync(),
        MeasureSourceType.OpenNonConformances => await ResolveOpenNonConformancesAsync(),
        MeasureSourceType.SpcCapability => await ResolveSpcCapabilityAsync(measure),
        MeasureSourceType.TrainingCompliance => await ResolveTrainingComplianceAsync(measure),
        MeasureSourceType.Oee => await ResolveOeeAsync(measure),
        MeasureSourceType.QualityCost => await ResolveQualityCostAsync(measure),
        MeasureSourceType.PromptMetric => await ResolvePromptMetricAsync(measure),
        _ => null,
    };

    private async Task<decimal?> ResolveManualAsync(ObjectiveMeasure measure)
    {
        var latest = await _db.MeasureSnapshots
            .Where(s => s.MeasureId == measure.Id)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync();
        return latest?.Value;
    }

    /// <summary>
    /// Yield (%) = good items ÷ total items across all jobs of the linked Process.
    /// "Good" means the item's grade matches the Grade id in SourceParameter when one
    /// is configured; otherwise any non-scrapped item counts as good.
    /// </summary>
    private async Task<decimal?> ResolveProcessYieldAsync(ObjectiveMeasure measure)
    {
        if (measure.SourceEntityId is not Guid processId) return null;

        var items = _db.Items.Where(i => i.Job.ProcessId == processId);

        var total = await items.CountAsync();
        if (total == 0) return null;

        int good;
        if (Guid.TryParse(measure.SourceParameter, out var goodGradeId))
            good = await items.CountAsync(i => i.GradeId == goodGradeId);
        else
            good = await items.CountAsync(i => i.Status != ItemStatus.Scrapped);

        return Math.Round((decimal)good / total * 100m, 2);
    }

    /// <summary>Mean Phase 8 maturity score (0–100) across the process's step templates.</summary>
    private async Task<decimal?> ResolveProcessMaturityAsync(ObjectiveMeasure measure)
    {
        if (measure.SourceEntityId is not Guid processId) return null;

        var stepTemplates = await _db.ProcessSteps
            .Where(ps => ps.ProcessId == processId)
            .Include(ps => ps.StepTemplate)
                .ThenInclude(st => st.Contents)
            .Select(ps => ps.StepTemplate)
            .ToListAsync();

        if (stepTemplates.Count == 0) return null;

        var scores = stepTemplates.Select(st => MaturityScoringService.Evaluate(st).Score);
        return Math.Round((decimal)scores.Average(), 2);
    }

    /// <summary>Completed workorders of the linked Workflow in the last 30 days (or SourceParameter days).</summary>
    private async Task<decimal?> ResolveWorkflowThroughputAsync(ObjectiveMeasure measure)
    {
        if (measure.SourceEntityId is not Guid workflowId) return null;

        var windowDays = ParseWindowDays(measure.SourceParameter, defaultDays: 30);
        var since = DateTime.UtcNow.AddDays(-windowDays);

        return await _db.Workorders.CountAsync(w =>
            w.WorkflowId == workflowId &&
            w.CompletedAt != null &&
            w.CompletedAt >= since);
    }

    /// <summary>Percent of action items created in the last 30 days that are complete or verified.</summary>
    private async Task<decimal?> ResolveActionCloseRateAsync()
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var window = _db.ActionItems.Where(a => a.CreatedAt >= since && a.Status != ActionItemStatus.Cancelled);

        var total = await window.CountAsync();
        if (total == 0) return null;

        var closed = await window.CountAsync(a =>
            a.Status == ActionItemStatus.Complete || a.Status == ActionItemStatus.Verified);

        return Math.Round((decimal)closed / total * 100m, 2);
    }

    private async Task<decimal?> ResolveOpenNonConformancesAsync() =>
        await _db.NonConformances.CountAsync(nc =>
            nc.DispositionStatus == DispositionStatus.Pending ||
            nc.DispositionStatus == DispositionStatus.Quarantine);

    /// <summary>Latest Cpk of the linked SpcChart, computed from its data points.</summary>
    private async Task<decimal?> ResolveSpcCapabilityAsync(ObjectiveMeasure measure)
    {
        if (measure.SourceEntityId is not Guid chartId) return null;

        var chart = await _db.SpcCharts
            .Include(c => c.DataPoints)
            .FirstOrDefaultAsync(c => c.Id == chartId);
        if (chart is null) return null;

        var values = chart.DataPoints
            .OrderBy(d => d.SubgroupIndex)
            .Select(d => d.Value)
            .ToList();
        if (values.Count < chart.SubgroupSize) return null;

        return _spc.Calculate(values, chart.SubgroupSize, chart.LSL, chart.USL).Cpk;
    }

    /// <summary>
    /// Percent of competency records that are current (not expired). Superseded records are
    /// excluded; SourceEntityId optionally narrows to one training process.
    /// </summary>
    private async Task<decimal?> ResolveTrainingComplianceAsync(ObjectiveMeasure measure)
    {
        var now = DateTime.UtcNow;
        var records = _db.CompetencyRecords.Where(r => r.Status != CompetencyStatus.Superseded);

        if (measure.SourceEntityId is Guid trainingProcessId)
            records = records.Where(r => r.TrainingProcessId == trainingProcessId);

        var total = await records.CountAsync();
        if (total == 0) return null;

        var current = await records.CountAsync(r =>
            r.Status == CompetencyStatus.Current &&
            (r.ExpiresAt == null || r.ExpiresAt > now));

        return Math.Round((decimal)current / total * 100m, 2);
    }

    /// <summary>Average OEE % over the rolling window, plant-wide or for the linked Equipment.</summary>
    private async Task<decimal?> ResolveOeeAsync(ObjectiveMeasure measure)
    {
        var windowDays = ParseWindowDays(measure.SourceParameter, defaultDays: 30);
        var dashboard = await _oee.GetDashboardAsync(
            DateTime.UtcNow.AddDays(-windowDays), DateTime.UtcNow, measure.SourceEntityId);

        return dashboard.EquipmentCount > 0 ? dashboard.AverageOee : null;
    }

    /// <summary>Total cost of quality recorded in the rolling window.</summary>
    private async Task<decimal?> ResolveQualityCostAsync(ObjectiveMeasure measure)
    {
        var windowDays = ParseWindowDays(measure.SourceParameter, defaultDays: 30);
        var since = DateTime.UtcNow.AddDays(-windowDays);

        var costs = await _db.QualityCosts
            .Where(q => q.RecordedAt >= since)
            .Select(q => q.Amount)
            .ToListAsync();

        return costs.Count > 0 ? costs.Sum() : null;
    }

    /// <summary>
    /// Mean numeric prompt response for the linked content block (StepTemplateContent or
    /// ProcessStepContent) over the rolling window. Non-numeric responses are ignored.
    /// </summary>
    private async Task<decimal?> ResolvePromptMetricAsync(ObjectiveMeasure measure)
    {
        if (measure.SourceEntityId is not Guid contentBlockId) return null;

        var windowDays = ParseWindowDays(measure.SourceParameter, defaultDays: 30);
        var since = DateTime.UtcNow.AddDays(-windowDays);

        var responses = await _db.PromptResponses
            .Where(r => (r.StepTemplateContentId == contentBlockId || r.ProcessStepContentId == contentBlockId)
                        && r.RespondedAt >= since)
            .Select(r => r.ResponseValue)
            .ToListAsync();

        var values = responses
            .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        return values.Count > 0 ? Math.Round(values.Average(), 4) : null;
    }

    private static int ParseWindowDays(string? sourceParameter, int defaultDays) =>
        int.TryParse(sourceParameter, out var days) && days > 0 ? days : defaultDays;

    // ── RAG evaluation ───────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a resolved value against a measure's thresholds, respecting direction.
    /// Falls back to target attainment (Green/Amber) when thresholds are not set.
    /// Returns "Green", "Amber", "Red", or "NoData".
    /// </summary>
    public static string EvaluateRag(ObjectiveMeasure measure, decimal? value)
    {
        if (value is not decimal v) return "NoData";

        switch (measure.Direction)
        {
            case MeasureDirection.HigherIsBetter:
                if (measure.GreenThreshold is decimal gh && v >= gh) return "Green";
                if (measure.RedThreshold is decimal rh && v <= rh) return "Red";
                if (measure.GreenThreshold is null && measure.RedThreshold is null)
                    return v >= measure.TargetValue ? "Green" : "Amber";
                return "Amber";

            case MeasureDirection.LowerIsBetter:
                if (measure.GreenThreshold is decimal gl && v <= gl) return "Green";
                if (measure.RedThreshold is decimal rl && v >= rl) return "Red";
                if (measure.GreenThreshold is null && measure.RedThreshold is null)
                    return v <= measure.TargetValue ? "Green" : "Amber";
                return "Amber";

            case MeasureDirection.TargetIsBest:
                var deviation = Math.Abs(v - measure.TargetValue);
                if (measure.GreenThreshold is decimal gt && deviation <= gt) return "Green";
                if (measure.RedThreshold is decimal rt && deviation >= rt) return "Red";
                if (measure.GreenThreshold is null && measure.RedThreshold is null)
                    return deviation == 0 ? "Green" : "Amber";
                return "Amber";

            default:
                return "NoData";
        }
    }
}
