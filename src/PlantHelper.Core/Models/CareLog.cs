using System.Text.Json;
using SQLite;

namespace PlantHelper.Core.Models;

/// <summary>
/// A single journal entry for a plant. One table holds every log type; only the
/// fields relevant to <see cref="Type"/> are filled. All values are stored in
/// metric units (ml, cm, °C, g, EC mS/cm) and converted for display.
/// </summary>
public class CareLog
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed, NotNull]
    public string PlantId { get; set; } = string.Empty;

    [Indexed]
    public LogType Type { get; set; }

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    // Water / Feed
    public double? AmountMl { get; set; }
    public double? Ph { get; set; }
    public double? Ec { get; set; }
    public double? WaterTempC { get; set; }
    public double? RunoffPh { get; set; }
    public double? RunoffEc { get; set; }

    /// <summary>Feed doses serialised as JSON (see <see cref="Doses"/>).</summary>
    public string? DosesJson { get; set; }

    // Measure
    public double? HeightCm { get; set; }
    public double? WidthCm { get; set; }
    public int? NodeCount { get; set; }
    public int? LeafCount { get; set; }

    // Environment
    public double? TempC { get; set; }
    public double? HumidityPct { get; set; }
    public double? LightHours { get; set; }
    public double? Ppfd { get; set; }
    public double? Co2Ppm { get; set; }
    public double? SoilMoisturePct { get; set; }

    // Pest / Training / Transplant: short title (issue, method, container description)
    public string? Title { get; set; }
    public Severity? Severity { get; set; }
    public string? Treatment { get; set; }

    // Transplant
    public double? ContainerLiters { get; set; }
    public GrowMedium? Medium { get; set; }

    // Harvest
    public double? WeightG { get; set; }
    public int? Quantity { get; set; }
    public int? Rating { get; set; }

    // Stage change
    public GrowthStage? FromStage { get; set; }
    public GrowthStage? ToStage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Ignore]
    public List<NutrientDose> Doses
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DosesJson))
                return [];
            try
            {
                return JsonSerializer.Deserialize(DosesJson, CoreJsonContext.Default.ListNutrientDose) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
        set => DosesJson = value is { Count: > 0 }
            ? JsonSerializer.Serialize(value, CoreJsonContext.Default.ListNutrientDose)
            : null;
    }

    /// <summary>Feeding waters the plant too, so it resets the watering clock.</summary>
    [Ignore]
    public bool CountsAsWatering => Type is LogType.Water or LogType.Feed;
}

public class NutrientDose
{
    public string? NutrientId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Millilitres of product per litre of water.</summary>
    public double MlPerLiter { get; set; }
}
