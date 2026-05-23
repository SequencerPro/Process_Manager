using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Services;

/// <summary>
/// Pure-domain analyzer that turns a flat list of <see cref="StepExecutionPhaseEvent"/>
/// rows into per-phase timing aggregates: total/median duration and statistical
/// outliers (visits longer than mean + 2σ). Feeds the OEE "median phase
/// duration" widget (Phase 36.4 / T4.5).
///
/// No DB, no DTOs — callers map the result into their own response types.
/// Only <i>completed</i> visits (those with an ExitedAt) are considered; an
/// open phase the operator is still in has no measurable duration yet.
/// </summary>
public static class PhaseTimingAnalyzer
{
    public static PhaseTimingReport Analyze(IEnumerable<StepExecutionPhaseEvent> events)
    {
        if (events is null) throw new ArgumentNullException(nameof(events));

        var completed = events
            .Where(e => e.ExitedAt.HasValue && e.ExitedAt.Value >= e.EnteredAt)
            .ToList();

        var perPhase = new List<PhaseTimingStat>();
        var outliers = new List<PhaseTimingOutlier>();

        foreach (var group in completed.GroupBy(e => e.Phase).OrderBy(g => g.Key))
        {
            var durations = group
                .Select(e => (e.ExitedAt!.Value - e.EnteredAt).TotalSeconds)
                .OrderBy(d => d)
                .ToList();

            if (durations.Count == 0) continue;

            var total = durations.Sum();
            var mean = total / durations.Count;
            var median = Median(durations);
            var stdDev = StdDev(durations, mean);

            perPhase.Add(new PhaseTimingStat(
                group.Key,
                durations.Count,
                TotalSeconds: total,
                MeanSeconds: mean,
                MedianSeconds: median,
                StdDevSeconds: stdDev));

            // Outlier threshold: mean + 2σ. Require ≥3 samples and a non-zero
            // spread so we don't flag noise on tiny data sets.
            if (durations.Count >= 3 && stdDev > 0)
            {
                var threshold = mean + 2 * stdDev;
                foreach (var e in group.Where(e => (e.ExitedAt!.Value - e.EnteredAt).TotalSeconds > threshold))
                {
                    outliers.Add(new PhaseTimingOutlier(
                        e.Id,
                        e.StepExecutionId,
                        group.Key,
                        (e.ExitedAt!.Value - e.EnteredAt).TotalSeconds,
                        threshold));
                }
            }
        }

        return new PhaseTimingReport(perPhase, outliers);
    }

    private static double Median(IReadOnlyList<double> sorted)
    {
        var n = sorted.Count;
        if (n == 0) return 0;
        return n % 2 == 1
            ? sorted[n / 2]
            : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
    }

    private static double StdDev(IReadOnlyList<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return Math.Sqrt(variance);
    }
}

public sealed record PhaseTimingStat(
    ExecutionPhase Phase,
    int SampleCount,
    double TotalSeconds,
    double MeanSeconds,
    double MedianSeconds,
    double StdDevSeconds);

public sealed record PhaseTimingOutlier(
    Guid PhaseEventId,
    Guid StepExecutionId,
    ExecutionPhase Phase,
    double DurationSeconds,
    double ThresholdSeconds);

public sealed record PhaseTimingReport(
    IReadOnlyList<PhaseTimingStat> PerPhase,
    IReadOnlyList<PhaseTimingOutlier> Outliers);
