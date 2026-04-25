using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

public record CreateCheckoutSessionDto(
    SubscriptionPlan TargetPlan,
    string? SuccessUrl,
    string? CancelUrl,
    string? CouponCode);

public record CheckoutSessionResultDto(string Url, string SessionId);

public record ChangePlanDto(
    SubscriptionPlan TargetPlan,
    string? Reason);

public record ChangePlanResultDto(
    SubscriptionPlan FromPlan,
    SubscriptionPlan ToPlan,
    bool IsDowngrade,
    string? DowngradeWarning);

public record PlanChangeLogDto(
    Guid Id,
    SubscriptionPlan FromPlan,
    SubscriptionPlan ToPlan,
    DateTime ChangedAt,
    string? ChangedByUserId,
    string? Reason);

public record PlanComparisonDto(
    SubscriptionPlan Plan,
    string Name,
    string PriceLabel,
    int? MaxUsers,
    int? MaxProcesses,
    int? MaxSites,
    int? MaxMonthlyExecutions,
    bool AdvancedModulesEnabled,
    bool IsCurrent);

public record DowngradeCheckDto(
    bool HasExcessUsage,
    List<DowngradeWarningItem> Warnings);

public record DowngradeWarningItem(
    string Resource,
    int CurrentUsage,
    int TargetLimit);
