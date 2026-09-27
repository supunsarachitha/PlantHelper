using PlantHelper.Core.Models;

namespace PlantHelper.Theme;

/// <summary>Colour + icon language shared by every screen.</summary>
public static class Visuals
{
    public static readonly Color Primary = Color.FromArgb("#0E9F6E");
    public static readonly Color Danger = Color.FromArgb("#EF4444");
    public static readonly Color Warning = Color.FromArgb("#F59E0B");
    public static readonly Color Info = Color.FromArgb("#3B82F6");
    public static readonly Color Muted = Color.FromArgb("#64748B");

    public static Color Soft(Color color, float alpha = 0.16f) => color.WithAlpha(alpha);

    public static Color StageColor(GrowthStage stage) => Color.FromArgb(stage switch
    {
        GrowthStage.Seed => "#A16207",
        GrowthStage.Germination => "#CA8A04",
        GrowthStage.Seedling => "#65A30D",
        GrowthStage.Vegetative => "#16A34A",
        GrowthStage.Budding => "#C026D3",
        GrowthStage.Flowering => "#EC4899",
        GrowthStage.Fruiting => "#F97316",
        GrowthStage.Ripening => "#EF4444",
        GrowthStage.Mature => "#0D9488",
        _ => "#64748B",
    });

    public static string StageIcon(GrowthStage stage) => stage switch
    {
        GrowthStage.Seed => Icons.Seed,
        GrowthStage.Germination => Icons.SproutOutline,
        GrowthStage.Seedling => Icons.Sprout,
        GrowthStage.Vegetative => Icons.Leaf,
        GrowthStage.Budding => Icons.FlowerPollen,
        GrowthStage.Flowering => Icons.Flower,
        GrowthStage.Fruiting => Icons.FruitCherries,
        GrowthStage.Ripening => Icons.FoodApple,
        GrowthStage.Mature => Icons.Tree,
        _ => Icons.WeatherNight,
    };

    public static Color CategoryColor(PlantCategory category) => Color.FromArgb(category switch
    {
        PlantCategory.Vegetable => "#16A34A",
        PlantCategory.Herb => "#059669",
        PlantCategory.Fruit => "#EF4444",
        PlantCategory.Flower => "#EC4899",
        PlantCategory.Houseplant => "#0D9488",
        PlantCategory.Succulent => "#65A30D",
        PlantCategory.Tree => "#15803D",
        PlantCategory.Microgreens => "#84CC16",
        _ => "#64748B",
    });

    public static string CategoryIcon(PlantCategory category) => category switch
    {
        PlantCategory.Vegetable => Icons.ChiliHot,
        PlantCategory.Herb => Icons.Leaf,
        PlantCategory.Fruit => Icons.FruitCherries,
        PlantCategory.Flower => Icons.FlowerTulip,
        PlantCategory.Houseplant => Icons.PotMix,
        PlantCategory.Succulent => Icons.Cactus,
        PlantCategory.Tree => Icons.Tree,
        PlantCategory.Microgreens => Icons.Grass,
        _ => Icons.SproutOutline,
    };

    /// <summary>Two-colour gradient used as the "no photo yet" placeholder.</summary>
    public static Brush CategoryGradient(PlantCategory category)
    {
        var c = CategoryColor(category);
        return new LinearGradientBrush(
            [new GradientStop(c, 0), new GradientStop(c.WithLuminosity(Math.Min(0.75f, c.GetLuminosity() + 0.22f)), 1)],
            new Point(0, 0), new Point(1, 1));
    }

    public static Color LogColor(LogType type) => Color.FromArgb(type switch
    {
        LogType.Water => "#3B82F6",
        LogType.Feed => "#8B5CF6",
        LogType.Measure => "#F97316",
        LogType.Environment => "#06B6D4",
        LogType.Pest => "#EF4444",
        LogType.Training => "#EAB308",
        LogType.Transplant => "#B45309",
        LogType.Harvest => "#F59E0B",
        LogType.Stage => "#16A34A",
        _ => "#64748B",
    });

    public static string LogIcon(LogType type) => type switch
    {
        LogType.Water => Icons.Water,
        LogType.Feed => Icons.Flask,
        LogType.Measure => Icons.Ruler,
        LogType.Environment => Icons.Thermometer,
        LogType.Pest => Icons.Bug,
        LogType.Training => Icons.ContentCut,
        LogType.Transplant => Icons.Shovel,
        LogType.Harvest => Icons.Basket,
        LogType.Stage => Icons.Sprout,
        _ => Icons.NoteText,
    };

    public static Color StatusColor(PlantStatus status) => status switch
    {
        PlantStatus.Active => Primary,
        PlantStatus.Harvested => Warning,
        PlantStatus.Archived => Muted,
        _ => Danger,
    };

    public static Color NutrientColor(NutrientType type) => Color.FromArgb(type switch
    {
        NutrientType.Grow => "#16A34A",
        NutrientType.Bloom => "#EC4899",
        NutrientType.CalMag => "#0EA5E9",
        NutrientType.Additive => "#8B5CF6",
        NutrientType.Booster => "#F97316",
        NutrientType.Organic => "#A16207",
        NutrientType.PhUp or NutrientType.PhDown => "#EF4444",
        NutrientType.Base => "#0D9488",
        _ => "#64748B",
    });
}
