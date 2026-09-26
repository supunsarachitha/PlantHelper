using SQLite;

namespace PlantHelper.Core.Models;

public class PlantPhoto
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed, NotNull]
    public string PlantId { get; set; } = string.Empty;

    /// <summary>File name inside the app's photo folder (never a full path, so backups stay portable).</summary>
    [NotNull]
    public string FileName { get; set; } = string.Empty;

    [Indexed]
    public DateTime TakenAt { get; set; } = DateTime.Now;

    public string? Caption { get; set; }

    /// <summary>Optional plant height recorded with the photo, in cm.</summary>
    public double? HeightCm { get; set; }

    public GrowthStage? Stage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
