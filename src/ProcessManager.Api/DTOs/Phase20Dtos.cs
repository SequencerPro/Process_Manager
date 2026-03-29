using System.ComponentModel.DataAnnotations;

namespace ProcessManager.Api.DTOs;

// ── MCP Audit Log ──────────────────────────────────────────────────────────

public record McpAuditLogDto(
    Guid Id,
    string ToolName,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string? UserId,
    string? UserDisplayName,
    string? RequestPayload,
    string? ResponseSummary,
    bool IsSuccess,
    string? ErrorMessage,
    long DurationMs,
    DateTime PerformedAt
);

// ── Webhook Subscriptions ──────────────────────────────────────────────────

public record CreateWebhookSubscriptionDto(
    [Required][Url] string Url,
    string? Secret,
    [Required] string EventTypes,
    string? Description
);

public record UpdateWebhookSubscriptionDto(
    [Required][Url] string Url,
    string? Secret,
    [Required] string EventTypes,
    string? Description,
    bool IsActive
);

public record WebhookSubscriptionDto(
    Guid Id,
    string Url,
    bool HasSecret,
    string EventTypes,
    string? Description,
    bool IsActive,
    int DeliveryCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record WebhookDeliveryDto(
    Guid Id,
    Guid WebhookSubscriptionId,
    string EventType,
    string Payload,
    int? StatusCode,
    string? ResponseBody,
    string? ErrorMessage,
    bool IsSuccess,
    int AttemptNumber,
    DateTime CreatedAt,
    DateTime? DeliveredAt
);
