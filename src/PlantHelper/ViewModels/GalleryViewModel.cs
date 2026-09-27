using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public partial class GalleryViewModel(
    PhotoRepository photos, PlantRepository plants, PhotoStore store, PhotoService photoService,
    SettingsService settings, DialogService dialogs) : BaseViewModel
{
    private static readonly string[] ChipColors = ["#EC4899", "#8B5CF6", "#3B82F6", "#06B6D4", "#10B981", "#F59E0B", "#F97316", "#EF4444"];
    private List<PhotoItemViewModel> _all = [];
    private string? _selectedPlantId;

    public ObservableCollection<PhotoItemViewModel> Photos { get; } = [];
    public ObservableCollection<SelectableOption> Filters { get; } = [];
    private readonly List<string?> _filterPlantIds = [];

    [ObservableProperty] public partial string Summary { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var allPlants = (await plants.GetAllAsync()).ToDictionary(p => p.Id);
        var list = await photos.GetAllAsync();
        _all = list
            .Where(p => allPlants.ContainsKey(p.PlantId) && store.Exists(p.FileName))
            .Select(p => new PhotoItemViewModel(p, store.GetPath(p.FileName), allPlants[p.PlantId], settings.Formatter))
            .ToList();

        var withPhotos = _all.GroupBy(p => p.Photo.PlantId).Select(g => allPlants[g.Key]).OrderBy(p => p.Name).ToList();
        if (_selectedPlantId is not null && withPhotos.All(p => p.Id != _selectedPlantId))
            _selectedPlantId = null;

        Filters.Clear();
        _filterPlantIds.Clear();
        Filters.Add(new SelectableOption(0, $"All ({_all.Count})", Icons.ImageMultiple, Visuals.Primary) { IsSelected = _selectedPlantId is null });
        _filterPlantIds.Add(null);
        for (var i = 0; i < withPhotos.Count; i++)
        {
            var p = withPhotos[i];
            Filters.Add(new SelectableOption(i + 1, p.Name, Visuals.CategoryIcon(p.Category), Color.FromArgb(ChipColors[i % ChipColors.Length]))
            {
                IsSelected = p.Id == _selectedPlantId,
            });
            _filterPlantIds.Add(p.Id);
        }

        Summary = _all.Count == 0 ? "Photos you take will appear here" : $"{_all.Count} photo{(_all.Count == 1 ? "" : "s")} · {withPhotos.Count} plant{(withPhotos.Count == 1 ? "" : "s")}";
        IsEmpty = _all.Count == 0;
        Apply();
    }

    [RelayCommand]
    private void SelectFilter(SelectableOption option)
    {
        foreach (var f in Filters)
            f.IsSelected = f == option;
        _selectedPlantId = _filterPlantIds[option.Value];
        Apply();
    }

    private void Apply()
    {
        Photos.Clear();
        foreach (var p in _all.Where(p => _selectedPlantId is null || p.Photo.PlantId == _selectedPlantId))
            Photos.Add(p);
    }

    [RelayCommand]
    private Task OpenAsync(PhotoItemViewModel item) =>
        Routes.GoAsync($"{Routes.PhotoViewer}?plantId={item.Photo.PlantId}&photoId={item.Id}");

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var active = await plants.GetByStatusAsync(PlantStatus.Active);
        if (active.Count == 0)
        {
            await dialogs.AlertAsync("No plants yet", "Add a plant first, then capture its growth.");
            return;
        }
        var plant = active.Count == 1 ? active[0] : null;
        if (plant is null)
        {
            var name = await dialogs.ChooseAsync("Which plant?", [.. active.Select(p => p.Name)]);
            plant = active.FirstOrDefault(p => p.Name == name);
        }
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
}
