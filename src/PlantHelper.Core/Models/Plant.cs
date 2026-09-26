using SQLite;

namespace PlantHelper.Core.Models;

public class Plant
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>Common or scientific species, e.g. "Tomato" or "Solanum lycopersicum".</summary>
    public string? Species { get; set; }

    /// <summary>Cultivar / variety / strain, e.g. "Cherry Sweet 100".</summary>
    public string? Variety { get; set; }

    public PlantCategory Category { get; set; } = PlantCategory.Vegetable;
    public GrowthStage Stage { get; set; } = GrowthStage.Seedling;
    public GrowMedium Medium { get; set; } = GrowMedium.Soil;
    public GrowLocation Location { get; set; } = GrowLocation.Indoor;
    public PlantSource Source { get; set; } = PlantSource.Seed;

    [Indexed]
    public PlantStatus Status { get; set; } = PlantStatus.Active;

    /// <summary>Container volume in litres (null = in-ground / unknown).</summary>
    public double? ContainerLiters { get; set; }

    /// <summary>Free text position, e.g. "Tent A, left" or "Raised bed 2".</summary>
    public string? Spot { get; set; }

    public DateTime PlantedOn { get; set; } = DateTime.Today;
    public DateTime? GerminatedOn { get; set; }
    public DateTime StageStartedOn { get; set; } = DateTime.Today;
    public DateTime? ExpectedHarvestOn { get; set; }
    public DateTime? EndedOn { get; set; }

    /// <summary>Watering interval in days; 0 disables watering reminders.</summary>
    public int WaterEveryDays { get; set; }

    /// <summary>Feeding interval in days; 0 disables feeding reminders.</summary>
    public int FeedEveryDays { get; set; }

    public bool IsFavorite { get; set; }

    public string? CoverPhotoId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AgeInDays(DateTime today) => Math.Max(0, (int)(today.Date - PlantedOn.Date).TotalDays);

    public int DaysInStage(DateTime today) => Math.Max(0, (int)(today.Date - StageStartedOn.Date).TotalDays);
}
