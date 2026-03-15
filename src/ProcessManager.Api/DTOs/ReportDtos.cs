namespace ProcessManager.Api.DTOs;

// ───── Reports ─────

/// <summary>Overall operational summary — single call for the top KPI row.</summary>
public record ReportSummaryDto(
    int TotalJobs,
    int ActiveJobs,
    int CompletedThisMonth,
    int FailedStepsAllTime,
    double? AvgJobDurationHours);   // null when no completed jobs yet

/// <summary>Count of jobs in each status.</summary>
public record JobStatusBreakdownDto(string Status, int Count);

/// <summary>Per-step performance across all executions ever.</summary>
public record StepPerformanceDto(
    string StepName,
    int Total,
    int Completed,
    int Failed,
    double? AvgDurationMinutes);

/// <summary>A recently completed job with wall-clock duration.</summary>
public record RecentCompletionDto(
    Guid JobId,
    string Code,
    string Name,
    string ProcessName,
    DateTime? StartedAt,
    DateTime CompletedAt,
    double? DurationHours);

/// <summary>Jobs created per calendar day over a rolling window.</summary>
public record ThroughputPointDto(DateOnly Date, int Created, int Completed);

/// <summary>Timing statistics for one step template within a specific process.</summary>
public record StepTimingDto(
    int Sequence,
    string StepCode,
    string StepName,
    int CompletedExecutions,
    double? MinMinutes,
    double? AvgMinutes,
    double? MedianMinutes,
    double? P95Minutes,
    double? MaxMinutes);

/// <summary>
/// Full timing profile for one process: overall job durations (min/avg/median/p95/max)
/// plus a per-step breakdown in sequence order.
/// </summary>
public record ProcessTimingDto(
    Guid ProcessId,
    string Code,
    string Name,
    string ProcessRole,
    int CompletedJobs,
    double? MinHours,
    double? AvgHours,
    double? MedianHours,
    double? P95Hours,
    double? MaxHours,
    List<StepTimingDto> Steps);
