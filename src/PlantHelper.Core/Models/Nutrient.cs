using System.Text.Json.Serialization;
using SQLite;

namespace PlantHelper.Core.Models;

/// <summary>A fertiliser / additive in the grower's nutrient library.</summary>
public class Nutrient
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public NutrientType Type { get; set; } = NutrientType.Base;

    public double? N { get; set; }
    public double? P { get; set; }
    public double? K { get; set; }

    /// <summary>Label dose in ml per litre.</summary>
    public double? DefaultMlPerLiter { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Ignore, JsonIgnore]
    public string NpkText => N is null && P is null && K is null
        ? string.Empty
        : $"{N ?? 0:0.#}-{P ?? 0:0.#}-{K ?? 0:0.#}";
}
