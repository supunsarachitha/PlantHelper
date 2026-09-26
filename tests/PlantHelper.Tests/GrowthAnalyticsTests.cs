using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class GrowthAnalyticsTests
{
    [Fact]
    public void HeightSeries_MergesLogsAndPhotos_LastReadingPerDayWins()
    {
        var logs = new[]
        {
            new CareLog { Type = LogType.Measure, Date = new DateTime(2026, 1, 1, 8, 0, 0), HeightCm = 10 },
            new CareLog { Type = LogType.Measure, Date = new DateTime(2026, 1, 1, 20, 0, 0), HeightCm = 11 },
            new CareLog { Type = LogType.Water, Date = new DateTime(2026, 1, 2) },
            new CareLog { Type = LogType.Measure, Date = new DateTime(2026, 1, 11), HeightCm = 21 },
        };
        var photos = new[] { new PlantPhoto { TakenAt = new DateTime(2026, 1, 6), HeightCm = 16 }, new PlantPhoto { TakenAt = new DateTime(2026, 1, 7) } };

        var series = GrowthAnalytics.HeightSeries(logs, photos);

        Assert.Equal([11d, 16d, 21d], series.Select(p => p.Value));
    }

    [Fact]
    public void Summary_ComputesTotalsAndRates()
    {
        var series = new List<DataPoint>
        {
            new(new DateTime(2026, 1, 1), 10),
            new(new DateTime(2026, 1, 11), 30),
            new(new DateTime(2026, 1, 21), 40),
        };

        var s = GrowthAnalytics.Summarize(series, recentDays: 10);

        Assert.Equal(40, s.LatestHeightCm);
        Assert.Equal(30, s.TotalGrowthCm);
        Assert.Equal(1.5, s.AverageRateCmPerDay!.Value, 3);
        Assert.Equal(1.0, s.RecentRateCmPerDay!.Value, 3);
        Assert.Equal(3, s.MeasurementCount);
    }

    [Fact]
    public void Summary_HandlesEmptyAndSingle()
    {
        Assert.Null(GrowthAnalytics.Summarize([]).LatestHeightCm);
        var one = GrowthAnalytics.Summarize([new DataPoint(DateTime.Today, 5)]);
        Assert.Equal(5, one.LatestHeightCm);
        Assert.Null(one.AverageRateCmPerDay);
    }

    [Fact]
    public void Series_SelectsFieldOldestFirst()
    {
        var logs = new[]
        {
            new CareLog { Date = new DateTime(2026, 1, 3), Ph = 6.5 },
            new CareLog { Date = new DateTime(2026, 1, 1), Ph = 5.8 },
            new CareLog { Date = new DateTime(2026, 1, 2) },
        };
        var s = GrowthAnalytics.Series(logs, l => l.Ph);
        Assert.Equal([5.8, 6.5], s.Select(p => p.Value));
    }
}
