using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Tests;

public class CalculatorTests
{
    [Fact]
    public void SaturationVaporPressure_KnownValues()
    {
        Assert.Equal(0.6108, GrowCalculators.SaturationVaporPressure(0), 3);
        Assert.Equal(3.168, GrowCalculators.SaturationVaporPressure(25), 2);
    }

    [Fact]
    public void Vpd_AirOnly_MatchesHandCalculation()
    {
        // 25 °C, 60% RH, leaf = air -> 3.168 * 0.4 = 1.267 kPa
        Assert.Equal(1.267, GrowCalculators.Vpd(25, 60, leafOffsetC: 0), 2);
    }

    [Fact]
    public void Vpd_LeafOffsetLowersVpd()
    {
        Assert.True(GrowCalculators.Vpd(25, 60) < GrowCalculators.Vpd(25, 60, 0));
        Assert.Equal(0, GrowCalculators.Vpd(20, 100, 0), 6);
    }

    [Fact]
    public void Vpd_RejectsBadHumidity() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GrowCalculators.Vpd(25, 120));

    [Theory]
    [InlineData(26, 55, GrowthStage.Flowering, VpdZone.Ideal)]
    [InlineData(24, 90, GrowthStage.Vegetative, VpdZone.TooLow)]
    [InlineData(32, 30, GrowthStage.Seedling, VpdZone.TooHigh)]
    public void EvaluateVpd_Zones(double t, double rh, GrowthStage stage, VpdZone expected) =>
        Assert.Equal(expected, GrowCalculators.EvaluateVpd(t, rh, stage).Zone);

    [Fact]
    public void Dli_AndInverse()
    {
        Assert.Equal(34.56, GrowCalculators.Dli(800, 12), 2);
        Assert.Equal(800, GrowCalculators.PpfdForDli(34.56, 12), 1);
        Assert.Equal(0, GrowCalculators.PpfdForDli(20, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GrowCalculators.Dli(500, 25));
    }

    [Theory]
    [InlineData(NutrientScale.Ec, 1.6)]
    [InlineData(NutrientScale.Ppm500, 800)]
    [InlineData(NutrientScale.Ppm640, 1024)]
    [InlineData(NutrientScale.Ppm700, 1120)]
    public void Ec_Ppm_RoundTrip(NutrientScale scale, double expected)
    {
        Assert.Equal(expected, GrowCalculators.FromEc(1.6, scale), 6);
        Assert.Equal(1.6, GrowCalculators.ToEc(expected, scale), 6);
    }

    [Fact]
    public void NutrientDose()
    {
        Assert.Equal(20, GrowCalculators.NutrientDoseMl(2, 10));
        Assert.Equal(10, GrowCalculators.NutrientDoseMl(2, 10, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => GrowCalculators.NutrientDoseMl(-1, 10));
    }

    [Fact]
    public void UnitConversions()
    {
        Assert.Equal(212, Units.CToF(100), 6);
        Assert.Equal(0, Units.FToC(32), 6);
        Assert.Equal(1, Units.CmToIn(2.54), 6);
        Assert.Equal(3.785411784, Units.GalToL(1), 9);
        Assert.Equal(1, Units.MlToFlOz(29.5735295625), 9);
        Assert.Equal(28.349523125, Units.OzToG(1), 9);
    }
}

public class ToolMathTests
{
    [Fact]
    public void HumidityForVpd_InvertsVpd()
    {
        var rh = GrowCalculators.HumidityForVpd(26, 1.2);
        Assert.Equal(1.2, GrowCalculators.Vpd(26, rh), 3);
        Assert.Equal(100, GrowCalculators.HumidityForVpd(20, -5));
        Assert.Equal(0, GrowCalculators.HumidityForVpd(20, 50));
    }

    [Theory]
    [InlineData("cm", "in", 2.54, 1)]
    [InlineData("ft", "cm", 1, 30.48)]
    [InlineData("gal", "L", 1, 3.785411784)]
    [InlineData("tsp", "ml", 1, 4.92892159375)]
    [InlineData("°C", "°F", 100, 212)]
    [InlineData("°F", "°C", 32, 0)]
    [InlineData("°C", "K", 0, 273.15)]
    [InlineData("K", "°F", 373.15, 212)]
    [InlineData("lb", "g", 1, 453.59237)]
    [InlineData("kg", "oz", 1, 35.27396195)]
    public void Convert(string from, string to, double value, double expected)
    {
        var f = UnitConversions.All.Single(u => u.Symbol == from);
        var t = UnitConversions.All.Single(u => u.Symbol == to);
        Assert.Equal(expected, UnitConversions.Convert(value, f, t), 6);
    }

    [Fact]
    public void Convert_AcrossCategories_Throws() =>
        Assert.Throws<ArgumentException>(() => UnitConversions.Convert(1, UnitConversions.All[0], UnitConversions.All[^1]));

    [Fact]
    public void EveryCategoryHasUnits() =>
        Assert.All(Enum.GetValues<UnitCategory>(), c => Assert.True(UnitConversions.In(c).Count() >= 3));
}
