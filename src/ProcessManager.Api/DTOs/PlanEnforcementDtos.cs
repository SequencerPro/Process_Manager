using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

public record PlanCheckResultDto(
    PlanCheckOutcome Outcome,
    string? Message,
    int? CurrentCount,
    int? Limit,
    SubscriptionPlan? SuggestedUpgrade);

public record PlanLimitsDto(
    SubscriptionPlan Plan,
    int? MaxUsers,
    int? MaxProcesses,
    int? MaxSites,
    int? MaxMonthlyExecutions,
    bool AdvancedModulesEnabled);

public record PlanUsageSummaryDto(
    SubscriptionPlan Plan,
    PlanLimitsDto Limits,
    int CurrentUsers,
    int CurrentProcesses,
    int CurrentMonthlyExecutions);
