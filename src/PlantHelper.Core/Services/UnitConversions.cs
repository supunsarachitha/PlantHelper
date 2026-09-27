namespace PlantHelper.Core.Services;

public enum UnitCategory
{
    Length,
    Volume,
    Temperature,
    Weight,
}

public sealed record UnitDefinition(string Symbol, string Name, UnitCategory Category, double ToBase, double Offset = 0);

/// <summary>General-purpose converter used by the Tools tab.</summary>
public static class UnitConversions
{
    // Base units: cm, ml, °C, g. Value in base = value * ToBase + Offset.
    public static IReadOnlyList<UnitDefinition> All { get; } =
    [
        new("mm", "Millimetres", UnitCategory.Length, 0.1),
        new("cm", "Centimetres", UnitCategory.Length, 1),
        new("m", "Metres", UnitCategory.Length, 100),
        new("in", "Inches", UnitCategory.Length, Units.CmPerInch),
        new("ft", "Feet", UnitCategory.Length, Units.CmPerInch * 12),

        new("ml", "Millilitres", UnitCategory.Volume, 1),
        new("L", "Litres", UnitCategory.Volume, 1000),
        new("tsp", "Teaspoons (US)", UnitCategory.Volume, 4.92892159375),
        new("tbsp", "Tablespoons (US)", UnitCategory.Volume, 14.78676478125),
        new("fl oz", "Fluid ounces (US)", UnitCategory.Volume, Units.MlPerUsFlOz),
        new("cup", "Cups (US)", UnitCategory.Volume, 236.5882365),
        new("gal", "Gallons (US)", UnitCategory.Volume, Units.LitersPerUsGallon * 1000),

        new("°C", "Celsius", UnitCategory.Temperature, 1),
        new("°F", "Fahrenheit", UnitCategory.Temperature, 5.0 / 9.0, -32 * 5.0 / 9.0),
        new("K", "Kelvin", UnitCategory.Temperature, 1, -273.15),

        new("g", "Grams", UnitCategory.Weight, 1),
        new("kg", "Kilograms", UnitCategory.Weight, 1000),
        new("oz", "Ounces", UnitCategory.Weight, Units.GramsPerOunce),
        new("lb", "Pounds", UnitCategory.Weight, Units.GramsPerPound),
    ];

    public static IEnumerable<UnitDefinition> In(UnitCategory category) => All.Where(u => u.Category == category);

    public static double Convert(double value, UnitDefinition from, UnitDefinition to)
    {
        if (from.Category != to.Category)
            throw new ArgumentException("Units must be in the same category.");
        var baseValue = value * from.ToBase + from.Offset;
        return (baseValue - to.Offset) / to.ToBase;
    }
}
