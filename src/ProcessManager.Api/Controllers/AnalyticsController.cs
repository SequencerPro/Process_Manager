using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    public AnalyticsController(ProcessManagerDbContext db) => _db = db;

    /// <summary>
    /// Execute an ad-hoc analytics query.  Fetches PromptResponse records for all
    /// requested series, bins them into temporal buckets, and returns the aligned
    /// bucket rows ready for charting.
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<AnalyticsQueryResultDto>> Query(AnalyticsQueryDto dto)
    {
        if (dto.Series.Count == 0)
            return BadRequest("At least one series is required.");
        if (dto.StartDate >= dto.EndDate)
            return BadRequest("StartDate must be before EndDate.");

        var startUtc = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        var endUtc   = DateTime.SpecifyKind(dto.EndDate,   DateTimeKind.Utc);
        var bucketTicks = TimeSpan.FromMinutes(dto.BucketSizeMinutes).Ticks;

        var contentIds = dto.Series.Select(s => s.ContentId).Distinct().ToList();

        // ── Fetch source content metadata (for units) ──
        var contents = await _db.StepTemplateContents
            .Where(c => contentIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Units })
            .ToListAsync();

        // ── Fetch all numeric PromptResponses for the requested content + window ──
        var responses = await _db.PromptResponses
            .Where(r => r.StepTemplateContentId != null
                     && contentIds.Contains(r.StepTemplateContentId.Value)
                     && r.CreatedAt >= startUtc
                     && r.CreatedAt <= endUtc)
            .Select(r => new
            {
                ContentId  = r.StepTemplateContentId!.Value,
                r.CreatedAt,
                r.ResponseValue
            })
            .ToListAsync();

        int totalResponses = responses.Count;

        // ── Bucket accumulator: contentId → bucketKey → list of parsed values ──
        var acc = contentIds.ToDictionary(
            id => id,
            _ => new Dictionary<DateTime, List<double>>());

        foreach (var r in responses)
        {
            if (!double.TryParse(r.ResponseValue, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var val)) continue;

            // Truncate timestamp to bucket boundary
            var bucketKey = new DateTime(
                (r.CreatedAt.Ticks / bucketTicks) * bucketTicks,
                DateTimeKind.Utc);

            var dict = acc[r.ContentId];
            if (!dict.TryGetValue(bucketKey, out var list))
                dict[bucketKey] = list = new List<double>();

            list.Add(val);
        }

        // ── Collect every unique bucket across all series ──
        var allBuckets = acc.Values
            .SelectMany(d => d.Keys)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        // ── Build series metadata list ──
        var seriesMeta = dto.Series.Select(s =>
        {
            var meta = contents.FirstOrDefault(c => c.Id == s.ContentId);
            return new AnalyticsSeriesResultDto(
                s.ContentId, s.Label, s.Color, s.YAxis, meta?.Units);
        }).ToList();

        // ── Build aligned bucket rows (mean of values in each bucket) ──
        var rows = allBuckets.Select(bucket =>
        {
            var values = new Dictionary<string, double?>();
            foreach (var s in dto.Series)
            {
                values[s.ContentId.ToString()] =
                    acc[s.ContentId].TryGetValue(bucket, out var pts)
                        ? pts.Average()
                        : null;
            }
            return new AnalyticsBucketRowDto(bucket, values);
        }).ToList();

        return new AnalyticsQueryResultDto(
            seriesMeta, rows, dto.ChartType, dto.BucketSizeMinutes, totalResponses);
    }
}
