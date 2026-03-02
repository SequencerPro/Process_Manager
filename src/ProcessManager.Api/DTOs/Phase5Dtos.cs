using System.ComponentModel.DataAnnotations;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.DTOs;

// ───── Job ─────

public record CreateJobDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    Guid ProcessId,
    [Range(0, int.MaxValue)] int Priority = 0);

public record UpdateJobDto(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description,
    [Range(0, int.MaxValue)] int Priority = 0);

public record JobResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid ProcessId,
    string ProcessName,
    string Status,
    int Priority,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<StepExecutionResponseDto>? StepExecutions = null);

// ───── Item ─────

public record CreateItemDto(
    Guid KindId,
    Guid GradeId,
    Guid JobId,
    [StringLength(100)] string? SerialNumber = null,
    Guid? BatchId = null);

public record UpdateItemDto(
    string? SerialNumber = null,
    Guid? BatchId = null);

public record ItemResponseDto(
    Guid Id,
    string? SerialNumber,
    Guid KindId,
    string KindName,
    Guid GradeId,
    string GradeName,
    Guid JobId,
    string JobName,
    Guid? BatchId,
    string? BatchCode,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Batch ─────

public record CreateBatchDto(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    Guid KindId,
    Guid GradeId,
    Guid JobId,
    [Range(0, int.MaxValue)] int Quantity = 0);

public record UpdateBatchDto(
    int? Quantity = null);

public record BatchResponseDto(
    Guid Id,
    string Code,
    Guid KindId,
    string KindName,
    Guid GradeId,
    string GradeName,
    Guid JobId,
    string JobName,
    int Quantity,
    string Status,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ───── Step Execution ─────

public record StepExecutionResponseDto(
    Guid Id,
    Guid JobId,
    Guid ProcessStepId,
    int Sequence,
    string StepName,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PortTransactionResponseDto>? PortTransactions = null,
    string? JobCode = null,
    string? JobName = null);

public record UpdateStepExecutionNotesDto(
    string? Notes);

// ───── Port Transaction ─────

public record CreatePortTransactionDto(
    Guid PortId,
    Guid? ItemId = null,
    Guid? BatchId = null,
    int Quantity = 1);

public record PortTransactionResponseDto(
    Guid Id,
    Guid StepExecutionId,
    Guid PortId,
    string PortName,
    string PortDirection,
    Guid? ItemId,
    string? ItemSerialNumber,
    Guid? BatchId,
    string? BatchCode,
    int Quantity,
    DateTime CreatedAt);

// ───── Execution Data ─────

public record CreateExecutionDataDto(
    [Required, StringLength(200, MinimumLength = 1)] string Key,
    [Required, StringLength(1000)] string Value,
    DataValueType DataType = DataValueType.String,
    [StringLength(50)] string? UnitOfMeasure = null);

public record ExecutionDataResponseDto(
    Guid Id,
    string Key,
    string Value,
    DataValueType DataType,
    string? UnitOfMeasure,
    Guid? StepExecutionId,
    Guid? BatchId,
    Guid? ItemId,
    DateTime CreatedAt);
