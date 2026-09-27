using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Services;

namespace PlantHelper.ViewModels;

/// <summary>Full-screen, swipeable growth timeline for one plant.</summary>
public partial class PhotoViewerViewModel(
    PhotoRepository photos, PlantRepository plants, PhotoStore store, PhotoService photoService,
    SettingsService settings, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    private List<PhotoItemViewModel> _items = [];
    private Plant? _plant;
    private string? _plantId;
    private string? _startPhotoId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Counter), nameof(HasPrevious), nameof(HasNext))]
    public partial int Index { get; set; }

    [ObservableProperty] public partial PhotoItemViewModel? Current { get; set; }

    public string Counter => _items.Count == 0 ? string.Empty : $"{Index + 1} / {_items.Count}";
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index < _items.Count - 1;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _plantId = query.TryGetValue("plantId", out var p) ? p as string : _plantId;
        _startPhotoId = query.TryGetValue("photoId", out var ph) ? ph as string : _startPhotoId;
    }

    public async Task LoadAsync()
    {
        if (_plantId is null)
            return;
        _plant = await plants.GetAsync(_plantId);
        var list = await photos.GetForPlantAsync(_plantId);
        _items = list.Where(p => store.Exists(p.FileName))
            .Select(p => new PhotoItemViewModel(p, store.GetPath(p.FileName), _plant, settings.Formatter))
            .ToList();
        if (_items.Count == 0)
        {
            await Routes.BackAsync();
            return;
        }
        var start = Current?.Id ?? _startPhotoId;
        Show(Math.Max(0, _items.FindIndex(i => i.Id == start)));
    }

    private void Show(int index)
    {
        Index = Math.Clamp(index, 0, _items.Count - 1);
        Current = _items[Index];
    }

    [RelayCommand]
    private void Next()
    {
        if (HasNext) Show(Index + 1);
    }

    [RelayCommand]
    private void Previous()
    {
        if (HasPrevious) Show(Index - 1);
    }

    [RelayCommand]
    private Task CloseAsync() => Routes.BackAsync();

    [RelayCommand]
    private Task EditAsync() => Current is null ? Task.CompletedTask : Routes.GoAsync($"{Routes.PhotoEdit}?id={Current.Id}");

    [RelayCommand]
    private Task CompareAsync() => _plantId is null ? Task.CompletedTask : Routes.GoAsync($"{Routes.PhotoCompare}?plantId={_plantId}");

    [RelayCommand]
    private async Task SetCoverAsync()
    {
        if (Current is null || _plant is null)
            return;
        _plant.CoverPhotoId = Current.Id;
        await plants.SaveAsync(_plant);
        await dialogs.ToastAsync("Cover photo updated");
    }

    [RelayCommand]
    private Task ShareAsync() => Current is null ? Task.CompletedTask : photoService.ShareAsync(Current.Photo, $"{_plant?.Name} · {Current.DayText}");

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Current is null || !await dialogs.ConfirmAsync("Delete photo?", "The photo will be removed from this device.", "Delete", "Keep"))
            return;
        await photoService.DeleteAsync(Current.Photo);
        var index = Index;
        _items.RemoveAt(index);
        if (_items.Count == 0)
        {
            await Routes.BackAsync();
            return;
        }
        Show(Math.Min(index, _items.Count - 1));
        OnPropertyChanged(nameof(Counter));
    }
}

/// <summary>Before / after comparison with growth between the two photos.</summary>
public partial class PhotoCompareViewModel(
    PhotoRepository photos, PlantRepository plants, CareLogRepository logs, PhotoStore store, SettingsService settings)
    : BaseViewModel, IQueryAttributable
{
    private List<PhotoItemViewModel> _items = [];
    private List<Core.Services.DataPoint> _heights = [];
    private string? _plantId;

    [ObservableProperty] public partial PhotoItemViewModel? Before { get; set; }
    [ObservableProperty] public partial PhotoItemViewModel? After { get; set; }
    [ObservableProperty] public partial string DaysText { get; set; } = string.Empty;
    [ObservableProperty] public partial string GrowthText { get; set; } = "–";
    [ObservableProperty] public partial string RateText { get; set; } = "–";
    [ObservableProperty] public partial bool HasEnoughPhotos { get; set; }
    private int _before, _after;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("plantId", out var p) && p is string id)
            _plantId = id;
    }

    public async Task LoadAsync()
    {
        if (_plantId is null)
            return;
        var plant = await plants.GetAsync(_plantId);
        Title = plant is null ? "Compare" : $"{plant.Name} · before & after";
        var list = await photos.GetForPlantAsync(_plantId);
        _items = list.Where(p => store.Exists(p.FileName))
            .Select(p => new PhotoItemViewModel(p, store.GetPath(p.FileName), plant, settings.Formatter))
            .ToList();
        _heights = Core.Services.GrowthAnalytics.HeightSeries(await logs.GetForPlantAsync(_plantId), list);
        HasEnoughPhotos = _items.Count >= 2;
        _before = 0;
        _after = Math.Max(0, _items.Count - 1);
        Update();
    }

    [RelayCommand] private void BeforePrevious() { _before = Math.Max(0, _before - 1); Update(); }
    [RelayCommand] private void BeforeNext() { _before = Math.Min(_items.Count - 1, _before + 1); Update(); }
    [RelayCommand] private void AfterPrevious() { _after = Math.Max(0, _after - 1); Update(); }
    [RelayCommand] private void AfterNext() { _after = Math.Min(_items.Count - 1, _after + 1); Update(); }

    private void Update()
    {
        if (_items.Count == 0)
            return;
        Before = _items[_before];
        After = _items[_after];
        var days = (int)(After.Photo.TakenAt.Date - Before.Photo.TakenAt.Date).TotalDays;
        DaysText = days switch { 0 => "Same day", 1 or -1 => "1 day apart", _ => $"{Math.Abs(days)} days apart" };

        var h1 = HeightAt(Before.Photo);
        var h2 = HeightAt(After.Photo);
        var fmt = settings.Formatter;
        if (h1 is not null && h2 is not null)
        {
            var diff = h2.Value - h1.Value;
            GrowthText = (diff >= 0 ? "+" : "−") + fmt.FormatLength(Math.Abs(diff));
            RateText = days > 0 ? fmt.FormatRate(diff / days) : "–";
        }
        else
        {
            GrowthText = "–";
            RateText = "Add heights to photos";
        }
    }

    /// <summary>The photo's own height, else the closest measurement within 3 days.</summary>
    private double? HeightAt(Core.Models.PlantPhoto photo)
    {
        if (photo.HeightCm is not null)
            return photo.HeightCm;
        var near = _heights
            .Where(h => Math.Abs((h.Date.Date - photo.TakenAt.Date).TotalDays) <= 3)
            .OrderBy(h => Math.Abs((h.Date - photo.TakenAt).TotalHours))
            .Select(h => (double?)h.Value)
            .FirstOrDefault();
        return near;
    }
}
