using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ──────────── WorkflowSchedule ────────────

public record CreateWorkflowScheduleDto(
    Guid WorkflowId,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    ScheduleRecurrenceType RecurrenceType,
    [Range(1, 1000)] int RecurrenceInterval = 1,
    int? DayOfWeek = null,
    int? DayOfMonth = null,
    DateTime StartDate = default,
    DateTime? EndDate = null,
    [StringLength(500)] string? SubjectTemplate = null,
    bool IsActive = true);

public record UpdateWorkflowScheduleDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    ScheduleRecurrenceType RecurrenceType,
    [Range(1, 1000)] int RecurrenceInterval = 1,
    int? DayOfWeek = null,
    int? DayOfMonth = null,
    DateTime StartDate = default,
    DateTime? EndDate = null,
    [StringLength(500)] string? SubjectTemplate = null,
    bool IsActive = true);

public record WorkflowScheduleResponseDto(
    Guid Id,
    Guid WorkflowId,
    string WorkflowName,
    string Name,
    string RecurrenceType,
    int RecurrenceInterval,
    int? DayOfWeek,
    int? DayOfMonth,
    DateTime StartDate,
    DateTime? EndDate,
    string SubjectTemplate,
    bool IsActive,
    DateTime? NextRunAt,
    DateTime? LastRunAt,
    int WorkorderCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
