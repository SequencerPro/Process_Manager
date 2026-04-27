using System.Net.Http.Json;

namespace ProcessManager.Web.Services;

public class PlanEnforcementHandler : DelegatingHandler
{
    private readonly PlanEnforcementNotifier _notifier;
    private readonly ILogger<PlanEnforcementHandler> _logger;

    public PlanEnforcementHandler(PlanEnforcementNotifier notifier, ILogger<PlanEnforcementHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode == 402)
        {
            try
            {
                // Buffer the body so both the handler and any caller can read it
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                response.Content = new ByteArrayContent(bytes);
                response.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                using var doc = System.Text.Json.JsonDocument.Parse(bytes);
                var root = doc.RootElement;
                var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
                var upgrade = root.TryGetProperty("suggestedUpgrade", out var u) ? u.GetString() : null;
                var resource = root.TryGetProperty("error", out var e) ? e.GetString() : null;

                _notifier.NotifyBlocked(message ?? "Plan limit reached.", upgrade, resource);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlanEnforcementHandler] Failed to parse 402 body from {Url}",
                    request.RequestUri);
                _notifier.NotifyBlocked("Plan limit reached.", null, null);
            }
        }

        return response;
    }
}
