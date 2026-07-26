namespace ProcessManager.Api.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// Phase 50: Strategy Management — Balanced Scorecard
// ─────────────────────────────────────────────────────────────────────────────

// ── Scorecard ────────────────────────────────────────────────────────────────

public record ScorecardSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    Guid? OwnerOrgUnitId,
    int PerspectiveCount,
    int ObjectiveCount,
    int MeasureCount,
    DateTime CreatedAt);

public record ScorecardResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? MissionStatement,
    string? VisionStatement,
    string? StrategyNotes,
    string Status,
    Guid? OwnerOrgUnitId,
    List<PerspectiveResponseDto> Perspectives,
    List<CauseLinkResponseDto> CauseLinks,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateScorecardDto(
    string Name,
    string? MissionStatement,
    string? VisionStatement,
    string? StrategyNotes,
    Guid? OwnerOrgUnitId);

public record UpdateScorecardDto(
    string? Name,
    string? MissionStatement,
    string? VisionStatement,
    string? StrategyNotes,
    string? Status,
    Guid? OwnerOrgUnitId);

// ── Perspective ──────────────────────────────────────────────────────────────

public record PerspectiveResponseDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    List<ObjectiveResponseDto> Objectives);

public record CreatePerspectiveDto(
    string Name,
    string? Description,
    int SortOrder);

public record UpdatePerspectiveDto(
    string? Name,
    string? Description,
    int? SortOrder);

// ── Objective ────────────────────────────────────────────────────────────────

public record ObjectiveResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? OwnerOrgUnitId,
    string Status,
    DateTime? TargetDate,
    int SortOrder,
    List<MeasureResponseDto> Measures,
    List<ProcessLinkResponseDto> ProcessLinks);

public record CreateObjectiveDto(
    string Name,
    string? Description,
    Guid? OwnerOrgUnitId,
    DateTime? TargetDate,
    int SortOrder);

public record UpdateObjectiveDto(
    string? Name,
    string? Description,
    Guid? OwnerOrgUnitId,
    string? Status,
    DateTime? TargetDate,
    int? SortOrder);

// ── Measure ──────────────────────────────────────────────────────────────────

public record MeasureResponseDto(
    Guid Id,
    string Name,
    string? Units,
    string Direction,
    decimal? BaselineValue,
    decimal TargetValue,
    decimal? GreenThreshold,
    decimal? RedThreshold,
    string SourceType,
    Guid? SourceEntityId,
    string? SourceParameter,
    decimal? CurrentValue,
    string Rag,
    DateTime? LatestSnapshotAt,
    List<decimal> Trend);

public record CreateMeasureDto(
    string Name,
    string? Units,
    string Direction,
    decimal? BaselineValue,
    decimal TargetValue,
    decimal? GreenThreshold,
    decimal? RedThreshold,
    string SourceType,
    Guid? SourceEntityId,
    string? SourceParameter);

public record UpdateMeasureDto(
    string? Name,
    string? Units,
    string? Direction,
    decimal? BaselineValue,
    decimal? TargetValue,
    decimal? GreenThreshold,
    decimal? RedThreshold,
    string? SourceType,
    Guid? SourceEntityId,
    string? SourceParameter);

// ── Snapshots ────────────────────────────────────────────────────────────────

public record MeasureSnapshotResponseDto(
    Guid Id,
    Guid MeasureId,
    decimal Value,
    DateTime CapturedAt,
    string CaptureSource,
    string? Note);

public record CreateMeasureSnapshotDto(
    decimal Value,
    DateTime? CapturedAt,
    string? Note);

// ── Links ────────────────────────────────────────────────────────────────────

public record CauseLinkResponseDto(
    Guid Id,
    Guid SourceObjectiveId,
    Guid TargetObjectiveId,
    string? Description);

public record CreateCauseLinkDto(
    Guid SourceObjectiveId,
    Guid TargetObjectiveId,
    string? Description);

public record ProcessLinkResponseDto(
    Guid Id,
    Guid? ProcessId,
    string? ProcessName,
    Guid? WorkflowId,
    string? WorkflowName,
    string? Note);

public record CreateProcessLinkDto(
    Guid? ProcessId,
    Guid? WorkflowId,
    string? Note);

// ── Prompt-metric lookup ─────────────────────────────────────────────────────

/// <summary>A numeric data-collection prompt that a PromptMetric measure can read from.</summary>
public record PromptMetricOptionDto(
    Guid Id,
    string Label,
    string StepTemplateName,
    string? Units);

// ── Strategy map ─────────────────────────────────────────────────────────────

public record StrategyMapDto(
    Guid ScorecardId,
    string ScorecardName,
    List<StrategyMapNodeDto> Nodes,
    List<CauseLinkResponseDto> Edges);

public record StrategyMapNodeDto(
    Guid ObjectiveId,
    string Code,
    string Name,
    Guid PerspectiveId,
    string PerspectiveName,
    int PerspectiveSortOrder,
    string Status,
    string Rag);

// ── Dashboard widget (GET /api/scorecards/dashboard) ─────────────────────────

/// <summary>
/// Compact strategy snapshot for the home dashboard: the headline active scorecard's
/// RAG rollup plus its most off-track objectives. Null fields when no active scorecard.
/// </summary>
public record ScorecardDashboardDto(
    Guid? ScorecardId,
    string? ScorecardCode,
    string? ScorecardName,
    int ActiveScorecardCount,
    int GreenCount,
    int AmberCount,
    int RedCount,
    int NoDataCount,
    List<AtRiskObjectiveDto> AtRiskObjectives);

public record AtRiskObjectiveDto(
    Guid ObjectiveId,
    string Code,
    string Name,
    string PerspectiveName,
    string Rag);

// ── Rollups (GET /{id}/status) ───────────────────────────────────────────────

public record ScorecardStatusDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    int GreenCount,
    int AmberCount,
    int RedCount,
    int NoDataCount,
    List<PerspectiveResponseDto> Perspectives);
