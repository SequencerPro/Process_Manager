using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

public record TenantSubscriptionDto(
    Guid Id,
    SubscriptionPlan PlanCode,
    SubscriptionStatus Status,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    DateTime? TrialEndsAt,
    DateTime? CurrentPeriodEnd,
    DateTime? GraceEndsAt,
    int FailedPaymentCount);

public record UpdateSubscriptionPlanDto(SubscriptionPlan PlanCode);

public record UsageMetricDto(
    Guid Id,
    UsageMetricType MetricType,
    int Count,
    DateTime PeriodStart,
    DateTime PeriodEnd);

public record BillingEventDto(
    Guid Id,
    string StripeEventId,
    BillingEventType EventType,
    string? Description,
    DateTime ProcessedAt);

public record BillingDashboardDto(
    TenantSubscriptionDto? Subscription,
    List<UsageMetricDto> CurrentPeriodUsage,
    List<BillingEventDto> RecentEvents);

public record CreatePortalSessionDto(string? ReturnUrl);

public record PortalSessionResultDto(string Url);
