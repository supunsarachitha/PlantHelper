using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public sealed record InfoRow(string Icon, string Label, string Value, Color Color);

public sealed record CareStatus(string Title, string Detail, string Icon, Color Color)
{
    public Color Soft => Visuals.Soft(Color);
}

public partial class PlantDetailViewModel : BaseViewModel, IQueryAttributable
{
    public enum DetailTab { Overview, Journal, Photos, Growth }

    private readonly PlantRepository _plants;
    private readonly CareLogRepository _logs;
    private readonly PhotoRepository _photos;
    private readonly PhotoStore _store;
    private readonly SettingsService _settings;
    private readonly DialogService _dialogs;
    private readonly TimeProvider _time;
    private string? _plantId;

    public PlantDetailViewModel(
        PlantRepository plants, CareLogRepository logs, PhotoRepository photos, PhotoStore store,
        SettingsService settings, DialogService dialogs, TimeProvider time)
    {
        _plants = plants;
        _logs = logs;
        _photos = photos;
        _store = store;
        _settings = settings;
        _dialogs = dialogs;
        _time = time;
        Tabs =
        [
            new((int)DetailTab.Overview, "Overview", Icons.Information, Visuals.Primary) { IsSelected = true },
            new((int)DetailTab.Journal, "Journal", Icons.History, Color.FromArgb("#8B5CF6")),
            new((int)DetailTab.Photos, "Photos", Icons.Camera, Color.FromArgb("#EC4899")),
            new((int)DetailTab.Growth, "Growth", Icons.ChartLine, Color.FromArgb("#F97316")),
        ];
    }

    public IReadOnlyList<SelectableOption> Tabs { get; }

    public Plant? Plant { get; private set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string Subtitle { get; set; } = string.Empty;
    [ObservableProperty] public partial ImageSource? Cover { get; set; }
    [ObservableProperty] public partial bool HasCover { get; set; }
    [ObservableProperty] public partial Brush? PlaceholderBrush { get; set; }
    [ObservableProperty] public partial string CategoryIcon { get; set; } = Icons.Sprout;
    [ObservableProperty] public partial string StageText { get; set; } = string.Empty;
    [ObservableProperty] public partial string StageIcon { get; set; } = Icons.Sprout;
    [ObservableProperty] public partial Color StageColor { get; set; } = Visuals.Primary;
    [ObservableProperty] public partial string AgeValue { get; set; } = "0";
    [ObservableProperty] public partial string StageDaysValue { get; set; } = "0";
    [ObservableProperty] public partial string ThirdStatTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string ThirdStatValue { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusText { get; set; } = string.Empty;
    [ObservableProperty] public partial Color StatusColor { get; set; } = Visuals.Primary;
    [ObservableProperty] public partial bool IsActive { get; set; }
    [ObservableProperty] public partial CareStatus? WaterStatus { get; set; }
    [ObservableProperty] public partial CareStatus? FeedStatus { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial bool HasNotes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteColor))]
    public partial bool IsFavorite { get; set; }

    public Color FavoriteColor => IsFavorite ? Color.FromArgb("#FB7185") : Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverviewTab), nameof(IsJournalTab), nameof(IsPhotosTab), nameof(IsGrowthTab))]
    public partial DetailTab SelectedTab { get; set; }

    partial void OnSelectedTabChanged(DetailTab value)
    {
        foreach (var t in Tabs)
            t.IsSelected = t.Value == (int)value;
    }

    public bool IsOverviewTab => SelectedTab == DetailTab.Overview;
    public bool IsJournalTab => SelectedTab == DetailTab.Journal;
    public bool IsPhotosTab => SelectedTab == DetailTab.Photos;
    public bool IsGrowthTab => SelectedTab == DetailTab.Growth;

    public ObservableCollection<InfoRow> InfoRows { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string plantId)
            _plantId = plantId;
        if (query.TryGetValue("tab", out var tab) && Enum.TryParse<DetailTab>(tab as string, true, out var t))
            SelectedTab = t;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_plantId is null)
            return;
        var plant = await _plants.GetAsync(_plantId);
        if (plant is null)
        {
            await Routes.BackAsync();
            return;
        }
        Plant = plant;
        var today = _time.GetLocalNow().Date;
        var fmt = _settings.Formatter;
        var logs = await _logs.GetForPlantAsync(plant.Id);
        var photos = await _photos.GetForPlantAsync(plant.Id);

        Name = plant.Name;
        Subtitle = string.Join(" · ", new[] { plant.Variety, plant.Species, DisplayNames.Of(plant.Category) }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var cover = photos.FirstOrDefault(p => p.Id == plant.CoverPhotoId) ?? photos.LastOrDefault();
        var coverPath = cover is not null && _store.Exists(cover.FileName) ? _store.GetPath(cover.FileName) : null;
        Cover = coverPath is null ? null : ImageSource.FromFile(coverPath);
        HasCover = Cover is not null;
        PlaceholderBrush = Visuals.CategoryGradient(plant.Category);
        CategoryIcon = Visuals.CategoryIcon(plant.Category);
        StageText = DisplayNames.Of(plant.Stage);
        StageIcon = Visuals.StageIcon(plant.Stage);
        StageColor = Visuals.StageColor(plant.Stage);
        AgeValue = plant.AgeInDays(today).ToString();
        StageDaysValue = plant.DaysInStage(today).ToString();
        IsFavorite = plant.IsFavorite;
        IsActive = plant.Status == PlantStatus.Active;
        StatusText = DisplayNames.Of(plant.Status);
        StatusColor = Visuals.StatusColor(plant.Status);
        Notes = plant.Notes;
        HasNotes = !string.IsNullOrWhiteSpace(plant.Notes);

        if (plant.ExpectedHarvestOn is { } harvest && IsActive)
        {
            var days = (int)(harvest.Date - today).TotalDays;
            ThirdStatTitle = days >= 0 ? "To harvest" : "Harvest due";
            ThirdStatValue = days >= 0 ? $"{days}d" : $"{-days}d ago";
        }
        else
        {
            ThirdStatTitle = "Photos";
            ThirdStatValue = photos.Count.ToString();
        }

        // Care status
        var lastDates = logs
            .GroupBy(l => (plant.Id, l.Type))
            .ToDictionary(g => g.Key, g => g.Max(l => l.Date));
        var (water, feed) = CareScheduleService.ComputeTasks(plant, lastDates, today);
        WaterStatus = BuildStatus(water, plant.WaterEveryDays, "Watering", Icons.Water, Visuals.LogColor(LogType.Water), today);
        FeedStatus = BuildStatus(feed, plant.FeedEveryDays, "Feeding", Icons.Flask, Visuals.LogColor(LogType.Feed), today);

        // Info rows
        InfoRows.Clear();
        void Add(string icon, string label, string? value, string color)
        {
            if (!string.IsNullOrWhiteSpace(value))
                InfoRows.Add(new InfoRow(icon, label, value, Color.FromArgb(color)));
        }
        Add(Visuals.CategoryIcon(plant.Category), "Category", DisplayNames.Of(plant.Category), "#16A34A");
        Add(Icons.PotMix, "Medium", DisplayNames.Of(plant.Medium), "#B45309");
        Add(Icons.MapMarker, "Location", DisplayNames.Of(plant.Location), "#EF4444");
        Add(Icons.ArrowExpandVertical, "Container", plant.ContainerLiters is null ? null : fmt.FormatContainer(plant.ContainerLiters), "#F97316");
        Add(Icons.Seed, "Started from", DisplayNames.Of(plant.Source), "#A16207");
        Add(Icons.Greenhouse, "Spot", plant.Spot, "#0D9488");
        Add(Icons.CalendarToday, "Planted", plant.PlantedOn.ToString("d MMM yyyy"), "#6366F1");
        Add(Icons.SproutOutline, "Germinated", plant.GerminatedOn?.ToString("d MMM yyyy"), "#65A30D");
        Add(Icons.CalendarCheck, "Expected harvest", plant.ExpectedHarvestOn?.ToString("d MMM yyyy"), "#F59E0B");
        Add(Icons.Archive, "Ended", plant.EndedOn?.ToString("d MMM yyyy"), "#64748B");
        Add(Icons.History, "Journal entries", logs.Count.ToString(), "#8B5CF6");
    }

    private static CareStatus BuildStatus(CareTask? task, int interval, string name, string icon, Color color, DateTime today)
    {
        if (task is null)
            return new CareStatus(name, interval == 0 ? "No schedule" : "Paused", icon, Visuals.Muted);
        var last = task.LastDone is { } d ? $"Last {Relative(d.Date, today)}" : "Not logged yet";
        return task.DaysOverdue switch
        {
            > 0 => new CareStatus($"{name} overdue", $"{task.DaysOverdue} day(s) late · {last}", icon, Visuals.Danger),
            0 => new CareStatus($"{name} due today", last, icon, color),
            _ => new CareStatus($"{name} in {-task.DaysOverdue}d", last, icon, color),
        };
    }

    public static string Relative(DateTime date, DateTime today) => (int)(today - date.Date).TotalDays switch
    {
        0 => "today",
        1 => "yesterday",
        < 0 and var n => $"in {-n} days",
        var n => $"{n} days ago",
    };

    [RelayCommand]
    private void SelectTab(SelectableOption tab) => SelectedTab = (DetailTab)tab.Value;

    [RelayCommand]
    private Task EditAsync() => Plant is null ? Task.CompletedTask : Routes.GoAsync($"{Routes.PlantEdit}?id={Plant.Id}");

    [RelayCommand]
    private Task BackAsync() => Routes.BackAsync();

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Plant is null)
            return;
        Plant.IsFavorite = IsFavorite = !IsFavorite;
        await _plants.SaveAsync(Plant);
    }

    [RelayCommand]
    private async Task ChangeStageAsync()
    {
        if (Plant is null)
            return;
        var names = Enum.GetValues<GrowthStage>().Select(DisplayNames.Of).ToArray();
        var choice = await _dialogs.ChooseAsync("Move to growth stage", names);
        if (choice is null)
            return;
        var stage = Enum.GetValues<GrowthStage>().First(s => DisplayNames.Of(s) == choice);
        if (stage == Plant.Stage)
            return;
        var from = Plant.Stage;
        Plant.Stage = stage;
        Plant.StageStartedOn = _time.GetLocalNow().Date;
        await _plants.SaveAsync(Plant);
        await _logs.SaveAsync(new CareLog { PlantId = Plant.Id, Type = LogType.Stage, Date = _time.GetLocalNow().DateTime, FromStage = from, ToStage = stage });
        await _dialogs.ToastAsync($"Now in {choice} {(stage is GrowthStage.Flowering ? "🌸" : "🌿")}");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        if (Plant is null)
            return;
        var options = Plant.Status == PlantStatus.Active
            ? new[] { "Mark as harvested", "Archive", "Mark as lost" }
            : new[] { "Make active again" };
        var choice = await _dialogs.ChooseAsync("Plant status", options);
        (PlantStatus status, DateTime? ended)? change = choice switch
        {
            "Mark as harvested" => (PlantStatus.Harvested, _time.GetLocalNow().Date),
            "Archive" => (PlantStatus.Archived, _time.GetLocalNow().Date),
            "Mark as lost" => (PlantStatus.Lost, _time.GetLocalNow().Date),
            "Make active again" => (PlantStatus.Active, null),
            _ => null,
        };
        if (change is null)
            return;
        Plant.Status = change.Value.status;
        Plant.EndedOn = change.Value.ended;
        await _plants.SaveAsync(Plant);
        await _dialogs.ToastAsync($"{Plant.Name}: {DisplayNames.Of(Plant.Status)}");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Plant is null)
            return;
        var ok = await _dialogs.ConfirmAsync(
            $"Delete {Plant.Name}?",
            "This permanently removes the plant with all its journal entries and photos. Consider archiving instead.",
            "Delete", "Keep");
        if (!ok)
            return;
        var files = await _plants.DeleteAsync(Plant.Id);
        foreach (var file in files)
            _store.Delete(file);
        await _dialogs.ToastAsync("Plant deleted");
        await Routes.BackAsync();
    }
}
