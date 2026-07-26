using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Services;

public interface IUsageMeteringService
{
    Task IncrementAsync(UsageMetricType metricType, int amount = 1);
}

public class UsageMeteringService : IUsageMeteringService
{
    private readonly ProcessManagerDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UsageMeteringService(ProcessManagerDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task IncrementAsync(UsageMetricType metricType, int amount = 1)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var metric = await _db.UsageMetrics
            .FirstOrDefaultAsync(m =>
                m.MetricType == metricType &&
                m.PeriodStart == periodStart);

        if (metric is not null)
        {
            metric.Count += amount;
        }
        else
        {
            _db.UsageMetrics.Add(new UsageMetric
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.CurrentTenantId,
                MetricType = metricType,
                Count = amount,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            });
        }

        await _db.SaveChangesAsync();
    }
}
