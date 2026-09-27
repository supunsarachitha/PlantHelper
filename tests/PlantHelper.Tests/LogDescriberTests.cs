using System.Globalization;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class LogDescriberTests
{
    private readonly LogDescriber _metric;

    public LogDescriberTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _metric = new LogDescriber(new UnitFormatter(new UnitPreferences()));
    }

    [Fact]
    public void Water()
    {
        var log = new CareLog { Type = LogType.Water, AmountMl = 500, Ph = 6.24, Ec = 1.2, RunoffPh = 6.0 };
        Assert.Equal("Watered", _metric.Title(log));
        Assert.Equal("500 ml · pH 6.2 · 1.2 EC · runoff pH 6.0", _metric.Details(log));
    }

    [Fact]
    public void Feed_ListsDoses()
    {
        var log = new CareLog { Type = LogType.Feed, AmountMl = 1500, Doses = [new NutrientDose { Name = "Grow", MlPerLiter = 2 }, new NutrientDose { Name = "CalMag", MlPerLiter = 0.5 }] };
        Assert.Equal("1.5 L · Grow 2 ml/L, CalMag 0.5 ml/L", _metric.Details(log));
    }

    [Fact]
    public void Measure_AndEnvironment()
    {
        Assert.Equal("Height 45 cm · 8 nodes", _metric.Details(new CareLog { Type = LogType.Measure, HeightCm = 45, NodeCount = 8 }));
        var env = _metric.Details(new CareLog { Type = LogType.Environment, TempC = 25, HumidityPct = 60, LightHours = 18 });
        Assert.StartsWith("25°C · 60% RH · VPD ", env);
        Assert.EndsWith("18.0 h light", env);
    }

    [Fact]
    public void Pest_UsesIssueAsTitle()
    {
        var log = new CareLog { Type = LogType.Pest, Title = "Aphids", Severity = Severity.High, Treatment = "Neem oil" };
        Assert.Equal("Aphids", _metric.Title(log));
        Assert.Equal("High severity · Treated: Neem oil", _metric.Details(log));
    }

    [Fact]
    public void Harvest_Stage_Note_Transplant()
    {
        Assert.Equal("350 g · 12 pcs · ★★★★☆", _metric.Details(new CareLog { Type = LogType.Harvest, WeightG = 350, Quantity = 12, Rating = 4 }));
        var stage = new CareLog { Type = LogType.Stage, FromStage = GrowthStage.Vegetative, ToStage = GrowthStage.Flowering };
        Assert.Equal("Moved to Flowering", _metric.Title(stage));
        Assert.Equal("Vegetative → Flowering", _metric.Details(stage));
        Assert.Equal("Looks healthy", _metric.Details(new CareLog { Type = LogType.Note, Notes = "Looks healthy" }));
        Assert.Equal("into 11 L · Coco coir", _metric.Details(new CareLog { Type = LogType.Transplant, ContainerLiters = 11, Medium = GrowMedium.Coco }));
    }

    [Fact]
    public void EmptyEntry_FallsBackToNotes()
    {
        Assert.Equal("just checked", _metric.Details(new CareLog { Type = LogType.Water, Notes = "just checked" }));
        Assert.Equal(string.Empty, _metric.Details(new CareLog { Type = LogType.Water }));
    }
}
