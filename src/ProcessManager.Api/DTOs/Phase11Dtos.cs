using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ───── Equipment Category ─────

public record EquipmentCategoryCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name);

public record EquipmentCategoryUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name);

public record EquipmentCategoryResponseDto(
    Guid Id,
    string Code,
    string Name,
    int EquipmentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Equipment ─────

public record EquipmentCreateDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    Guid CategoryId,
    [StringLength(200)] string? Location = null,
    [StringLength(200)] string? Manufacturer = null,
    [StringLength(200)] string? Model = null,
    [StringLength(100)] string? SerialNumber = null,
    DateTime? InstallDate = null);

public record EquipmentUpdateDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    Guid CategoryId,
    [StringLength(200)] string? Location = null,
    [StringLength(200)] string? Manufacturer = null,
    [StringLength(200)] string? Model = null,
    [StringLength(100)] string? SerialNumber = null,
    DateTime? InstallDate = null,
    bool IsActive = true);

public record EquipmentSummaryDto(
    Guid Id,
    string Code,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string? Location,
    bool IsActive,
    /// <summary>True when any DowntimeRecord for this equipment has no EndedAt.</summary>
    bool IsCurrentlyDown,
    /// <summary>Type of current downtime (null if not down).</summary>
    string? CurrentDowntimeType,
    /// <summary>Next maintenance task due (null if none).</summary>
    DateTime? NextMaintenanceDue,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record EquipmentResponseDto(
    Guid Id,
    string Code,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string? Location,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    DateTime? InstallDate,
    bool IsActive,
    bool IsCurrentlyDown,
    string? CurrentDowntimeType,
    DateTime? NextMaintenanceDue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<DowntimeRecordResponseDto>? DowntimeRecords = null,
    List<MaintenanceTriggerResponseDto>? MaintenanceTriggers = null,
    List<MaintenanceTaskResponseDto>? MaintenanceTasks = null);

// ───── Downtime Record ─────

public record CreateDowntimeRecordDto(
    [Required] DowntimeType Type,
    DateTime StartedAt,
    [Required, StringLength(2000, MinimumLength = 1)] string Reason);

public record CloseDowntimeRecordDto(
    DateTime EndedAt,
    [StringLength(200)] string? ResolvedBy = null);

public record DowntimeRecordResponseDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    string Type,
    DateTime StartedAt,
    DateTime? EndedAt,
    /// <summary>Duration in minutes (null if still open).</summary>
    double? DurationMinutes,
    string Reason,
    string? ResolvedBy,
    Guid? LinkedMaintenanceTaskId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Maintenance Trigger ─────

public record CreateMaintenanceTriggerDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required] MaintenanceTriggerType TriggerType,
    int? IntervalDays = null,
    int? IntervalUsageCycles = null,
    int AdvanceNoticeDays = 7);

public record UpdateMaintenanceTriggerDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    int? IntervalDays = null,
    int? IntervalUsageCycles = null,
    int AdvanceNoticeDays = 7,
    DateTime? LastTriggeredAt = null,
    DateTime? NextDueAt = null);

public record MaintenanceTriggerResponseDto(
    Guid Id,
    Guid EquipmentId,
    string Title,
    string TriggerType,
    int? IntervalDays,
    int? IntervalUsageCycles,
    DateTime? LastTriggeredAt,
    DateTime? NextDueAt,
    int AdvanceNoticeDays,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Maintenance Task ─────

public record CreateMaintenanceTaskDto(
    Guid EquipmentId,
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required] MaintenanceTaskType Type,
    DateTime DueDate,
    [StringLength(200)] string? AssignedTo = null,
    Guid? TriggerId = null);

public record UpdateMaintenanceTaskDto(
    [Required, StringLength(300, MinimumLength = 1)] string Title,
    [Required] MaintenanceTaskType Type,
    DateTime DueDate,
    [StringLength(200)] string? AssignedTo = null);

public record CompleteMaintenanceTaskDto(
    [StringLength(200)] string? CompletedBy = null,
    [StringLength(2000)] string? Notes = null,
    Guid? LinkedDowntimeRecordId = null);

public record MaintenanceTaskResponseDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    Guid? TriggerId,
    string Title,
    string Type,
    string Status,
    DateTime DueDate,
    string? AssignedTo,
    DateTime? CompletedAt,
    string? CompletedBy,
    string? Notes,
    Guid? LinkedDowntimeRecordId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Production Dashboard ─────

public record WipJobDto(
    Guid JobId,
    string JobCode,
    string JobName,
    string ProcessName,
    string Status,
    DateTime? DueDate,
    DateTime? PlannedStartDate,
    /// <summary>Estimated completion based on PlannedStartDate + remaining step durations.</summary>
    DateTime? ExpectedCompletionDate,
    bool IsLate,
    int DaysLate,
    string CurrentStepName,
    int? CurrentStepExpectedMinutes,
    /// <summary>How long the current step has been running (minutes).</summary>
    double? CurrentStepRunningMinutes,
    /// <summary>Id of equipment assigned to the current step execution.</summary>
    Guid? CurrentEquipmentId,
    string? CurrentEquipmentCode,
    bool CurrentEquipmentDown);

public record BottleneckStepDto(
    Guid StepTemplateId,
    string StepName,
    /// <summary>Number of pending step executions for this template.</summary>
    int PendingCount,
    int? ExpectedDurationMinutes,
    /// <summary>PendingCount / (1 / ExpectedDurationMinutes) — how many minutes of backlog.</summary>
    double BacklogMinutes);

public record ProductionDashboardDto(
    List<WipJobDto> LateJobs,
    List<WipJobDto> ActiveJobs,
    List<BottleneckStepDto> Bottlenecks,
    List<MaintenanceTaskResponseDto> MaintenanceDue,
    int TotalActiveJobs,
    int LateJobCount,
    int EquipmentDownCount);
