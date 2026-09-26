using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

public readonly record struct DataPoint(DateTime Date, double Value);

public sealed record GrowthSummary(
    double? LatestHeightCm,
    double? TotalGrowthCm,
    double? AverageRateCmPerDay,
    double? RecentRateCmPerDay,
    int MeasurementCount);

public static class GrowthAnalytics
{
    /// <summary>
    /// Height over time from measurement logs and photos that recorded a height.
    /// When several readings share a day the last one wins.
    /// </summary>
    public static List<DataPoint> HeightSeries(IEnumerable<CareLog> logs, IEnumerable<PlantPhoto> photos)
    {
        var points = logs
            .Where(l => l.HeightCm is > 0)
            .Select(l => new DataPoint(l.Date, l.HeightCm!.Value))
            .Concat(photos.Where(p => p.HeightCm is > 0).Select(p => new DataPoint(p.TakenAt, p.HeightCm!.Value)));

        return points
            .OrderBy(p => p.Date)
            .GroupBy(p => p.Date.Date)
            .Select(g => new DataPoint(g.Key, g.Last().Value))
            .ToList();
    }

    /// <summary>A time series of any numeric log field, oldest first.</summary>
    public static List<DataPoint> Series(IEnumerable<CareLog> logs, Func<CareLog, double?> selector) =>
        logs
            .Select(l => (l.Date, Value: selector(l)))
            .Where(x => x.Value.HasValue)
            .OrderBy(x => x.Date)
            .Select(x => new DataPoint(x.Date, x.Value!.Value))
            .ToList();

    public static GrowthSummary Summarize(IReadOnlyList<DataPoint> heights, int recentDays = 7)
    {
        if (heights.Count == 0)
            return new GrowthSummary(null, null, null, null, 0);

        var first = heights[0];
        var last = heights[^1];
        if (heights.Count == 1)
            return new GrowthSummary(last.Value, null, null, null, 1);

        return new GrowthSummary(
            last.Value,
            last.Value - first.Value,
            RatePerDay(first, last),
            RecentRate(heights, recentDays),
            heights.Count);
    }

    /// <summary>Growth rate between the latest reading and the reading closest to <paramref name="days"/> earlier.</summary>
    public static double? RecentRate(IReadOnlyList<DataPoint> heights, int days)
    {
        if (heights.Count < 2)
            return null;
        var last = heights[^1];
        var target = last.Date.AddDays(-days);
        var reference = heights
            .Take(heights.Count - 1)
            .OrderBy(p => Math.Abs((p.Date - target).TotalDays))
            .First();
        return RatePerDay(reference, last);
    }

    private static double? RatePerDay(DataPoint from, DataPoint to)
    {
        var days = (to.Date.Date - from.Date.Date).TotalDays;
        return days <= 0 ? null : (to.Value - from.Value) / days;
    }
}
