using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class CareScheduleTests
{
    private static readonly DateTime Today = new(2026, 9, 26);

    [Fact]
    public void NoInterval_NoTask()
    {
        var plant = new Plant { Name = "Cactus", WaterEveryDays = 0 };
        Assert.Null(CareScheduleService.ComputeTask(plant, CareTaskKind.Water, null, Today));
    }

    [Fact]
    public void InactivePlant_NoTask()
    {
        var plant = new Plant { Name = "Done", WaterEveryDays = 2, Status = PlantStatus.Harvested };
        Assert.Null(CareScheduleService.ComputeTask(plant, CareTaskKind.Water, Today.AddDays(-10), Today));
    }

    [Theory]
    [InlineData(-3, 3, 0)]   // watered 3 days ago, every 3 days -> due today
    [InlineData(-5, 3, 2)]   // 2 days overdue
    [InlineData(-1, 3, -2)]  // due in 2 days
    public void DueDate_FromLastDone(int lastDoneOffset, int interval, int expectedOverdue)
    {
        var plant = new Plant { Name = "Basil", WaterEveryDays = interval };
        var task = CareScheduleService.ComputeTask(plant, CareTaskKind.Water, Today.AddDays(lastDoneOffset).AddHours(9), Today)!;
        Assert.Equal(expectedOverdue, task.DaysOverdue);
        Assert.Equal(Today.AddDays(lastDoneOffset + interval), task.DueDate);
        Assert.Equal(expectedOverdue > 0, task.IsOverdue);
        Assert.Equal(expectedOverdue == 0, task.IsDueToday);
    }

    [Fact]
    public void NeverDone_CountsFromCreation()
    {
        var created = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Local).ToUniversalTime();
        var plant = new Plant { Name = "New", FeedEveryDays = 7, CreatedAt = created };
        var task = CareScheduleService.ComputeTask(plant, CareTaskKind.Feed, null, Today)!;
        Assert.Equal(new DateTime(2026, 10, 1), task.DueDate);
        Assert.Equal(-5, task.DaysOverdue);
    }

    [Fact]
    public async Task GetTasks_FeedingResetsWatering_AndLookaheadFilters()
    {
        await using var t = new TestDatabase();
        var time = new FixedTimeProvider(new DateTimeOffset(Today.AddHours(10)));
        var service = new CareScheduleService(t.Plants, t.Logs, time);

        var basil = new Plant { Name = "Basil", WaterEveryDays = 2, FeedEveryDays = 14 };
        var tomato = new Plant { Name = "Tomato", WaterEveryDays = 1 };
        var archived = new Plant { Name = "Old", WaterEveryDays = 1, Status = PlantStatus.Archived };
        foreach (var p in new[] { basil, tomato, archived })
            await t.Plants.SaveAsync(p);

        // Basil: watered 5 days ago but fed yesterday -> watering due tomorrow; feeding due in 13 days
        await t.Logs.SaveAsync(new CareLog { PlantId = basil.Id, Type = LogType.Water, Date = Today.AddDays(-5) });
        await t.Logs.SaveAsync(new CareLog { PlantId = basil.Id, Type = LogType.Feed, Date = Today.AddDays(-1) });
        // Tomato: watered 3 days ago -> 2 days overdue
        await t.Logs.SaveAsync(new CareLog { PlantId = tomato.Id, Type = LogType.Water, Date = Today.AddDays(-3) });

        var dueNow = await service.GetTasksAsync();
        var single = Assert.Single(dueNow);
        Assert.Equal("Tomato", single.Plant.Name);
        Assert.Equal(2, single.DaysOverdue);

        var week = await service.GetTasksAsync(lookaheadDays: 7);
        Assert.Equal(2, week.Count);
        Assert.Contains(week, x => x.Plant.Name == "Basil" && x.Kind == CareTaskKind.Water && x.DaysOverdue == -1);
        Assert.DoesNotContain(week, x => x.Plant.Name == "Old");
        Assert.Equal("Tomato", week[0].Plant.Name); // most overdue first
    }
}
