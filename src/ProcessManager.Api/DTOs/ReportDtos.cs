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
