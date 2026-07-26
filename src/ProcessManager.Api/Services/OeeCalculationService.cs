using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

public interface IOeeCalculationService
{
    Task<OeeSnapshotDto?> CalculateForEquipmentAsync(Guid equipmentId, DateTime shiftDate, ShiftDefinition shift);
    Task<OeeDashboardDto> GetDashboardAsync(DateTime? fromDate, DateTime? toDate, Guid? equipmentId, decimal targetOee = 85m);
    Task<OeeTrendDto?> GetTrendAsync(Guid equipmentId, DateTime fromDate, DateTime toDate);
    Task<List<OeeLossCategoryDto>> GetLossesAsync(DateTime? fromDate, DateTime? toDate, Guid? equipmentId);
}

public class OeeCalculationService : IOeeCalculationService
{
    private readonly ProcessManagerDbContext _db;

    public OeeCalculationService(ProcessManagerDbContext db) => _db = db;

    public async Task<OeeSnapshotDto?> CalculateForEquipmentAsync(Guid equipmentId, DateTime shiftDate, ShiftDefinition shift)
    {
        var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment is null) return null;

        var shiftStart = shiftDate.Date + shift.StartTime.ToTimeSpan();
        var shiftEnd = shift.EndTime > shift.StartTime
            ? shiftDate.Date + shift.EndTime.ToTimeSpan()
            : shiftDate.Date.AddDays(1) + shift.EndTime.ToTimeSpan();

        var plannedMinutes = (decimal)(shiftEnd - shiftStart).TotalMinutes;
        if (plannedMinutes <= 0) return null;

        // Availability: planned time minus downtime
        var downtimeRecords = await _db.DowntimeRecords
            .Where(d => d.EquipmentId == equipmentId)
            .Where(d => d.StartedAt < shiftEnd && (d.EndedAt == null || d.EndedAt > shiftStart))
            .ToListAsync();

        decimal downtimeMinutes = 0;
        foreach (var dt in downtimeRecords)
        {
            var overlapStart = dt.StartedAt < shiftStart ? shiftStart : dt.StartedAt;
            var overlapEnd = dt.EndedAt.HasValue
                ? (dt.EndedAt.Value > shiftEnd ? shiftEnd : dt.EndedAt.Value)
                : shiftEnd;
            downtimeMinutes += (decimal)(overlapEnd - overlapStart).TotalMinutes;
        }

        var runTimeMinutes = plannedMinutes - downtimeMinutes;
        var availability = runTimeMinutes > 0 ? runTimeMinutes / plannedMinutes * 100m : 0m;

        // Performance: actual production rate vs ideal
        var stepExecutions = await _db.StepExecutions
            .Include(se => se.ProcessStep)
                .ThenInclude(ps => ps.StepTemplate)
            .Where(se => se.EquipmentId == equipmentId)
            .Where(se => se.CompletedAt != null)
            .Where(se => se.StartedAt >= shiftStart && se.CompletedAt <= shiftEnd)
            .ToListAsync();

        var outputTransactions = await _db.PortTransactions
            .Include(pt => pt.Port)
            .Where(pt => stepExecutions.Select(se => se.Id).Contains(pt.StepExecutionId))
            .Where(pt => pt.Port.Direction == PortDirection.Output)
            .ToListAsync();

        var totalPieces = outputTransactions.Sum(pt => pt.Quantity);

        // Ideal cycle time: use ExpectedDurationMinutes from step template if available
        decimal performance = 0m;
        if (runTimeMinutes > 0 && totalPieces > 0)
        {
            var idealCycleTimeMinutes = stepExecutions
                .Where(se => se.ProcessStep.StepTemplate.ExpectedDurationMinutes.HasValue)
                .Select(se => (decimal)se.ProcessStep.StepTemplate.ExpectedDurationMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average();

            if (idealCycleTimeMinutes > 0)
                performance = Math.Min((idealCycleTimeMinutes * totalPieces) / runTimeMinutes * 100m, 100m);
            else
                performance = totalPieces > 0 ? 100m : 0m;
        }

        // Quality: good pieces vs total (subtract non-conformances from that shift)
        var ncCount = await _db.NonConformances
            .Where(nc => stepExecutions.Select(se => se.Id).Contains(nc.StepExecutionId))
            .Where(nc => nc.DispositionStatus == DispositionStatus.Scrap
                      || nc.DispositionStatus == DispositionStatus.Rework)
            .CountAsync();

        var goodPieces = Math.Max(totalPieces - ncCount, 0);
        var quality = totalPieces > 0 ? (decimal)goodPieces / totalPieces * 100m : 100m;

        var oee = availability * performance * quality / 10000m;

        return new OeeSnapshotDto(
            equipmentId,
            equipment.Code,
            equipment.Name,
            shiftDate,
            shift.Code,
            shift.Name,
            Math.Round(availability, 1),
            Math.Round(performance, 1),
            Math.Round(quality, 1),
            Math.Round(oee, 1),
            Math.Round(plannedMinutes, 0),
            Math.Round(downtimeMinutes, 0),
            Math.Round(runTimeMinutes, 0),
            totalPieces,
            goodPieces,
            ncCount);
    }

    public async Task<OeeDashboardDto> GetDashboardAsync(DateTime? fromDate, DateTime? toDate, Guid? equipmentId, decimal targetOee = 85m)
    {
        var from = fromDate ?? DateTime.UtcNow.Date;
        var to = toDate ?? DateTime.UtcNow.Date;

        var shifts = await _db.Set<ShiftDefinition>()
            .Where(s => s.IsActive)
            .ToListAsync();

        if (!shifts.Any())
        {
            return new OeeDashboardDto(0, 0, 0, 0, 0, 0, targetOee,
                new List<OeeSnapshotDto>(), new List<OeeLossCategoryDto>());
        }

        var equipmentQuery = _db.Equipment.Where(e => e.IsActive);
        if (equipmentId.HasValue)
            equipmentQuery = equipmentQuery.Where(e => e.Id == equipmentId.Value);

        var equipmentList = await equipmentQuery.ToListAsync();
        var snapshots = new List<OeeSnapshotDto>();

        foreach (var eq in equipmentList)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                foreach (var shift in shifts)
                {
                    var snapshot = await CalculateForEquipmentAsync(eq.Id, date, shift);
                    if (snapshot is not null && snapshot.TotalPieces > 0)
                        snapshots.Add(snapshot);
                }
            }
        }

        var avgOee = snapshots.Any() ? snapshots.Average(s => s.OeePct) : 0m;
        var avgAvail = snapshots.Any() ? snapshots.Average(s => s.AvailabilityPct) : 0m;
        var avgPerf = snapshots.Any() ? snapshots.Average(s => s.PerformancePct) : 0m;
        var avgQual = snapshots.Any() ? snapshots.Average(s => s.QualityPct) : 0m;

        var latestPerEquipment = snapshots
            .GroupBy(s => s.EquipmentId)
            .Select(g => g.OrderByDescending(s => s.ShiftDate).First())
            .ToList();

        var belowTarget = latestPerEquipment.Count(s => s.OeePct < targetOee);

        var losses = await GetLossesAsync(fromDate, toDate, equipmentId);

        return new OeeDashboardDto(
            equipmentList.Count,
            Math.Round(avgOee, 1),
            Math.Round(avgAvail, 1),
            Math.Round(avgPerf, 1),
            Math.Round(avgQual, 1),
            belowTarget,
            targetOee,
            latestPerEquipment.OrderByDescending(s => s.OeePct).ToList(),
            losses.Take(10).ToList());
    }

    public async Task<OeeTrendDto?> GetTrendAsync(Guid equipmentId, DateTime fromDate, DateTime toDate)
    {
        var equipment = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment is null) return null;

        var shifts = await _db.Set<ShiftDefinition>()
            .Where(s => s.IsActive)
            .ToListAsync();

        var points = new List<OeeTrendPointDto>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            foreach (var shift in shifts)
            {
                var snapshot = await CalculateForEquipmentAsync(equipmentId, date, shift);
                if (snapshot is not null && snapshot.TotalPieces > 0)
                {
                    points.Add(new OeeTrendPointDto(
                        date, shift.Code,
                        snapshot.AvailabilityPct,
                        snapshot.PerformancePct,
                        snapshot.QualityPct,
                        snapshot.OeePct));
                }
            }
        }

        return new OeeTrendDto(equipmentId, equipment.Code, equipment.Name, points);
    }

    public async Task<List<OeeLossCategoryDto>> GetLossesAsync(DateTime? fromDate, DateTime? toDate, Guid? equipmentId)
    {
        var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-7);
        var to = toDate ?? DateTime.UtcNow.Date.AddDays(1);

        var downtimeQuery = _db.DowntimeRecords
            .Where(d => d.StartedAt >= from && d.StartedAt < to);

        if (equipmentId.HasValue)
            downtimeQuery = downtimeQuery.Where(d => d.EquipmentId == equipmentId.Value);

        var downtimeRecords = await downtimeQuery.ToListAsync();

        var totalDowntimeMinutes = downtimeRecords.Sum(d =>
        {
            var end = d.EndedAt ?? DateTime.UtcNow;
            return (decimal)(end - d.StartedAt).TotalMinutes;
        });

        var losses = downtimeRecords
            .GroupBy(d => new { d.Type, d.Reason })
            .Select(g =>
            {
                var minutes = g.Sum(d =>
                {
                    var end = d.EndedAt ?? DateTime.UtcNow;
                    return (decimal)(end - d.StartedAt).TotalMinutes;
                });
                return new OeeLossCategoryDto(
                    string.IsNullOrWhiteSpace(g.Key.Reason) ? "Unspecified" : g.Key.Reason,
                    g.Key.Type.ToString(),
                    Math.Round(minutes, 0),
                    totalDowntimeMinutes > 0 ? Math.Round(minutes / totalDowntimeMinutes * 100m, 1) : 0m,
                    g.Count());
            })
            .OrderByDescending(l => l.MinutesLost)
            .ToList();

        return losses;
    }
}
