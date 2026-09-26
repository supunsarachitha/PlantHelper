using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

/// <summary>Human-friendly names for enum values.</summary>
public static class DisplayNames
{
    public static string Of(GrowthStage stage) => stage.ToString();

    public static string Of(PlantCategory category) => category switch
    {
        PlantCategory.Microgreens => "Microgreens",
        _ => category.ToString(),
    };

    public static string Of(GrowMedium medium) => medium switch
    {
        GrowMedium.PottingMix => "Potting mix",
        GrowMedium.Coco => "Coco coir",
        GrowMedium.ClayPebbles => "Clay pebbles",
        GrowMedium.Water => "Water (propagation)",
        _ => medium.ToString(),
    };

    public static string Of(GrowLocation location) => location switch
    {
        GrowLocation.GrowTent => "Grow tent",
        _ => location.ToString(),
    };

    public static string Of(PlantSource source) => source.ToString();

    public static string Of(PlantStatus status) => status.ToString();

    public static string Of(Severity severity) => severity.ToString();

    public static string Of(NutrientType type) => type switch
    {
        NutrientType.CalMag => "Cal-Mag",
        NutrientType.PhUp => "pH Up",
        NutrientType.PhDown => "pH Down",
        _ => type.ToString(),
    };

    public static string Of(LogType type) => type switch
    {
        LogType.Water => "Watering",
        LogType.Feed => "Feeding",
        LogType.Measure => "Measurement",
        LogType.Environment => "Environment",
        LogType.Pest => "Pest / Disease",
        LogType.Training => "Training / Pruning",
        LogType.Transplant => "Transplant",
        LogType.Harvest => "Harvest",
        LogType.Stage => "Stage change",
        _ => "Note",
    };

    public static string Verb(LogType type) => type switch
    {
        LogType.Water => "Watered",
        LogType.Feed => "Fed",
        LogType.Measure => "Measured",
        LogType.Environment => "Checked environment",
        LogType.Pest => "Pest / disease",
        LogType.Training => "Trained",
        LogType.Transplant => "Transplanted",
        LogType.Harvest => "Harvested",
        LogType.Stage => "Stage changed",
        _ => "Note",
    };

    public static string Of(NutrientScale scale) => scale switch
    {
        NutrientScale.Ppm500 => "PPM (500 scale · Hanna/US)",
        NutrientScale.Ppm640 => "PPM (640 scale)",
        NutrientScale.Ppm700 => "PPM (700 scale · Truncheon/EU)",
        _ => "EC (mS/cm)",
    };

    public static IReadOnlyList<string> TrainingMethods { get; } =
    [
        "Topping", "FIMing", "Pinching", "Pruning", "Defoliation", "Lollipopping", "Low stress training (LST)",
        "High stress training (HST)", "Supercropping", "Staking", "Trellis / net", "Deadheading", "Other",
    ];

    public static IReadOnlyList<string> CommonPests { get; } =
    [
        "Aphids", "Spider mites", "Fungus gnats", "Whiteflies", "Thrips", "Mealybugs", "Scale insects", "Caterpillars",
        "Slugs / snails", "Powdery mildew", "Root rot", "Botrytis / bud rot", "Leaf spot", "Nutrient burn",
        "Nitrogen deficiency", "Calcium deficiency", "Magnesium deficiency", "Overwatering", "Underwatering",
        "Heat stress", "Light burn", "Other",
    ];
}
