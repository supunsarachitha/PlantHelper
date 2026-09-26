using PlantHelper.Core.Models;

namespace PlantHelper.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task Plant_RoundTrips_AllFields()
    {
        await using var t = new TestDatabase();
        var plant = new Plant
        {
            Name = "  Cherry Tomato ",
            Species = "Solanum lycopersicum",
            Variety = "Sweet 100",
            Category = PlantCategory.Vegetable,
            Stage = GrowthStage.Flowering,
            Medium = GrowMedium.Coco,
            Location = GrowLocation.Greenhouse,
            Source = PlantSource.Seed,
            ContainerLiters = 11.5,
            Spot = "Bed 2",
            PlantedOn = new DateTime(2026, 5, 1),
            GerminatedOn = new DateTime(2026, 5, 6),
            ExpectedHarvestOn = new DateTime(2026, 8, 1),
            WaterEveryDays = 2,
            FeedEveryDays = 7,
            IsFavorite = true,
            Notes = "South facing",
        };

        await t.Plants.SaveAsync(plant);
        var loaded = await t.Plants.GetAsync(plant.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Cherry Tomato", loaded.Name); // trimmed
        Assert.Equal(GrowthStage.Flowering, loaded.Stage);
        Assert.Equal(GrowMedium.Coco, loaded.Medium);
        Assert.Equal(GrowLocation.Greenhouse, loaded.Location);
        Assert.Equal(11.5, loaded.ContainerLiters);
        Assert.Equal(new DateTime(2026, 5, 6), loaded.GerminatedOn);
        Assert.Equal(7, loaded.FeedEveryDays);
        Assert.True(loaded.IsFavorite);
    }

    [Fact]
    public async Task Plant_WithoutName_IsRejected()
    {
        await using var t = new TestDatabase();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => t.Plants.SaveAsync(new Plant { Name = "  " }));
    }

    [Fact]
    public async Task Plants_FilterByStatus_AndSortByName()
    {
        await using var t = new TestDatabase();
        await t.Plants.SaveAsync(new Plant { Name = "Zucchini" });
        await t.Plants.SaveAsync(new Plant { Name = "Basil" });
        await t.Plants.SaveAsync(new Plant { Name = "Old pepper", Status = PlantStatus.Harvested });

        var active = await t.Plants.GetByStatusAsync(PlantStatus.Active);
        Assert.Equal(["Basil", "Zucchini"], active.Select(p => p.Name));
        Assert.Equal(1, await t.Plants.CountAsync(PlantStatus.Harvested));
        Assert.Equal(3, (await t.Plants.GetAllAsync()).Count);
    }

    [Fact]
    public async Task DeletePlant_CascadesLogsAndPhotos_AndReturnsFiles()
    {
        await using var t = new TestDatabase();
        var plant = new Plant { Name = "Mint" };
        var other = new Plant { Name = "Sage" };
        await t.Plants.SaveAsync(plant);
        await t.Plants.SaveAsync(other);
        await t.Logs.SaveAsync(new CareLog { PlantId = plant.Id, Type = LogType.Water, AmountMl = 250 });
        await t.Logs.SaveAsync(new CareLog { PlantId = other.Id, Type = LogType.Water });
        await t.PhotoRows.SaveAsync(new PlantPhoto { PlantId = plant.Id, FileName = "a.jpg" });

        var files = await t.Plants.DeleteAsync(plant.Id);

        Assert.Equal(["a.jpg"], files);
        Assert.Null(await t.Plants.GetAsync(plant.Id));
        Assert.Empty(await t.Logs.GetForPlantAsync(plant.Id));
        Assert.Empty(await t.PhotoRows.GetForPlantAsync(plant.Id));
        Assert.Single(await t.Logs.GetForPlantAsync(other.Id));
    }

    [Fact]
    public async Task CareLog_RoundTrips_TypeSpecificFields_AndDoses()
    {
        await using var t = new TestDatabase();
        var log = new CareLog
        {
            PlantId = "p1",
            Type = LogType.Feed,
            Date = new DateTime(2026, 6, 1, 8, 30, 0),
            AmountMl = 1500,
            Ph = 6.2,
            Ec = 1.4,
            RunoffPh = 6.0,
            Severity = Severity.High,
            Medium = GrowMedium.Rockwool,
            ToStage = GrowthStage.Flowering,
            Doses = [new NutrientDose { Name = "Grow A", MlPerLiter = 2 }, new NutrientDose { Name = "Cal-Mag", MlPerLiter = 0.5 }],
        };

        await t.Logs.SaveAsync(log);
        var loaded = (await t.Logs.GetAsync(log.Id))!;

        Assert.Equal(LogType.Feed, loaded.Type);
        Assert.Equal(new DateTime(2026, 6, 1, 8, 30, 0), loaded.Date);
        Assert.Equal(1500, loaded.AmountMl);
        Assert.Equal(6.2, loaded.Ph);
        Assert.Equal(Severity.High, loaded.Severity);
        Assert.Equal(GrowMedium.Rockwool, loaded.Medium);
        Assert.Equal(GrowthStage.Flowering, loaded.ToStage);
        Assert.Null(loaded.FromStage);
        Assert.Equal(2, loaded.Doses.Count);
        Assert.Equal("Cal-Mag", loaded.Doses[1].Name);
        Assert.Equal(0.5, loaded.Doses[1].MlPerLiter);
    }

    [Fact]
    public async Task CareLog_CorruptDosesJson_ReturnsEmpty()
    {
        var log = new CareLog { DosesJson = "{not json" };
        Assert.Empty(log.Doses);
        log.Doses = [];
        Assert.Null(log.DosesJson);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Logs_AreNewestFirst_AndLastDatesGroupByPlantAndType()
    {
        await using var t = new TestDatabase();
        await t.Logs.SaveAsync(new CareLog { PlantId = "p1", Type = LogType.Water, Date = new DateTime(2026, 1, 1) });
        await t.Logs.SaveAsync(new CareLog { PlantId = "p1", Type = LogType.Water, Date = new DateTime(2026, 1, 5) });
        await t.Logs.SaveAsync(new CareLog { PlantId = "p1", Type = LogType.Feed, Date = new DateTime(2026, 1, 3) });
        await t.Logs.SaveAsync(new CareLog { PlantId = "p2", Type = LogType.Water, Date = new DateTime(2026, 1, 2) });

        var p1 = await t.Logs.GetForPlantAsync("p1");
        Assert.Equal(new DateTime(2026, 1, 5), p1[0].Date);

        var last = await t.Logs.GetLastDatesAsync();
        Assert.Equal(new DateTime(2026, 1, 5), last[("p1", LogType.Water)]);
        Assert.Equal(new DateTime(2026, 1, 3), last[("p1", LogType.Feed)]);
        Assert.Equal(new DateTime(2026, 1, 2), last[("p2", LogType.Water)]);

        Assert.Equal(new DateTime(2026, 1, 5), (await t.Logs.GetLastAsync("p1", LogType.Water))!.Date);
        Assert.Equal(2, await t.Logs.CountSinceAsync(new DateTime(2026, 1, 3)));
        Assert.Equal(2, (await t.Logs.GetRecentAsync(2)).Count);
    }

    [Fact]
    public async Task DeletePhoto_ClearsCover()
    {
        await using var t = new TestDatabase();
        var plant = new Plant { Name = "Fern" };
        var photo = new PlantPhoto { PlantId = plant.Id, FileName = "f.jpg" };
        plant.CoverPhotoId = photo.Id;
        await t.Plants.SaveAsync(plant);
        await t.PhotoRows.SaveAsync(photo);

        var file = await t.PhotoRows.DeleteAsync(photo.Id);

        Assert.Equal("f.jpg", file);
        Assert.Null((await t.Plants.GetAsync(plant.Id))!.CoverPhotoId);
        Assert.Equal(0, await t.PhotoRows.CountAsync());
    }

    [Fact]
    public async Task Photos_TimelineIsOldestFirst_GalleryNewestFirst()
    {
        await using var t = new TestDatabase();
        await t.PhotoRows.SaveAsync(new PlantPhoto { PlantId = "p", FileName = "b.jpg", TakenAt = new DateTime(2026, 2, 1) });
        await t.PhotoRows.SaveAsync(new PlantPhoto { PlantId = "p", FileName = "a.jpg", TakenAt = new DateTime(2026, 1, 1) });

        Assert.Equal("a.jpg", (await t.PhotoRows.GetForPlantAsync("p"))[0].FileName);
        Assert.Equal("b.jpg", (await t.PhotoRows.GetAllAsync())[0].FileName);
    }

    [Fact]
    public async Task Nutrients_CrudAndNpkText()
    {
        await using var t = new TestDatabase();
        var n = new Nutrient { Name = "Bloom", Brand = "Acme", Type = NutrientType.Bloom, N = 1, P = 5, K = 4.5, DefaultMlPerLiter = 2 };
        await t.Nutrients.SaveAsync(n);
        var loaded = (await t.Nutrients.GetAsync(n.Id))!;
        Assert.Equal("1-5-4.5", loaded.NpkText);
        Assert.Equal(NutrientType.Bloom, loaded.Type);

        await t.Nutrients.DeleteAsync(n.Id);
        Assert.Empty(await t.Nutrients.GetAllAsync());
        Assert.Equal(string.Empty, new Nutrient().NpkText);
    }

    [Fact]
    public async Task DeleteAll_EmptiesEveryTable()
    {
        await using var t = new TestDatabase();
        await t.Plants.SaveAsync(new Plant { Name = "X" });
        await t.Logs.SaveAsync(new CareLog { PlantId = "x" });
        await t.PhotoRows.SaveAsync(new PlantPhoto { PlantId = "x", FileName = "x.jpg" });
        await t.Nutrients.SaveAsync(new Nutrient { Name = "N" });

        await t.Database.DeleteAllAsync();

        Assert.Empty(await t.Plants.GetAllAsync());
        Assert.Empty(await t.Logs.GetAllAsync());
        Assert.Empty(await t.PhotoRows.GetAllAsync());
        Assert.Empty(await t.Nutrients.GetAllAsync());
    }
}
