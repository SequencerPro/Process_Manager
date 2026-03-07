using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── Query ────────────────────

public record AnalyticsQueryDto(
    /// <summary>"RunOverTime" (more types to follow)</summary>
    [Required] string ChartType,
    DateTime StartDate,
    DateTime EndDate,
    /// <summary>Temporal bucket width in minutes (e.g. 60, 480, 1440).</summary>
    [Range(1, 525600)] int BucketSizeMinutes,
    [Required, MinLength(1), MaxLength(6)] List<AnalyticsSeriesRequestDto> Series
);

public record AnalyticsSeriesRequestDto(
    Guid ContentId,
    [Required, StringLength(300, MinimumLength = 1)] string Label,
    [Required, StringLength(20)] string Color,
    /// <summary>0 = left Y axis, 1 = right Y axis.</summary>
    [Range(0, 1)] int YAxis
);

// ──────────────────── Result ────────────────────

public record AnalyticsQueryResultDto(
    List<AnalyticsSeriesResultDto> Series,
    List<AnalyticsBucketRowDto> Rows,
    string ChartType,
    int BucketSizeMinutes,
    int TotalResponses
);

public record AnalyticsSeriesResultDto(
    Guid ContentId,
    string Label,
    string Color,
    int YAxis,
    string? Units
);

/// <summary>
/// One time bucket. Values keyed by ContentId.ToString().
/// Null means no data was collected in that bucket for that series.
/// </summary>
public record AnalyticsBucketRowDto(
    DateTime Bucket,
    Dictionary<string, double?> Values
);
