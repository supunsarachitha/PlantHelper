using System.Globalization;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class UnitFormatterTests
{
    public UnitFormatterTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    [Fact]
    public void Metric_Formatting()
    {
        var f = new UnitFormatter(new UnitPreferences());
        Assert.Equal("12.5 cm", f.FormatLength(12.5));
        Assert.Equal("500 ml", f.FormatVolume(500));
        Assert.Equal("1.5 L", f.FormatVolume(1500));
        Assert.Equal("24.3°C", f.FormatTemperature(24.26));
        Assert.Equal("350 g", f.FormatWeight(350));
        Assert.Equal("1.2 kg", f.FormatWeight(1200));
        Assert.Equal("1.4 EC", f.FormatStrength(1.4));
        Assert.Equal("11 L", f.FormatContainer(11));
        Assert.Equal("–", f.FormatLength(null));
        Assert.Equal("0.75 cm/day", f.FormatRate(0.75));
    }

    [Fact]
    public void Imperial_Formatting_AndInputRoundTrip()
    {
        var f = new UnitFormatter(new UnitPreferences(UnitSystem.Imperial, TemperatureUnit.Fahrenheit, NutrientScale.Ppm500));
        Assert.Equal("10 in", f.FormatLength(25.4));
        Assert.Equal("77°F", f.FormatTemperature(25));
        Assert.Equal("700 ppm", f.FormatStrength(1.4));
        Assert.Equal("1 gal", f.FormatVolume(3785.411784));
        Assert.Equal("16.9 fl oz", f.FormatVolume(500));
        Assert.Equal("1 lb", f.FormatWeight(453.59237));
        Assert.Equal("5 gal", f.FormatContainer(Units.GalToL(5)));

        Assert.Equal(25.4, f.LengthFromDisplay(10)!.Value, 6);
        Assert.Equal(25, f.TemperatureFromDisplay(77)!.Value, 6);
        Assert.Equal(1.4, f.StrengthFromDisplay(700)!.Value, 6);
        Assert.Equal(500, f.VolumeFromDisplay(f.VolumeToDisplay(500))!.Value, 6);
        Assert.Null(f.WeightFromDisplay(null));
        Assert.Equal("in", f.LengthUnit);
        Assert.Equal("ppm", f.StrengthUnit);
    }
}
