using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 36.4 (T4.5) — pure unit tests for PhaseTimingAnalyzer. No DB.
/// </summary>
public class PhaseTimingAnalyzerTests
{
    private static StepExecutionPhaseEvent Evt(
        ExecutionPhase phase, double seconds, Guid? seId = null)
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        return new StepExecutionPhaseEvent
        {
            Id = Guid.NewGuid(),
            StepExecutionId = seId ?? Guid.NewGuid(),
            Phase = phase,
            EnteredAt = start,
            ExitedAt = seconds >= 0 ? start.AddSeconds(seconds) : (DateTime?)null,
        };
    }

    [Fact]
    public void Empty_input_yields_empty_report()
    {
        var report = PhaseTimingAnalyzer.Analyze(Array.Empty<StepExecutionPhaseEvent>());
        Assert.Empty(report.PerPhase);
        Assert.Empty(report.Outliers);
    }

    [Fact]
    public void Open_phase_events_are_excluded()
    {
        // ExitedAt == null → no measurable duration.
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Setup, -1) // open
        });
        Assert.Empty(report.PerPhase);
    }

    [Fact]
    public void Computes_median_and_mean_per_phase()
    {
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 20),
            Evt(ExecutionPhase.Execution, 30),
        });

        var stat = Assert.Single(report.PerPhase);
        Assert.Equal(ExecutionPhase.Execution, stat.Phase);
        Assert.Equal(3, stat.SampleCount);
        Assert.Equal(60, stat.TotalSeconds);
        Assert.Equal(20, stat.MeanSeconds);
        Assert.Equal(20, stat.MedianSeconds);
    }

    [Fact]
    public void Median_handles_even_sample_count()
    {
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Setup, 10),
            Evt(ExecutionPhase.Setup, 20),
            Evt(ExecutionPhase.Setup, 30),
            Evt(ExecutionPhase.Setup, 40),
        });

        var stat = Assert.Single(report.PerPhase);
        Assert.Equal(25, stat.MedianSeconds); // (20+30)/2
    }

    [Fact]
    public void Separates_stats_by_phase()
    {
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Setup, 5),
            Evt(ExecutionPhase.Safety, 50),
            Evt(ExecutionPhase.Safety, 60),
        });

        Assert.Equal(2, report.PerPhase.Count);
        Assert.Contains(report.PerPhase, p => p.Phase == ExecutionPhase.Setup && p.SampleCount == 1);
        Assert.Contains(report.PerPhase, p => p.Phase == ExecutionPhase.Safety && p.SampleCount == 2);
    }

    [Fact]
    public void Flags_outlier_beyond_two_sigma()
    {
        // A tight cluster plus one huge value. The huge one should exceed mean+2σ.
        var events = new List<StepExecutionPhaseEvent>
        {
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 10),
            Evt(ExecutionPhase.Execution, 200), // outlier
        };

        var report = PhaseTimingAnalyzer.Analyze(events);

        var outlier = Assert.Single(report.Outliers);
        Assert.Equal(ExecutionPhase.Execution, outlier.Phase);
        Assert.Equal(200, outlier.DurationSeconds);
        Assert.True(outlier.DurationSeconds > outlier.ThresholdSeconds);
    }

    [Fact]
    public void No_outliers_flagged_for_uniform_data()
    {
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Setup, 10),
            Evt(ExecutionPhase.Setup, 10),
            Evt(ExecutionPhase.Setup, 10),
            Evt(ExecutionPhase.Setup, 10),
        });

        Assert.Empty(report.Outliers);
    }

    [Fact]
    public void Small_sample_never_flags_outliers()
    {
        // Only 2 samples — below the ≥3 guard, so no outlier even with spread.
        var report = PhaseTimingAnalyzer.Analyze(new[]
        {
            Evt(ExecutionPhase.Setup, 1),
            Evt(ExecutionPhase.Setup, 1000),
        });

        Assert.Empty(report.Outliers);
    }

    [Fact]
    public void Null_input_throws()
    {
        Assert.Throws<ArgumentNullException>(() => PhaseTimingAnalyzer.Analyze(null!));
    }
}
