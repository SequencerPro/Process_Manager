namespace ProcessManager.Web.Services;

public class PlanEnforcementNotifier
{
    public event Action<string, string?, string?>? OnPlanLimitHit;

    public void NotifyBlocked(string message, string? suggestedUpgrade, string? resource)
        => OnPlanLimitHit?.Invoke(message, suggestedUpgrade, resource);
}
