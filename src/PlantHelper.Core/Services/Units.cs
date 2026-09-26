namespace PlantHelper.Core.Services;

/// <summary>Raw unit conversions.</summary>
public static class Units
{
    public const double CmPerInch = 2.54;
    public const double MlPerUsFlOz = 29.5735295625;
    public const double LitersPerUsGallon = 3.785411784;
    public const double GramsPerOunce = 28.349523125;
    public const double GramsPerPound = 453.59237;

    public static double CToF(double c) => c * 9 / 5 + 32;
    public static double FToC(double f) => (f - 32) * 5 / 9;
    public static double CmToIn(double cm) => cm / CmPerInch;
    public static double InToCm(double inches) => inches * CmPerInch;
    public static double MlToFlOz(double ml) => ml / MlPerUsFlOz;
    public static double FlOzToMl(double flOz) => flOz * MlPerUsFlOz;
    public static double LToGal(double liters) => liters / LitersPerUsGallon;
    public static double GalToL(double gallons) => gallons * LitersPerUsGallon;
    public static double GToOz(double grams) => grams / GramsPerOunce;
    public static double OzToG(double ounces) => ounces * GramsPerOunce;
}
