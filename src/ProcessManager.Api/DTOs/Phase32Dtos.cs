using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── Change Order ─────────────────────────────────────────────────────────────

public record ChangeOrderResponseDto(
    Guid Id,
    string Code,
    string Type,
    string Priority,
    string Status,
    string Title,
    string? Description,
    string? Justification,
    string RequestedByUserId,
    string RequestedByDisplayName,
    DateTime RequestedAt,
    DateTime? TargetImplementationDate,
    DateTime? ClosedAt,
    string? RejectionReason,
    int ImpactCount,
    int ApproverCount,
    int TaskCount,
    int TasksCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ChangeOrderSummaryDto(
    Guid Id,
    string Code,
    string Type,
    string Priority,
    string Status,
    string Title,
    string RequestedByDisplayName,
    DateTime RequestedAt,
    DateTime? TargetImplementationDate,
    int ImpactCount,
    int ApproverCount,
    int TaskCount,
    int TasksCompleted,
    DateTime CreatedAt);

public class CreateChangeOrderDto
{
    [Required, StringLength(30)]
    public string Type { get; set; } = "ProcessChange";

    [StringLength(20)]
    public string Priority { get; set; } = "Routine";

    [Required, StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(4000)]
    public string? Justification { get; set; }

    [Required, StringLength(450)]
    public string RequestedByUserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string RequestedByDisplayName { get; set; } = string.Empty;

    public DateTime? TargetImplementationDate { get; set; }
}

public class UpdateChangeOrderDto
{
    [StringLength(500)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(4000)]
    public string? Justification { get; set; }

    [StringLength(20)]
    public string? Priority { get; set; }

    public DateTime? TargetImplementationDate { get; set; }
}

// ── Change Order Impact ──────────────────────────────────────────────────────

public record ChangeOrderImpactResponseDto(
    Guid Id,
    Guid ChangeOrderId,
    string AffectedEntityType,
    Guid AffectedEntityId,
    string? AffectedEntityName,
    string? ImpactDescription,
    string? MitigationPlan,
    DateTime CreatedAt);

public class CreateChangeOrderImpactDto
{
    [Required, StringLength(30)]
    public string AffectedEntityType { get; set; } = string.Empty;

    public Guid AffectedEntityId { get; set; }

    [StringLength(200)]
    public string? AffectedEntityName { get; set; }

    [StringLength(4000)]
    public string? ImpactDescription { get; set; }

    [StringLength(4000)]
    public string? MitigationPlan { get; set; }
}

// ── Change Order Approver ────────────────────────────────────────────────────

public record ChangeOrderApproverResponseDto(
    Guid Id,
    Guid ChangeOrderId,
    string UserId,
    string DisplayName,
    string? Role,
    string Decision,
    DateTime? DecidedAt,
    string? Comments,
    DateTime CreatedAt);

public class AddChangeOrderApproverDto
{
    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Role { get; set; }
}

public class RecordApproverDecisionDto
{
    [Required, StringLength(20)]
    public string Decision { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Comments { get; set; }
}

// ── Change Order Task ────────────────────────────────────────────────────────

public record ChangeOrderTaskResponseDto(
    Guid Id,
    Guid ChangeOrderId,
    string Title,
    string? Description,
    string? AssigneeUserId,
    string? AssigneeDisplayName,
    DateTime? DueDate,
    string Status,
    DateTime? CompletedAt,
    string? CompletedByUserId,
    string? Notes,
    DateTime CreatedAt);

public class CreateChangeOrderTaskDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(450)]
    public string? AssigneeUserId { get; set; }

    [StringLength(200)]
    public string? AssigneeDisplayName { get; set; }

    public DateTime? DueDate { get; set; }
}

public class CompleteChangeOrderTaskDto
{
    [StringLength(4000)]
    public string? Notes { get; set; }
}

// ── Lifecycle DTOs ───────────────────────────────────────────────────────────

public class TransitionChangeOrderDto
{
    [StringLength(4000)]
    public string? Notes { get; set; }
}

public class RejectChangeOrderDto
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}

// ── Dashboard ────────────────────────────────────────────────────────────────

public record ChangeOrderDashboardDto(
    int TotalOpen,
    int TotalClosed,
    int TotalRejected,
    double AvgDaysToClose,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByPriority,
    List<ChangeOrderSummaryDto> OverdueEcos);
