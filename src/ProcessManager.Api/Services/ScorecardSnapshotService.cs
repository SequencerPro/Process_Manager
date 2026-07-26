using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

/// <summary>
/// Phase 50b: periodically resolves every auto-sourced ObjectiveMeasure and appends a
/// MeasureSnapshot, so scorecard trends accumulate without manual entry. Capture interval
/// is configurable via ScorecardSnapshots:IntervalHours (default 24).
/// </summary>
public class ScorecardSnapshotService : BackgroundService
{
    /// <summary>
    /// Minimum gap before another auto snapshot of the same measure is taken. Slightly
    /// under the daily interval so a restart doesn't skip a day, but re-runs within the
    /// same day don't duplicate.
    /// </summary>
    private static readonly TimeSpan DuplicateGuard = TimeSpan.FromHours(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScorecardSnapshotService> _logger;
    private readonly TimeSpan _interval;

    public ScorecardSnapshotService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScorecardSnapshotService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var hours = configuration.GetValue<int>("ScorecardSnapshots:IntervalHours", 24);
        _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScorecardSnapshotService started. Capturing every {Interval}h.", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureSnapshotsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScorecardSnapshotService encountered an error.");
            }

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("ScorecardSnapshotService stopped.");
    }

    /// <summary>
    /// Captures one auto snapshot per auto-sourced measure, per tenant. Exposed as internal
    /// for direct invocation in integration tests.
    /// </summary>
    internal async Task CaptureSnapshotsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var resolver = scope.ServiceProvider.GetRequiredService<IMeasureValueResolver>();

        // Discover which tenants have auto-sourced measures (platform scope bypasses the filter).
        List<Guid> tenantIds;
        using (tenantContext.BeginScope(Tenant.DefaultTenantId, isPlatformAdmin: true))
        {
            tenantIds = await db.ObjectiveMeasures
                .Where(m => m.SourceType != MeasureSourceType.Manual)
                .Select(m => m.TenantId)
                .Distinct()
                .ToListAsync(ct);
        }

        var now = DateTime.UtcNow;

        // Resolve within each tenant's scope so the measure sources read tenant-correct data
        // and the SaveChanges interceptor stamps the right TenantId on new snapshots.
        foreach (var tenantId in tenantIds)
        {
            using var _ = tenantContext.BeginScope(tenantId);

            var measures = await db.ObjectiveMeasures
                .Where(m => m.SourceType != MeasureSourceType.Manual)
                .ToListAsync(ct);

            var captured = 0;
            foreach (var measure in measures)
            {
                var recentAuto = await db.MeasureSnapshots.AnyAsync(s =>
                    s.MeasureId == measure.Id &&
                    s.CaptureSource == MeasureCaptureSource.Auto &&
                    s.CapturedAt > now - DuplicateGuard, ct);
                if (recentAuto) continue;

                decimal? value;
                try
                {
                    value = await resolver.ResolveAsync(measure);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve measure {MeasureId} ({MeasureName}).", measure.Id, measure.Name);
                    continue;
                }

                if (value is not decimal v) continue;

                db.MeasureSnapshots.Add(new MeasureSnapshot
                {
                    MeasureId = measure.Id,
                    Value = v,
                    CapturedAt = now,
                    CaptureSource = MeasureCaptureSource.Auto,
                });
                captured++;
            }

            if (captured > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Captured {Count} scorecard snapshots for tenant {TenantId}.", captured, tenantId);
            }
        }
    }
}
