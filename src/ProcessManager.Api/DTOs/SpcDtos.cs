using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────────────── F7 — Statistical Process Control ──────────────────────

// ── SPC Chart DTOs ──────────────────────────────────────────────────────────

public record SpcChartDto(
    Guid     Id,
    Guid     ProcessId,
    string   ProcessName,
    Guid     ContentBlockId,
    string?  ContentBlockLabel,
    string   Name,
    string   ChartType,
    int      SubgroupSize,
    string   ControlLimitSource,
    decimal? UCL,
    decimal? LCL,
    decimal? CL,
    decimal? RangeUCL,
    decimal? RangeLCL,
    decimal? RangeCL,
    decimal? TargetCpk,
    decimal? LSL,
    decimal? USL,
    bool     IsActive,
    int      DataPointCount,
    DateTime CreatedAt
);

public record SpcChartSummaryDto(
    Guid     Id,
    Guid     ProcessId,
    string   ProcessName,
    string?  ContentBlockLabel,
    string   Name,
    string   ChartType,
    bool     IsActive,
    int      DataPointCount,
    decimal? Cp,
    decimal? Cpk,
    int      OutOfControlCount
);

public record CreateSpcChartDto(
    [Required] Guid     ProcessId,
    [Required] Guid     ContentBlockId,
    [Required][StringLength(200)] string Name,
    string   ChartType = "XbarR",
    int      SubgroupSize = 5,
    string   ControlLimitSource = "Calculated",
    decimal? UCL = null,
    decimal? LCL = null,
    decimal? CL = null,
    decimal? RangeUCL = null,
    decimal? RangeLCL = null,
    decimal? RangeCL = null,
    decimal? TargetCpk = null,
    decimal? LSL = null,
    decimal? USL = null
);

public record UpdateSpcChartDto(
    [Required][StringLength(200)] string Name,
    string   ChartType = "XbarR",
    int      SubgroupSize = 5,
    string   ControlLimitSource = "Calculated",
    decimal? UCL = null,
    decimal? LCL = null,
    decimal? CL = null,
    decimal? RangeUCL = null,
    decimal? RangeLCL = null,
    decimal? RangeCL = null,
    decimal? TargetCpk = null,
    decimal? LSL = null,
    decimal? USL = null,
    bool     IsActive = true
);

// ── SPC Data Point DTOs ─────────────────────────────────────────────────────

public record SpcDataPointDto(
    Guid     Id,
    Guid     SpcChartId,
    Guid     StepExecutionId,
    decimal  Value,
    int      SubgroupIndex,
    DateTime CapturedAt
);

// ── SPC Calculation Result DTOs ─────────────────────────────────────────────

public record SpcCalculationResultDto(
    decimal  XBar,
    decimal  RBar,
    decimal  UCL,
    decimal  LCL,
    decimal  CL,
    decimal  RangeUCL,
    decimal  RangeLCL,
    decimal  RangeCL,
    decimal? Cp,
    decimal? Cpk,
    decimal? Pp,
    decimal? Ppk,
    decimal  StdDev,
    int      SubgroupCount,
    int      TotalPoints,
    List<SpcOutOfControlPointDto> OutOfControlPoints
);

public record SpcOutOfControlPointDto(
    int      SubgroupIndex,
    decimal  Value,
    string   Rule,
    string   Description
);

public record SpcSubgroupDto(
    int      Index,
    decimal  Mean,
    decimal  Range,
    List<decimal> Values
);

// ── SPC Data Point Input ────────────────────────────────────────────────────

public record AddSpcDataPointDto(
    [Required] Guid    StepExecutionId,
    [Required] decimal Value
);
