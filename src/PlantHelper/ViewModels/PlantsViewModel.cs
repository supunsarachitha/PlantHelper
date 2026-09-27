using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public partial class PlantsViewModel : BaseViewModel
{
    private enum Filter { Active, NeedsCare, Favorites, Harvested, Archived, All }
    private enum SortMode { Name, Newest, Oldest, Stage, NeedsCare }

    private readonly PlantRepository _plants;
    private readonly PhotoRepository _photos;
    private readonly CareLogRepository _logs;
    private readonly PhotoStore _store;
    private readonly TimeProvider _time;
    private readonly DialogService _dialogs;
    private List<PlantItemViewModel> _all = [];
    private SortMode _sort = SortMode.Name;

    public PlantsViewModel(PlantRepository plants, PhotoRepository photos, CareLogRepository logs, PhotoStore store, TimeProvider time, DialogService dialogs)
    {
        _plants = plants;
        _photos = photos;
        _logs = logs;
        _store = store;
        _time = time;
        _dialogs = dialogs;
        Filters =
        [
            new((int)Filter.Active, "Active", Icons.Sprout, Visuals.Primary) { IsSelected = true },
            new((int)Filter.NeedsCare, "Needs care", Icons.Water, Visuals.Info),
            new((int)Filter.Favorites, "Favourites", Icons.Heart, Color.FromArgb("#F43F5E")),
            new((int)Filter.Harvested, "Harvested", Icons.Basket, Visuals.Warning),
            new((int)Filter.Archived, "Archived", Icons.Archive, Visuals.Muted),
            new((int)Filter.All, "All", Icons.FormatListChecks, Color.FromArgb("#8B5CF6")),
        ];
    }

    public ObservableCollection<PlantItemViewModel> Plants { get; } = [];
    public IReadOnlyList<SelectableOption> Filters { get; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasNoPlants { get; set; }

    [ObservableProperty]
    public partial bool HasNoResults { get; set; }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            var today = _time.GetLocalNow().Date;
            var plants = await _plants.GetAllAsync();
            var photos = await _photos.GetAllAsync(); // newest first
            var lastDates = await _logs.GetLastDatesAsync();
            var photoById = photos.ToDictionary(p => p.Id);
            var latestByPlant = photos.GroupBy(p => p.PlantId).ToDictionary(g => g.Key, g => g.First());

            _all = plants.Select(p =>
            {
                var cover = p.CoverPhotoId is { } cid && photoById.TryGetValue(cid, out var pinned) ? pinned
                    : latestByPlant.GetValueOrDefault(p.Id);
                var coverPath = cover is not null && _store.Exists(cover.FileName) ? _store.GetPath(cover.FileName) : null;
                var (water, feed) = CareScheduleService.ComputeTasks(p, lastDates, today);
                return new PlantItemViewModel(p, coverPath, water, feed, today);
            }).ToList();

            var active = _all.Count(p => !p.IsInactive);
            var needCare = _all.Count(p => p.NeedsCare);
            Summary = _all.Count == 0
                ? "Add your first plant to get started"
                : $"{active} active · {needCare} need care · {_all.Count} total";
            HasNoPlants = _all.Count == 0;
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectFilter(SelectableOption option)
    {
        foreach (var f in Filters)
            f.IsSelected = f == option;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ChangeSortAsync()
    {
        var choice = await _dialogs.ChooseAsync("Sort plants by", "Name (A–Z)", "Newest first", "Oldest first", "Growth stage", "Needs care first");
        _sort = choice switch
        {
            "Newest first" => SortMode.Newest,
            "Oldest first" => SortMode.Oldest,
            "Growth stage" => SortMode.Stage,
            "Needs care first" => SortMode.NeedsCare,
            "Name (A–Z)" => SortMode.Name,
            _ => _sort,
        };
        ApplyFilter();
    }

    [RelayCommand]
    private Task AddPlantAsync() => Routes.GoAsync(Routes.PlantEdit);

    [RelayCommand]
    private Task OpenPlantAsync(PlantItemViewModel item) =>
        Routes.GoAsync($"{Routes.PlantDetail}?id={item.Id}");

    [RelayCommand]
    private async Task ToggleFavoriteAsync(PlantItemViewModel item)
    {
        item.IsFavorite = !item.IsFavorite;
        item.Plant.IsFavorite = item.IsFavorite;
        await _plants.SaveAsync(item.Plant);
        if (Filters.First(f => f.IsSelected).Value == (int)Filter.Favorites)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = (Filter)(Filters.FirstOrDefault(f => f.IsSelected)?.Value ?? 0);
        IEnumerable<PlantItemViewModel> query = _all.Where(p => filter switch
        {
            Filter.Active => p.Plant.Status == PlantStatus.Active,
            Filter.NeedsCare => p.NeedsCare,
            Filter.Favorites => p.IsFavorite,
            Filter.Harvested => p.Plant.Status == PlantStatus.Harvested,
            Filter.Archived => p.Plant.Status is PlantStatus.Archived or PlantStatus.Lost,
            _ => true,
        });

        var term = SearchText?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p =>
                Contains(p.Plant.Name, term) || Contains(p.Plant.Variety, term) || Contains(p.Plant.Species, term)
                || Contains(p.Plant.Spot, term) || Contains(p.Plant.Notes, term)
                || Contains(DisplayNames.Of(p.Plant.Category), term) || Contains(p.StageText, term));
        }

        query = _sort switch
        {
            SortMode.Newest => query.OrderByDescending(p => p.Plant.PlantedOn),
            SortMode.Oldest => query.OrderBy(p => p.Plant.PlantedOn),
            SortMode.Stage => query.OrderBy(p => p.Plant.Stage).ThenBy(p => p.Name),
            SortMode.NeedsCare => query.OrderBy(p => p.Water?.DueDate ?? p.Feed?.DueDate ?? DateTime.MaxValue).ThenBy(p => p.Name),
            _ => query.OrderByDescending(p => p.IsFavorite).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        var list = query.ToList();
        Plants.Clear();
        foreach (var p in list)
            Plants.Add(p);
        HasNoResults = !HasNoPlants && list.Count == 0;
    }

    private static bool Contains(string? source, string term) =>
        source?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;
}
