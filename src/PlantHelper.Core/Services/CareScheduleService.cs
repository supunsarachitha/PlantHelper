using PlantHelper.Core.Data;
using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

/// <summary>Works out which active plants need watering or feeding.</summary>
public sealed class CareScheduleService(PlantRepository plants, CareLogRepository logs, TimeProvider time)
{
    /// <summary>Tasks due on or before today + <paramref name="lookaheadDays"/>, most overdue first.</summary>
    public async Task<List<CareTask>> GetTasksAsync(int lookaheadDays = 0)
    {
        var today = time.GetLocalNow().Date;
        var active = await plants.GetByStatusAsync(PlantStatus.Active);
        var lastDates = await logs.GetLastDatesAsync();

        var tasks = new List<CareTask>();
        foreach (var plant in active)
        {
            var lastWater = Max(Get(lastDates, plant.Id, LogType.Water), Get(lastDates, plant.Id, LogType.Feed));
            var lastFeed = Get(lastDates, plant.Id, LogType.Feed);

            if (ComputeTask(plant, CareTaskKind.Water, lastWater, today) is { } water)
                tasks.Add(water);
            if (ComputeTask(plant, CareTaskKind.Feed, lastFeed, today) is { } feed)
                tasks.Add(feed);
        }

        var horizon = today.AddDays(lookaheadDays);
        return tasks
            .Where(t => t.DueDate <= horizon)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Kind)
            .ThenBy(t => t.Plant.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Next due date = last time done + interval. A plant never cared for counts from the day it was added.
    /// Returns null when the plant has no interval for this kind of task.
    /// </summary>
    public static CareTask? ComputeTask(Plant plant, CareTaskKind kind, DateTime? lastDone, DateTime today)
    {
        var interval = kind == CareTaskKind.Water ? plant.WaterEveryDays : plant.FeedEveryDays;
        if (interval <= 0 || plant.Status != PlantStatus.Active)
            return null;

        var baseline = (lastDone ?? DateTime.SpecifyKind(plant.CreatedAt, DateTimeKind.Utc).ToLocalTime()).Date;
        var due = baseline.AddDays(interval);
        var overdue = (int)(today.Date - due).TotalDays;
        return new CareTask(plant, kind, due, lastDone, overdue);
    }

    private static DateTime? Get(Dictionary<(string, LogType), DateTime> map, string plantId, LogType type) =>
        map.TryGetValue((plantId, type), out var d) ? d : null;

    private static DateTime? Max(DateTime? a, DateTime? b) =>
        a is null ? b : b is null ? a : (a > b ? a : b);
}
