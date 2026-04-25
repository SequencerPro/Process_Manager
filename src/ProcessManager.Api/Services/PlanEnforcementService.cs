using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Services;

public enum PlanResource
{
    Users,
    Processes,
    Sites,
    MonthlyExecutions,
    AdvancedModules
}

public enum PlanCheckOutcome
{
    Allowed,
    AtLimit,
    Blocked
}

public record PlanCheckResult(
    PlanCheckOutcome Outcome,
    string? Message = null,
    int? CurrentCount = null,
    int? Limit = null,
    SubscriptionPlan? SuggestedUpgrade = null);

public record PlanLimits(
    int? MaxUsers,
    int? MaxProcesses,
    int? MaxSites,
    int? MaxMonthlyExecutions,
    bool AdvancedModulesEnabled);

public interface IPlanEnforcementService
{
    Task<PlanCheckResult> CheckAsync(PlanResource resource);
    PlanLimits GetLimitsForPlan(SubscriptionPlan plan);
    Task<SubscriptionPlan> GetCurrentPlanAsync();
}

public class PlanEnforcementService : IPlanEnforcementService
{
    private readonly ProcessManagerDbContext _db;
    private readonly ITenantContext _tenantContext;

    private static readonly Dictionary<SubscriptionPlan, PlanLimits> PlanDefinitions = new()
    {
        [SubscriptionPlan.Trial] = new(MaxUsers: 3, MaxProcesses: 1, MaxSites: 1, MaxMonthlyExecutions: 50, AdvancedModulesEnabled: false),
        [SubscriptionPlan.Starter] = new(MaxUsers: 25, MaxProcesses: null, MaxSites: 1, MaxMonthlyExecutions: null, AdvancedModulesEnabled: false),
        [SubscriptionPlan.Professional] = new(MaxUsers: 100, MaxProcesses: null, MaxSites: 3, MaxMonthlyExecutions: null, AdvancedModulesEnabled: true),
        [SubscriptionPlan.Enterprise] = new(MaxUsers: null, MaxProcesses: null, MaxSites: null, MaxMonthlyExecutions: null, AdvancedModulesEnabled: true),
    };

    public PlanEnforcementService(ProcessManagerDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public PlanLimits GetLimitsForPlan(SubscriptionPlan plan) =>
        PlanDefinitions.GetValueOrDefault(plan, PlanDefinitions[SubscriptionPlan.Trial]);

    public async Task<SubscriptionPlan> GetCurrentPlanAsync()
    {
        var sub = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);
        return sub?.PlanCode ?? SubscriptionPlan.Trial;
    }

    public async Task<PlanCheckResult> CheckAsync(PlanResource resource)
    {
        var sub = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.CurrentTenantId);

        // No subscription record means legacy/unseeded tenant — allow everything
        if (sub is null)
            return new PlanCheckResult(PlanCheckOutcome.Allowed);

        var plan = sub.PlanCode;
        var limits = GetLimitsForPlan(plan);

        return resource switch
        {
            PlanResource.Users => await CheckUsersAsync(limits, plan),
            PlanResource.Processes => await CheckProcessesAsync(limits, plan),
            PlanResource.Sites => new PlanCheckResult(PlanCheckOutcome.Allowed),
            PlanResource.MonthlyExecutions => await CheckMonthlyExecutionsAsync(limits, plan),
            PlanResource.AdvancedModules => CheckAdvancedModules(limits, plan),
            _ => new PlanCheckResult(PlanCheckOutcome.Allowed)
        };
    }

    private async Task<PlanCheckResult> CheckUsersAsync(PlanLimits limits, SubscriptionPlan plan)
    {
        if (limits.MaxUsers is null)
            return new PlanCheckResult(PlanCheckOutcome.Allowed);

        var currentCount = await _db.Users
            .Where(u => u.TenantId == _tenantContext.CurrentTenantId)
            .CountAsync();

        if (currentCount >= limits.MaxUsers)
        {
            var upgrade = GetNextPlan(plan);
            return new PlanCheckResult(
                PlanCheckOutcome.Blocked,
                $"You've reached the {limits.MaxUsers}-user limit on {plan}. Upgrade to {upgrade} for more users.",
                currentCount,
                limits.MaxUsers,
                upgrade);
        }

        return new PlanCheckResult(PlanCheckOutcome.Allowed, CurrentCount: currentCount, Limit: limits.MaxUsers);
    }

    private async Task<PlanCheckResult> CheckProcessesAsync(PlanLimits limits, SubscriptionPlan plan)
    {
        if (limits.MaxProcesses is null)
            return new PlanCheckResult(PlanCheckOutcome.Allowed);

        var currentCount = await _db.Processes
            .Where(p => !p.IsSystemContent)
            .CountAsync();

        if (currentCount >= limits.MaxProcesses)
        {
            var upgrade = GetNextPlan(plan);
            return new PlanCheckResult(
                PlanCheckOutcome.Blocked,
                $"You've reached the {limits.MaxProcesses}-process limit on {plan}. Upgrade to {upgrade} for unlimited processes.",
                currentCount,
                limits.MaxProcesses,
                upgrade);
        }

        return new PlanCheckResult(PlanCheckOutcome.Allowed, CurrentCount: currentCount, Limit: limits.MaxProcesses);
    }

    private async Task<PlanCheckResult> CheckMonthlyExecutionsAsync(PlanLimits limits, SubscriptionPlan plan)
    {
        if (limits.MaxMonthlyExecutions is null)
            return new PlanCheckResult(PlanCheckOutcome.Allowed);

        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var metric = await _db.UsageMetrics
            .FirstOrDefaultAsync(m =>
                m.MetricType == UsageMetricType.JobExecutions &&
                m.PeriodStart == periodStart);

        var currentCount = metric?.Count ?? 0;

        if (currentCount >= limits.MaxMonthlyExecutions)
        {
            var upgrade = GetNextPlan(plan);
            return new PlanCheckResult(
                PlanCheckOutcome.Blocked,
                $"You've reached the {limits.MaxMonthlyExecutions} monthly execution limit on {plan}. Upgrade to {upgrade} for unlimited executions.",
                currentCount,
                limits.MaxMonthlyExecutions,
                upgrade);
        }

        return new PlanCheckResult(PlanCheckOutcome.Allowed, CurrentCount: currentCount, Limit: limits.MaxMonthlyExecutions);
    }

    private static PlanCheckResult CheckAdvancedModules(PlanLimits limits, SubscriptionPlan plan)
    {
        if (limits.AdvancedModulesEnabled)
            return new PlanCheckResult(PlanCheckOutcome.Allowed);

        return new PlanCheckResult(
            PlanCheckOutcome.Blocked,
            $"Advanced modules are not available on {plan}. Upgrade to Professional to unlock them.",
            SuggestedUpgrade: SubscriptionPlan.Professional);
    }

    private static SubscriptionPlan GetNextPlan(SubscriptionPlan current) => current switch
    {
        SubscriptionPlan.Trial => SubscriptionPlan.Starter,
        SubscriptionPlan.Starter => SubscriptionPlan.Professional,
        SubscriptionPlan.Professional => SubscriptionPlan.Enterprise,
        _ => SubscriptionPlan.Enterprise
    };
}
