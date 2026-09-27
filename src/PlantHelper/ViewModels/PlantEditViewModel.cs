using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public partial class PlantEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly PlantRepository _plants;
    private readonly CareLogRepository _logs;
    private readonly SettingsService _settings;
    private readonly DialogService _dialogs;
    private readonly TimeProvider _time;
    private Plant? _existing;

    public PlantEditViewModel(PlantRepository plants, CareLogRepository logs, SettingsService settings, DialogService dialogs, TimeProvider time)
    {
        _plants = plants;
        _logs = logs;
        _settings = settings;
        _dialogs = dialogs;
        _time = time;

        Categories = Enum.GetValues<PlantCategory>()
            .Select(c => new SelectableOption((int)c, DisplayNames.Of(c), Visuals.CategoryIcon(c), Visuals.CategoryColor(c)))
            .ToList();
        Stages = Enum.GetValues<GrowthStage>()
            .Select(s => new SelectableOption((int)s, DisplayNames.Of(s), Visuals.StageIcon(s), Visuals.StageColor(s)))
            .ToList();
        Mediums = Enum.GetValues<GrowMedium>().Select(DisplayNames.Of).ToList();
        Locations = Enum.GetValues<GrowLocation>().Select(DisplayNames.Of).ToList();
        Sources = Enum.GetValues<PlantSource>().Select(DisplayNames.Of).ToList();

        Title = "New plant";
        Select(Categories, (int)PlantCategory.Vegetable);
        Select(Stages, (int)GrowthStage.Seedling);
        PlantedOn = _time.GetLocalNow().Date;
        GerminatedOn = PlantedOn;
        ExpectedHarvestOn = PlantedOn.Value.AddDays(90);
    }

    public IReadOnlyList<SelectableOption> Categories { get; }
    public IReadOnlyList<SelectableOption> Stages { get; }
    public IReadOnlyList<string> Mediums { get; }
    public IReadOnlyList<string> Locations { get; }
    public IReadOnlyList<string> Sources { get; }

    public string ContainerUnit => _settings.Formatter.ContainerUnit;
    public bool IsNew => _existing is null;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string? Species { get; set; }
    [ObservableProperty] public partial string? Variety { get; set; }
    [ObservableProperty] public partial string? Spot { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ContainerSize { get; set; }
    [ObservableProperty] public partial int MediumIndex { get; set; }
    [ObservableProperty] public partial int LocationIndex { get; set; }
    [ObservableProperty] public partial int SourceIndex { get; set; }
    [ObservableProperty] public partial DateTime? PlantedOn { get; set; }
    [ObservableProperty] public partial bool HasGerminated { get; set; }
    [ObservableProperty] public partial DateTime? GerminatedOn { get; set; }
    [ObservableProperty] public partial bool HasExpectedHarvest { get; set; }
    [ObservableProperty] public partial DateTime? ExpectedHarvestOn { get; set; }
    [ObservableProperty] public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaterText))]
    public partial int WaterEveryDays { get; set; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedText))]
    public partial int FeedEveryDays { get; set; } = 14;

    [ObservableProperty]
    public partial string? NameError { get; set; }

    public string WaterText => IntervalText(WaterEveryDays);
    public string FeedText => IntervalText(FeedEveryDays);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string plantId && _existing?.Id != plantId)
            _ = BaseViewModel.SafeRunAsync(() => LoadAsync(plantId));
    }

    private async Task LoadAsync(string id)
    {
        var plant = await _plants.GetAsync(id);
        if (plant is null)
            return;
        _existing = plant;
        OnPropertyChanged(nameof(IsNew));
        Title = "Edit plant";
        Name = plant.Name;
        Species = plant.Species;
        Variety = plant.Variety;
        Spot = plant.Spot;
        Notes = plant.Notes;
        ContainerSize = Numbers.ToInput(_settings.Formatter.ContainerToDisplay(plant.ContainerLiters), 1);
        MediumIndex = (int)plant.Medium;
        LocationIndex = (int)plant.Location;
        SourceIndex = (int)plant.Source;
        PlantedOn = plant.PlantedOn;
        HasGerminated = plant.GerminatedOn.HasValue;
        GerminatedOn = plant.GerminatedOn ?? plant.PlantedOn;
        HasExpectedHarvest = plant.ExpectedHarvestOn.HasValue;
        ExpectedHarvestOn = plant.ExpectedHarvestOn ?? plant.PlantedOn.AddDays(90);
        WaterEveryDays = plant.WaterEveryDays;
        FeedEveryDays = plant.FeedEveryDays;
        IsFavorite = plant.IsFavorite;
        Select(Categories, (int)plant.Category);
        Select(Stages, (int)plant.Stage);
    }

    [RelayCommand]
    private void SelectCategory(SelectableOption option) => Select(Categories, option.Value);

    [RelayCommand]
    private void SelectStage(SelectableOption option) => Select(Stages, option.Value);

    [RelayCommand]
    private void ChangeWater(string delta) => WaterEveryDays = Math.Clamp(WaterEveryDays + int.Parse(delta), 0, 60);

    [RelayCommand]
    private void ChangeFeed(string delta) => FeedEveryDays = Math.Clamp(FeedEveryDays + int.Parse(delta), 0, 90);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            NameError = "Please give your plant a name.";
            return;
        }
        NameError = null;

        var containerValue = Numbers.Parse(ContainerSize);
        if (!string.IsNullOrWhiteSpace(ContainerSize) && (containerValue is null || containerValue < 0))
        {
            await _dialogs.AlertAsync("Container size", "Enter the container size as a number, or leave it empty.");
            return;
        }

        var today = _time.GetLocalNow().Date;
        var planted = (PlantedOn ?? today).Date;
        if (planted > today)
        {
            await _dialogs.AlertAsync("Planted date", "The planted date can't be in the future.");
            return;
        }

        IsBusy = true;
        try
        {
            var plant = _existing ?? new Plant();
            var newStage = (GrowthStage)Stages.First(s => s.IsSelected).Value;
            var stageChanged = _existing is not null && _existing.Stage != newStage;
            var oldStage = plant.Stage;

            plant.Name = Name.Trim();
            plant.Species = Clean(Species);
            plant.Variety = Clean(Variety);
            plant.Spot = Clean(Spot);
            plant.Notes = Clean(Notes);
            plant.Category = (PlantCategory)Categories.First(c => c.IsSelected).Value;
            plant.Stage = newStage;
            plant.Medium = (GrowMedium)Math.Max(0, MediumIndex);
            plant.Location = (GrowLocation)Math.Max(0, LocationIndex);
            plant.Source = (PlantSource)Math.Max(0, SourceIndex);
            plant.ContainerLiters = _settings.Formatter.ContainerFromDisplay(containerValue);
            plant.PlantedOn = planted;
            plant.GerminatedOn = HasGerminated ? (GerminatedOn ?? planted).Date : null;
            plant.ExpectedHarvestOn = HasExpectedHarvest ? ExpectedHarvestOn?.Date : null;
            plant.WaterEveryDays = WaterEveryDays;
            plant.FeedEveryDays = FeedEveryDays;
            plant.IsFavorite = IsFavorite;

            if (_existing is null)
                plant.StageStartedOn = newStage is GrowthStage.Seed or GrowthStage.Germination ? planted : today;
            else if (stageChanged)
                plant.StageStartedOn = today;

            await _plants.SaveAsync(plant);

            if (stageChanged)
            {
                await _logs.SaveAsync(new CareLog
                {
                    PlantId = plant.Id,
                    Type = LogType.Stage,
                    Date = _time.GetLocalNow().DateTime,
                    FromStage = oldStage,
                    ToStage = newStage,
                });
            }

            await _dialogs.ToastAsync(_existing is null ? $"{plant.Name} added 🌱" : "Changes saved");

            if (_existing is null)
                await Shell.Current.GoToAsync($"../{Routes.PlantDetail}?id={plant.Id}");
            else
                await Routes.BackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Select(IEnumerable<SelectableOption> options, int value)
    {
        foreach (var o in options)
            o.IsSelected = o.Value == value;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string IntervalText(int days) => days switch
    {
        0 => "Off",
        1 => "Every day",
        _ => $"Every {days} days",
    };
}
