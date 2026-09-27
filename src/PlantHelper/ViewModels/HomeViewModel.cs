using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

/// <summary>A watering / feeding task card on the dashboard.</summary>
public sealed class TaskItemViewModel(CareTask task, string? coverPath, DateTime today)
{
    public CareTask Task { get; } = task;
    public string PlantId => Task.Plant.Id;
    public ImageSource? Cover { get; } = coverPath is null ? null : ImageSource.FromFile(coverPath);
    public bool HasCover => Cover is not null;
    public Brush Placeholder => Visuals.CategoryGradient(Task.Plant.Category);
    public string CategoryIcon => Visuals.CategoryIcon(Task.Plant.Category);
    public bool IsWater => Task.Kind == CareTaskKind.Water;
    public string Title => $"{(IsWater ? "Water" : "Feed")} {Task.Plant.Name}";
    public string Icon => IsWater ? Icons.Water : Icons.Flask;
    public Color Color => Task.IsOverdue ? Visuals.Danger : Visuals.LogColor(IsWater ? LogType.Water : LogType.Feed);
    public Color Soft => Visuals.Soft(Color);
    public string Status => Task.DaysOverdue switch
    {
        > 1 => $"Overdue by {Task.DaysOverdue} days",
        1 => "Overdue since yesterday",
        0 => "Due today",
        -1 => "Tomorrow",
        _ => Task.DueDate.ToString("dddd"),
    } + (Task.LastDone is { } last ? $" · last {PlantDetailViewModel.Relative(last.Date, today)}" : string.Empty);
}

public partial class HomeViewModel(
    PlantRepository plants, CareLogRepository logs, PhotoRepository photos, PhotoStore store,
    CareScheduleService schedule, CareActionsService care, PhotoService photoService,
    SettingsService settings, DialogService dialogs, TimeProvider time) : BaseViewModel
{
    public ObservableCollection<TaskItemViewModel> Today { get; } = [];
    public ObservableCollection<TaskItemViewModel> Upcoming { get; } = [];
    public ObservableCollection<LogItemViewModel> Recent { get; } = [];

    [ObservableProperty] public partial string Greeting { get; set; } = "Hello";
    [ObservableProperty] public partial string DateText { get; set; } = string.Empty;
    [ObservableProperty] public partial int ActiveCount { get; set; }
    [ObservableProperty] public partial int DueCount { get; set; }
    [ObservableProperty] public partial int PhotoCount { get; set; }
    [ObservableProperty] public partial int WeekLogCount { get; set; }
    [ObservableProperty] public partial bool HasPlants { get; set; }
    [ObservableProperty] public partial bool HasTasks { get; set; }
    [ObservableProperty] public partial bool AllCaughtUp { get; set; }
    [ObservableProperty] public partial bool HasUpcoming { get; set; }
    [ObservableProperty] public partial bool HasRecent { get; set; }
    [ObservableProperty] public partial string TipTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string TipText { get; set; } = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var now = time.GetLocalNow();
        var today = now.Date;
        var name = settings.GrowerName;
        var part = now.Hour switch { < 12 => "Good morning", < 17 => "Good afternoon", _ => "Good evening" };
        Greeting = string.IsNullOrWhiteSpace(name) ? part : $"{part}, {name}";
        DateText = now.ToString("dddd, d MMMM");
        (TipTitle, TipText) = GrowerTips.ForDay(today);

        var all = await plants.GetAllAsync();
        var allPhotos = await photos.GetAllAsync(); // newest first
        var covers = all.ToDictionary(p => p.Id, p =>
        {
            var photo = allPhotos.FirstOrDefault(x => x.Id == p.CoverPhotoId) ?? allPhotos.FirstOrDefault(x => x.PlantId == p.Id);
            return photo is not null && store.Exists(photo.FileName) ? store.GetPath(photo.FileName) : null;
        });

        ActiveCount = all.Count(p => p.Status == PlantStatus.Active);
        HasPlants = all.Count > 0;
        PhotoCount = allPhotos.Count;
        WeekLogCount = await logs.CountSinceAsync(today.AddDays(-6));

        var tasks = await schedule.GetTasksAsync(lookaheadDays: 3);
        Today.Clear();
        Upcoming.Clear();
        foreach (var t in tasks)
        {
            var item = new TaskItemViewModel(t, covers.GetValueOrDefault(t.Plant.Id), today);
            (t.DaysOverdue >= 0 ? Today : Upcoming).Add(item);
        }
        DueCount = Today.Count;
        HasTasks = Today.Count > 0;
        AllCaughtUp = ActiveCount > 0 && !HasTasks;
        HasUpcoming = Upcoming.Count > 0;

        var byId = all.ToDictionary(p => p.Id);
        var describer = new LogDescriber(settings.Formatter);
        Recent.Clear();
        foreach (var log in await logs.GetRecentAsync(6))
            Recent.Add(new LogItemViewModel(log, describer, byId.GetValueOrDefault(log.PlantId)?.Name));
        HasRecent = Recent.Count > 0;
    }

    [RelayCommand]
    private async Task CompleteAsync(TaskItemViewModel item)
    {
        await care.QuickLogAsync(item.Task.Plant, item.Task.Kind);
        await dialogs.ToastAsync(item.IsWater ? $"💧 {item.Task.Plant.Name} watered" : $"🧪 {item.Task.Plant.Name} fed");
        await LoadAsync();
    }

    [RelayCommand]
    private Task OpenTaskAsync(TaskItemViewModel item) => Routes.GoAsync($"{Routes.PlantDetail}?id={item.PlantId}");

    [RelayCommand]
    private Task OpenRecentAsync(LogItemViewModel item) => Routes.GoAsync($"{Routes.PlantDetail}?id={item.Log.PlantId}&tab=Journal");

    [RelayCommand]
    private Task AddPlantAsync() => Routes.GoAsync(Routes.PlantEdit);

    [RelayCommand]
    private Task ToolsAsync() => Shell.Current.GoToAsync("//tools");

    [RelayCommand]
    private Task AllPlantsAsync() => Shell.Current.GoToAsync("//plants");

    [RelayCommand]
    private async Task QuickLogAsync()
    {
        var plant = await ChoosePlantAsync("Log care for…");
        if (plant is null)
            return;
        var types = Enum.GetValues<LogType>().Where(t => t != LogType.Stage).ToList();
        var choice = await dialogs.ChooseAsync($"{plant.Name}: what did you do?", [.. types.Select(DisplayNames.Of)]);
        if (choice is not null)
            await Routes.GoAsync($"{Routes.LogEdit}?plantId={plant.Id}&type={types.First(t => DisplayNames.Of(t) == choice)}");
    }

    [RelayCommand]
    private async Task QuickPhotoAsync()
    {
        var plant = await ChoosePlantAsync("Photo of…");
        if (plant is null)
            return;
        var added = await photoService.AddInteractiveAsync(plant);
        if (added.Count == 1)
            await Routes.GoAsync($"{Routes.PhotoEdit}?id={added[0].Id}&new=1");
        else if (added.Count > 1)
        {
            await dialogs.ToastAsync($"{added.Count} photos added");
            await LoadAsync();
        }
    }

    private async Task<Plant?> ChoosePlantAsync(string title)
    {
        var active = await plants.GetByStatusAsync(PlantStatus.Active);
        if (active.Count == 0)
        {
            if (await dialogs.ConfirmAsync("No active plants", "Add a plant first?", "Add plant", "Later"))
                await AddPlantAsync();
            return null;
        }
        if (active.Count == 1)
            return active[0];
        return await dialogs.ChooseItemAsync(title, active, p => p.Name);
    }
}
