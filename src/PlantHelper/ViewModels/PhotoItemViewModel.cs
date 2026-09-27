using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public sealed class PhotoItemViewModel(PlantPhoto photo, string path, Plant? plant, UnitFormatter units)
{
    public PlantPhoto Photo { get; } = photo;
    public string Id => Photo.Id;
    public string Path { get; } = path;
    public ImageSource Source { get; } = ImageSource.FromFile(path);
    public string PlantName => plant?.Name ?? string.Empty;
    public string DateText => Photo.TakenAt.ToString("d MMM yyyy");
    public string DayText => plant is null ? DateText : $"Day {Math.Max(0, (int)(Photo.TakenAt.Date - plant.PlantedOn.Date).TotalDays)}";
    public string? Caption => string.IsNullOrWhiteSpace(Photo.Caption) ? null : Photo.Caption;
    public bool HasCaption => Caption is not null;
    public string? HeightText => Photo.HeightCm is null ? null : units.FormatLength(Photo.HeightCm);
    public bool HasHeight => HeightText is not null;
    public string StageText => Photo.Stage is { } s ? DisplayNames.Of(s) : string.Empty;
    public Color StageColor => Photo.Stage is { } s ? Visuals.StageColor(s) : Visuals.Muted;
    public bool IsCover => plant?.CoverPhotoId == Photo.Id;
    public string Summary => string.Join(" · ", new[] { DayText, DateText, StageText, HeightText }.Where(x => !string.IsNullOrEmpty(x)).Distinct());
}
