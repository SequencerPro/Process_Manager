using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ──────────── Workorder ────────────

public record CreateWorkorderDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    Guid WorkflowId,
    [Range(0, int.MaxValue)] int Priority = 0);

public record UpdateWorkorderDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Priority = 0);

public record WorkorderResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid WorkflowId,
    string WorkflowName,
    int WorkflowVersion,
    string Status,
    int Priority,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<WorkorderJobResponseDto>? Jobs = null);

// ──────────── WorkorderJob ────────────

public record WorkorderJobResponseDto(
    Guid Id,
    Guid WorkorderId,
    Guid WorkflowProcessId,
    string ProcessName,
    string ProcessCode,
    Guid JobId,
    string JobCode,
    string JobName,
    string JobStatus,
    bool CanStart);

// ──────────── Advance (Manual/GradeBased routing) ────────────

public record AdvanceWorkorderDto(Guid WorkflowLinkId);
