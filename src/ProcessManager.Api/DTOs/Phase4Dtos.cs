using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────── Workflow ────────────

public record CreateWorkflowDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description = null);

public record UpdateWorkflowDto(
    [StringLength(200, MinimumLength = 1)] string? Name = null,
    [StringLength(2000)] string? Description = null,
    bool? IsActive = null);

public record WorkflowResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<WorkflowProcessResponseDto>? Processes = null,
    List<WorkflowLinkResponseDto>? Links = null);

// ──────────── WorkflowProcess ────────────

public record AddWorkflowProcessDto(
    Guid ProcessId,
    bool IsEntryPoint = false,
    int SortOrder = 0,
    double PositionX = 0,
    double PositionY = 0);

public record UpdateWorkflowProcessDto(
    bool? IsEntryPoint = null,
    int? SortOrder = null,
    double? PositionX = null,
    double? PositionY = null);

public record WorkflowProcessResponseDto(
    Guid Id,
    Guid WorkflowId,
    Guid ProcessId,
    string ProcessName,
    string ProcessCode,
    bool IsEntryPoint,
    int SortOrder,
    double PositionX,
    double PositionY,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ──────────── Bulk Position Update ────────────

public record UpdateWorkflowProcessPositionsDto(
    List<WorkflowProcessPositionDto> Positions);

public record WorkflowProcessPositionDto(
    Guid WorkflowProcessId,
    double PositionX,
    double PositionY);

// ──────────── WorkflowLink ────────────

public record CreateWorkflowLinkDto(
    Guid SourceWorkflowProcessId,
    Guid TargetWorkflowProcessId,
    RoutingType RoutingType = RoutingType.Always,
    [StringLength(200)] string? Name = null,
    [Range(0, int.MaxValue)] int SortOrder = 0,
    List<Guid>? ConditionGradeIds = null);

public record UpdateWorkflowLinkDto(
    [StringLength(200)] string? Name = null,
    [Range(0, int.MaxValue)] int? SortOrder = null);

public record WorkflowLinkResponseDto(
    Guid Id,
    Guid WorkflowId,
    Guid SourceWorkflowProcessId,
    string SourceProcessName,
    Guid TargetWorkflowProcessId,
    string TargetProcessName,
    RoutingType RoutingType,
    string? Name,
    int SortOrder,
    List<WorkflowLinkConditionResponseDto>? Conditions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ──────────── WorkflowLinkCondition ────────────

public record AddWorkflowLinkConditionDto(Guid GradeId);

public record WorkflowLinkConditionResponseDto(
    Guid Id,
    Guid WorkflowLinkId,
    Guid GradeId,
    string GradeCode,
    string GradeName);

// ──────────── Workflow Validation ────────────

public record WorkflowValidationResultDto(
    bool IsValid,
    List<string> Errors,
    List<string> Warnings);
