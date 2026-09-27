using CommunityToolkit.Mvvm.ComponentModel;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

/// <summary>A plant as shown in lists (card).</summary>
public partial class PlantItemViewModel : ObservableObject
{
    public PlantItemViewModel(Plant plant, string? coverPath, CareTask? water, CareTask? feed, DateTime today)
    {
        Plant = plant;
        CoverPath = coverPath;
        Cover = coverPath is null ? null : ImageSource.FromFile(coverPath);
        Water = water;
        Feed = feed;
        IsFavorite = plant.IsFavorite;
        AgeText = $"Day {plant.AgeInDays(today)}";

        var urgent = new[] { water, feed }
            .Where(t => t is not null)
            .OrderBy(t => t!.DueDate)
            .FirstOrDefault();
        if (urgent is not null)
        {
            var verb = urgent.Kind == CareTaskKind.Water ? "Water" : "Feed";
            CareIcon = urgent.Kind == CareTaskKind.Water ? Icons.Water : Icons.Flask;
            (CareText, CareColor) = urgent.DaysOverdue switch
            {
                > 0 => ($"{verb} · {urgent.DaysOverdue}d late", Visuals.Danger),
                0 => ($"{verb} today", Visuals.LogColor(urgent.Kind == CareTaskKind.Water ? LogType.Water : LogType.Feed)),
                -1 => ($"{verb} tomorrow", Visuals.Muted),
                _ => ($"{verb} in {-urgent.DaysOverdue}d", Visuals.Muted),
            };
            NeedsCare = urgent.DaysOverdue >= 0;
        }
    }

    public Plant Plant { get; }
    public CareTask? Water { get; }
    public CareTask? Feed { get; }

    public string Id => Plant.Id;
    public string Name => Plant.Name;
    public string Subtitle => FirstNonEmpty(Plant.Variety, Plant.Species) ?? DisplayNames.Of(Plant.Category);
    public string? CoverPath { get; }
    public ImageSource? Cover { get; }
    public bool HasCover => Cover is not null;
    public Brush PlaceholderBrush => Visuals.CategoryGradient(Plant.Category);
    public string CategoryIcon => Visuals.CategoryIcon(Plant.Category);

    public string StageText => DisplayNames.Of(Plant.Stage);
    public string StageIcon => Visuals.StageIcon(Plant.Stage);
    public Color StageColor => Visuals.StageColor(Plant.Stage);
    public Color StageSoft => Visuals.Soft(StageColor);

    public string AgeText { get; }
    public string LocationText => DisplayNames.Of(Plant.Location);

    public bool HasCare => CareText is not null;
    public bool NeedsCare { get; }
    public string? CareText { get; }
    public string CareIcon { get; } = Icons.Water;
    public Color CareColor { get; } = Visuals.Muted;
    public Color CareSoft => Visuals.Soft(CareColor);

    public bool IsInactive => Plant.Status != PlantStatus.Active;
    public string StatusText => DisplayNames.Of(Plant.Status);
    public Color StatusColor => Visuals.StatusColor(Plant.Status);
    public Color StatusSoft => Visuals.Soft(StatusColor);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteColor))]
    public partial bool IsFavorite { get; set; }

    public Color FavoriteColor => IsFavorite ? Color.FromArgb("#F43F5E") : Color.FromArgb("#94A3B8");

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
