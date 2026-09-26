using System.Globalization;
using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

/// <summary>
/// Converts stored metric values to the user's preferred units for display and input, and back.
/// Input unit for volume: ml (metric) or fl oz (imperial). Container: L or gal.
/// </summary>
public sealed class UnitFormatter(UnitPreferences preferences)
{
    public UnitPreferences Preferences { get; } = preferences;

    private bool Metric => Preferences.System == UnitSystem.Metric;
    private bool Celsius => Preferences.Temperature == TemperatureUnit.Celsius;
    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    public string LengthUnit => Metric ? "cm" : "in";
    public string VolumeUnit => Metric ? "ml" : "fl oz";
    public string ContainerUnit => Metric ? "L" : "gal";
    public string TempUnit => Celsius ? "°C" : "°F";
    public string WeightUnit => Metric ? "g" : "oz";
    public string StrengthUnit => Preferences.Scale == NutrientScale.Ec ? "EC" : "ppm";
    public string DoseUnit => "ml/L";

    public double? LengthToDisplay(double? cm) => cm is null ? null : Metric ? cm : Units.CmToIn(cm.Value);
    public double? LengthFromDisplay(double? v) => v is null ? null : Metric ? v : Units.InToCm(v.Value);

    public double? VolumeToDisplay(double? ml) => ml is null ? null : Metric ? ml : Units.MlToFlOz(ml.Value);
    public double? VolumeFromDisplay(double? v) => v is null ? null : Metric ? v : Units.FlOzToMl(v.Value);

    public double? ContainerToDisplay(double? liters) => liters is null ? null : Metric ? liters : Units.LToGal(liters.Value);
    public double? ContainerFromDisplay(double? v) => v is null ? null : Metric ? v : Units.GalToL(v.Value);

    public double? TemperatureToDisplay(double? c) => c is null ? null : Celsius ? c : Units.CToF(c.Value);
    public double? TemperatureFromDisplay(double? v) => v is null ? null : Celsius ? v : Units.FToC(v.Value);

    public double? WeightToDisplay(double? g) => g is null ? null : Metric ? g : Units.GToOz(g.Value);
    public double? WeightFromDisplay(double? v) => v is null ? null : Metric ? v : Units.OzToG(v.Value);

    public double? StrengthToDisplay(double? ec) => ec is null ? null : GrowCalculators.FromEc(ec.Value, Preferences.Scale);
    public double? StrengthFromDisplay(double? v) => v is null ? null : GrowCalculators.ToEc(v.Value, Preferences.Scale);

    public string FormatLength(double? cm) => cm is null ? "–" : $"{Num(LengthToDisplay(cm)!.Value, 1)} {LengthUnit}";

    public string FormatVolume(double? ml)
    {
        if (ml is null)
            return "–";
        if (Metric)
            return ml >= 1000 ? $"{Num(ml.Value / 1000, 2)} L" : $"{Num(ml.Value, 0)} ml";
        var flOz = Units.MlToFlOz(ml.Value);
        return flOz >= 128 ? $"{Num(flOz / 128, 2)} gal" : $"{Num(flOz, 1)} fl oz";
    }

    public string FormatContainer(double? liters) => liters is null ? "–" : $"{Num(ContainerToDisplay(liters)!.Value, 1)} {ContainerUnit}";

    public string FormatTemperature(double? c) => c is null ? "–" : $"{Num(TemperatureToDisplay(c)!.Value, 1)}{TempUnit}";

    public string FormatWeight(double? g)
    {
        if (g is null)
            return "–";
        if (Metric)
            return g >= 1000 ? $"{Num(g.Value / 1000, 2)} kg" : $"{Num(g.Value, 1)} g";
        var oz = Units.GToOz(g.Value);
        return oz >= 16 ? $"{Num(oz / 16, 2)} lb" : $"{Num(oz, 1)} oz";
    }

    public string FormatStrength(double? ec) => ec is null
        ? "–"
        : Preferences.Scale == NutrientScale.Ec
            ? $"{Num(ec.Value, 2)} EC"
            : $"{Num(StrengthToDisplay(ec)!.Value, 0)} ppm";

    public string FormatRate(double? cmPerDay) => cmPerDay is null ? "–" : $"{Num(LengthToDisplay(cmPerDay)!.Value, 2)} {LengthUnit}/day";

    public static string Num(double value, int decimals) =>
        Math.Round(value, decimals).ToString(decimals == 0 ? "0" : "0." + new string('#', decimals), Culture);
}
