namespace PlantHelper.Core.Models;

public sealed record UnitPreferences(
    UnitSystem System = UnitSystem.Metric,
    TemperatureUnit Temperature = TemperatureUnit.Celsius,
    NutrientScale Scale = NutrientScale.Ec);
