using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

public enum VpdZone
{
    TooLow,
    Ideal,
    TooHigh,
}

public sealed record VpdResult(double Vpd, double IdealLow, double IdealHigh, VpdZone Zone);

public sealed record DliResult(double Dli, double RecommendedLow, double RecommendedHigh);

/// <summary>Formulas growers use every day. All inputs are metric.</summary>
public static class GrowCalculators
{
    /// <summary>Saturation vapour pressure in kPa (Tetens equation).</summary>
    public static double SaturationVaporPressure(double tempC) =>
        0.6108 * Math.Exp(17.27 * tempC / (tempC + 237.3));

    /// <summary>Leaf vapour pressure deficit in kPa. Leaves usually run ~2 °C cooler than the air under LEDs.</summary>
    public static double Vpd(double airTempC, double relativeHumidityPct, double leafOffsetC = -2)
    {
        if (relativeHumidityPct is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(relativeHumidityPct), "Humidity must be 0–100%.");
        var leafSvp = SaturationVaporPressure(airTempC + leafOffsetC);
        var airVp = SaturationVaporPressure(airTempC) * relativeHumidityPct / 100.0;
        return Math.Max(0, leafSvp - airVp);
    }

    /// <summary>Relative humidity (%) that gives the target VPD at this air temperature. Clamped to 0–100.</summary>
    public static double HumidityForVpd(double airTempC, double targetVpd, double leafOffsetC = -2)
    {
        var rh = (SaturationVaporPressure(airTempC + leafOffsetC) - targetVpd) / SaturationVaporPressure(airTempC) * 100;
        return Math.Clamp(rh, 0, 100);
    }

    public static (double Low, double High) IdealVpdRange(GrowthStage stage) => stage switch
    {
        GrowthStage.Seed or GrowthStage.Germination or GrowthStage.Seedling => (0.4, 0.8),
        GrowthStage.Vegetative or GrowthStage.Mature or GrowthStage.Dormant => (0.8, 1.2),
        _ => (1.0, 1.5),
    };

    public static VpdResult EvaluateVpd(double airTempC, double relativeHumidityPct, GrowthStage stage, double leafOffsetC = -2)
    {
        var vpd = Vpd(airTempC, relativeHumidityPct, leafOffsetC);
        var (low, high) = IdealVpdRange(stage);
        var zone = vpd < low ? VpdZone.TooLow : vpd > high ? VpdZone.TooHigh : VpdZone.Ideal;
        return new VpdResult(vpd, low, high, zone);
    }

    /// <summary>Daily light integral (mol/m²/day) from PPFD (µmol/m²/s) and photoperiod hours.</summary>
    public static double Dli(double ppfd, double hours)
    {
        if (ppfd < 0 || hours is < 0 or > 24)
            throw new ArgumentOutOfRangeException(nameof(hours), "PPFD must be ≥ 0 and hours 0–24.");
        return ppfd * hours * 3600 / 1_000_000;
    }

    /// <summary>PPFD needed to hit a target DLI in the given photoperiod.</summary>
    public static double PpfdForDli(double dli, double hours) =>
        hours <= 0 ? 0 : dli * 1_000_000 / (hours * 3600);

    public static (double Low, double High) RecommendedDli(GrowthStage stage) => stage switch
    {
        GrowthStage.Seed or GrowthStage.Germination or GrowthStage.Seedling => (6, 12),
        GrowthStage.Vegetative => (15, 25),
        GrowthStage.Mature => (10, 20),
        GrowthStage.Dormant => (3, 8),
        _ => (20, 40),
    };

    public static DliResult EvaluateDli(double ppfd, double hours, GrowthStage stage)
    {
        var (low, high) = RecommendedDli(stage);
        return new DliResult(Dli(ppfd, hours), low, high);
    }

    public static double ScaleFactor(NutrientScale scale) => scale switch
    {
        NutrientScale.Ppm500 => 500,
        NutrientScale.Ppm640 => 640,
        NutrientScale.Ppm700 => 700,
        _ => 1,
    };

    /// <summary>EC (mS/cm) → ppm on the chosen scale (or EC unchanged).</summary>
    public static double FromEc(double ec, NutrientScale scale) => ec * ScaleFactor(scale);

    /// <summary>ppm on the chosen scale (or EC) → EC (mS/cm).</summary>
    public static double ToEc(double value, NutrientScale scale) => value / ScaleFactor(scale);

    /// <summary>Millilitres of product needed for a batch of nutrient solution.</summary>
    public static double NutrientDoseMl(double labelMlPerLiter, double waterLiters, double strengthPct = 100)
    {
        if (labelMlPerLiter < 0 || waterLiters < 0 || strengthPct < 0)
            throw new ArgumentOutOfRangeException(nameof(labelMlPerLiter), "Values must be positive.");
        return labelMlPerLiter * waterLiters * strengthPct / 100.0;
    }
}
