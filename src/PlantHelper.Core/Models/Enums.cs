namespace PlantHelper.Core.Models;

public enum PlantCategory
{
    Vegetable,
    Herb,
    Fruit,
    Flower,
    Houseplant,
    Succulent,
    Tree,
    Microgreens,
    Other,
}

public enum GrowthStage
{
    Seed,
    Germination,
    Seedling,
    Vegetative,
    Budding,
    Flowering,
    Fruiting,
    Ripening,
    Mature,
    Dormant,
}

public enum GrowMedium
{
    Soil,
    PottingMix,
    Coco,
    Hydroponic,
    Aeroponic,
    Aquaponic,
    Rockwool,
    ClayPebbles,
    Perlite,
    Water,
    Other,
}

public enum GrowLocation
{
    Indoor,
    GrowTent,
    Greenhouse,
    Outdoor,
    Garden,
    Balcony,
    Windowsill,
}

public enum PlantSource
{
    Seed,
    Cutting,
    Clone,
    Nursery,
    Division,
    Bulb,
    Other,
}

public enum PlantStatus
{
    Active,
    Harvested,
    Archived,
    Lost,
}

public enum LogType
{
    Water,
    Feed,
    Measure,
    Environment,
    Pest,
    Training,
    Transplant,
    Harvest,
    Stage,
    Note,
}

public enum Severity
{
    Low,
    Medium,
    High,
}

public enum NutrientType
{
    Base,
    Grow,
    Bloom,
    CalMag,
    Additive,
    Booster,
    Organic,
    PhUp,
    PhDown,
    Other,
}

public enum CareTaskKind
{
    Water,
    Feed,
}

public enum UnitSystem
{
    Metric,
    Imperial,
}

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit,
}

/// <summary>How nutrient strength is shown. Stored internally as EC (mS/cm).</summary>
public enum NutrientScale
{
    Ec,
    Ppm500,
    Ppm640,
    Ppm700,
}
