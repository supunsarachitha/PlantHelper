using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public partial class LogEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly PlantRepository _plants;
    private readonly CareLogRepository _logs;
    private readonly NutrientRepository _nutrients;
    private readonly SettingsService _settings;
    private readonly DialogService _dialogs;
    private readonly TimeProvider _time;
    private Plant? _plant;
    private CareLog? _existing;
    private bool _loaded;

    public LogEditViewModel(PlantRepository plants, CareLogRepository logs, NutrientRepository nutrients,
        SettingsService settings, DialogService dialogs, TimeProvider time)
    {
        _plants = plants;
        _logs = logs;
        _nutrients = nutrients;
        _settings = settings;
        _dialogs = dialogs;
        _time = time;

        Types = Enum.GetValues<LogType>()
            .Where(t => t != LogType.Stage)
            .Select(t => new SelectableOption((int)t, ShortName(t), Visuals.LogIcon(t), Visuals.LogColor(t)))
            .ToList();
        Stars = Enumerable.Range(1, 5)
            .Select(i => new SelectableOption(i, i.ToString(), Icons.Star, Color.FromArgb("#F59E0B")))
            .ToList();
        Severities = Enum.GetValues<Severity>().Select(DisplayNames.Of).ToList();
        TransplantMediums = ["Unchanged", .. Enum.GetValues<GrowMedium>().Select(DisplayNames.Of)];

        var now = _time.GetLocalNow();
        Date = now.Date;
        Time = now.TimeOfDay;
        Title = "New entry";
        SelectedType = LogType.Water;
        SyncTypeChips(); // Water is the enum default, so the change callback doesn't fire for it
    }

    public IReadOnlyList<SelectableOption> Types { get; }
    public IReadOnlyList<SelectableOption> Stars { get; }
    public IReadOnlyList<string> Severities { get; }
    public IReadOnlyList<string> TransplantMediums { get; }
    public ObservableCollection<DoseItemViewModel> Doses { get; } = [];

    private UnitFormatter Units => _settings.Formatter;
    public string VolumeUnit => Units.VolumeUnit;
    public string StrengthUnit => Units.StrengthUnit;
    public string TempUnit => Units.TempUnit;
    public string LengthUnit => Units.LengthUnit;
    public string WeightUnit => Units.WeightUnit;
    public string ContainerUnit => Units.ContainerUnit;

    // Realistic example values in the user's units
    private bool Metric => _settings.UnitSystem == UnitSystem.Metric;
    public string AmountHint => Metric ? "500" : "16";
    public string TempHint => _settings.Temperature == TemperatureUnit.Celsius ? "24" : "75";
    public string WaterTempHint => _settings.Temperature == TemperatureUnit.Celsius ? "20" : "68";
    public string StrengthHint => _settings.NutrientScale == NutrientScale.Ec ? "1.2" : UnitFormatter.Num(GrowCalculators.FromEc(1.2, _settings.NutrientScale), 0);
    public string HeightHint => Metric ? "45" : "18";
    public string WidthHint => Metric ? "30" : "12";
    public string WeightHint => Metric ? "350" : "12";
    public string ContainerHint => Metric ? "11" : "3";

    [ObservableProperty] public partial string PlantName { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsExisting { get; set; }
    [ObservableProperty] public partial bool CanChangeType { get; set; } = true;
    [ObservableProperty] public partial DateTime? Date { get; set; }
    [ObservableProperty] public partial TimeSpan? Time { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaterOrFeed), nameof(IsFeed), nameof(IsMeasure), nameof(IsEnvironment), nameof(IsPest),
        nameof(IsTraining), nameof(IsTransplant), nameof(IsHarvest), nameof(IsStage), nameof(TypeTitle), nameof(TypeColor), nameof(TypeIcon))]
    public partial LogType SelectedType { get; set; }

    public bool IsWaterOrFeed => SelectedType is LogType.Water or LogType.Feed;
    public bool IsFeed => SelectedType == LogType.Feed;
    public bool IsMeasure => SelectedType == LogType.Measure;
    public bool IsEnvironment => SelectedType == LogType.Environment;
    public bool IsPest => SelectedType == LogType.Pest;
    public bool IsTraining => SelectedType == LogType.Training;
    public bool IsTransplant => SelectedType == LogType.Transplant;
    public bool IsHarvest => SelectedType == LogType.Harvest;
    public bool IsStage => SelectedType == LogType.Stage;
    public string TypeTitle => DisplayNames.Of(SelectedType);
    public Color TypeColor => Visuals.LogColor(SelectedType);
    public string TypeIcon => Visuals.LogIcon(SelectedType);

    // Water / feed
    [ObservableProperty] public partial string? Amount { get; set; }
    [ObservableProperty] public partial string? Ph { get; set; }
    [ObservableProperty] public partial string? Strength { get; set; }
    [ObservableProperty] public partial string? WaterTemp { get; set; }
    [ObservableProperty] public partial string? RunoffPh { get; set; }
    [ObservableProperty] public partial string? RunoffStrength { get; set; }
    // Measure
    [ObservableProperty] public partial string? Height { get; set; }
    [ObservableProperty] public partial string? Width { get; set; }
    [ObservableProperty] public partial string? Nodes { get; set; }
    [ObservableProperty] public partial string? Leaves { get; set; }
    // Environment
    [ObservableProperty] public partial string? Temp { get; set; }
    [ObservableProperty] public partial string? Humidity { get; set; }
    [ObservableProperty] public partial string? LightHours { get; set; }
    [ObservableProperty] public partial string? Ppfd { get; set; }
    [ObservableProperty] public partial string? Co2 { get; set; }
    [ObservableProperty] public partial string? SoilMoisture { get; set; }
    [ObservableProperty] public partial string? VpdText { get; set; }
    // Pest / training / transplant
    [ObservableProperty] public partial string? Issue { get; set; }
    [ObservableProperty] public partial int SeverityIndex { get; set; } = 1;
    [ObservableProperty] public partial string? Treatment { get; set; }
    [ObservableProperty] public partial string? ContainerSize { get; set; }
    [ObservableProperty] public partial int TransplantMediumIndex { get; set; }
    // Harvest
    [ObservableProperty] public partial string? Weight { get; set; }
    [ObservableProperty] public partial string? Quantity { get; set; }
    [ObservableProperty] public partial int Rating { get; set; }
    // Stage (read only)
    [ObservableProperty] public partial string? StageText { get; set; }

    partial void OnSelectedTypeChanged(LogType value)
    {
        SyncTypeChips();
        if (_loaded && !IsExisting)
            _ = SafeRunAsync(PrefillFromPreviousAsync);
    }

    private void SyncTypeChips()
    {
        foreach (var t in Types)
            t.IsSelected = t.Value == (int)SelectedType;
    }

    partial void OnAmountChanged(string? value) => UpdateDoseTotals();
    partial void OnTempChanged(string? value) => UpdateVpd();
    partial void OnHumidityChanged(string? value) => UpdateVpd();

    partial void OnRatingChanged(int value)
    {
        foreach (var s in Stars)
            s.IsSelected = s.Value <= value;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var plantId = query.TryGetValue("plantId", out var p) ? p as string : null;
        var logId = query.TryGetValue("id", out var i) ? i as string : null;
        if (query.TryGetValue("type", out var t) && Enum.TryParse<LogType>(t as string, true, out var type))
            SelectedType = type;
        _ = SafeRunAsync(() => LoadAsync(plantId, logId));
    }

    private async Task LoadAsync(string? plantId, string? logId)
    {
        if (logId is not null && await _logs.GetAsync(logId) is { } existing)
        {
            _existing = existing;
            IsExisting = true;
            CanChangeType = existing.Type != LogType.Stage;
            Title = "Edit entry";
            plantId = existing.PlantId;
            SelectedType = existing.Type;
            Fill(existing);
        }

        _plant = plantId is null ? null : await _plants.GetAsync(plantId);
        PlantName = _plant?.Name ?? string.Empty;
        _loaded = true;
        if (_existing is null)
            await PrefillFromPreviousAsync();
    }

    /// <summary>Growers repeat recipes, so start from the last entry of the same type.</summary>
    private async Task PrefillFromPreviousAsync()
    {
        ClearFields();
        if (_plant is null)
            return;
        var last = await _logs.GetLastAsync(_plant.Id, SelectedType);
        if (last is null)
            return;
        switch (SelectedType)
        {
            case LogType.Water:
            case LogType.Feed:
                Amount = Numbers.ToInput(Units.VolumeToDisplay(last.AmountMl), 1);
                Ph = Numbers.ToInput(last.Ph, 1);
                Strength = Numbers.ToInput(Units.StrengthToDisplay(last.Ec), 2);
                foreach (var d in last.Doses)
                    Doses.Add(new DoseItemViewModel(d.NutrientId, d.Name, d.MlPerLiter, BatchMl));
                UpdateDoseTotals();
                break;
            case LogType.Environment:
                LightHours = Numbers.ToInput(last.LightHours, 1);
                Ppfd = Numbers.ToInput(last.Ppfd, 0);
                break;
        }
    }

    private void Fill(CareLog log)
    {
        Date = log.Date.Date;
        Time = log.Date.TimeOfDay;
        Notes = log.Notes;
        Amount = Numbers.ToInput(Units.VolumeToDisplay(log.AmountMl), 1);
        Ph = Numbers.ToInput(log.Ph, 1);
        Strength = Numbers.ToInput(Units.StrengthToDisplay(log.Ec), 2);
        WaterTemp = Numbers.ToInput(Units.TemperatureToDisplay(log.WaterTempC), 1);
        RunoffPh = Numbers.ToInput(log.RunoffPh, 1);
        RunoffStrength = Numbers.ToInput(Units.StrengthToDisplay(log.RunoffEc), 2);
        Height = Numbers.ToInput(Units.LengthToDisplay(log.HeightCm), 1);
        Width = Numbers.ToInput(Units.LengthToDisplay(log.WidthCm), 1);
        Nodes = log.NodeCount?.ToString();
        Leaves = log.LeafCount?.ToString();
        Temp = Numbers.ToInput(Units.TemperatureToDisplay(log.TempC), 1);
        Humidity = Numbers.ToInput(log.HumidityPct, 0);
        LightHours = Numbers.ToInput(log.LightHours, 1);
        Ppfd = Numbers.ToInput(log.Ppfd, 0);
        Co2 = Numbers.ToInput(log.Co2Ppm, 0);
        SoilMoisture = Numbers.ToInput(log.SoilMoisturePct, 0);
        Issue = log.Title;
        SeverityIndex = (int)(log.Severity ?? Severity.Medium);
        Treatment = log.Treatment;
        ContainerSize = Numbers.ToInput(Units.ContainerToDisplay(log.ContainerLiters), 1);
        TransplantMediumIndex = log.Medium is { } m ? (int)m + 1 : 0;
        Weight = Numbers.ToInput(Units.WeightToDisplay(log.WeightG), 1);
        Quantity = log.Quantity?.ToString();
        Rating = log.Rating ?? 0;
        StageText = log.FromStage is { } f && log.ToStage is { } t ? $"{DisplayNames.Of(f)} → {DisplayNames.Of(t)}" : null;
        Doses.Clear();
        foreach (var d in log.Doses)
            Doses.Add(new DoseItemViewModel(d.NutrientId, d.Name, d.MlPerLiter, BatchMl));
        UpdateDoseTotals();
    }

    private void ClearFields()
    {
        Amount = Ph = Strength = WaterTemp = RunoffPh = RunoffStrength = null;
        Height = Width = Nodes = Leaves = null;
        Temp = Humidity = LightHours = Ppfd = Co2 = SoilMoisture = null;
        Issue = Treatment = ContainerSize = Weight = Quantity = null;
        TransplantMediumIndex = 0;
        SeverityIndex = 1;
        Rating = 0;
        Doses.Clear();
    }

    private double? BatchMl() => Units.VolumeFromDisplay(Numbers.Parse(Amount));

    private void UpdateDoseTotals()
    {
        foreach (var d in Doses)
            d.UpdateTotal();
    }

    private void UpdateVpd()
    {
        var t = Units.TemperatureFromDisplay(Numbers.Parse(Temp));
        var rh = Numbers.Parse(Humidity);
        if (t is null || rh is null or < 0 or > 100)
        {
            VpdText = null;
            return;
        }
        var stage = _plant?.Stage ?? GrowthStage.Vegetative;
        var r = GrowCalculators.EvaluateVpd(t.Value, rh.Value, stage);
        var verdict = r.Zone switch
        {
            VpdZone.TooLow => "too humid",
            VpdZone.TooHigh => "too dry",
            _ => "ideal",
        };
        VpdText = $"VPD {r.Vpd:0.00} kPa — {verdict} for {DisplayNames.Of(stage).ToLowerInvariant()} ({r.IdealLow:0.0}–{r.IdealHigh:0.0})";
    }

    [RelayCommand]
    private void SelectType(SelectableOption option) => SelectedType = (LogType)option.Value;

    [RelayCommand]
    private void SetRating(SelectableOption star) => Rating = Rating == star.Value ? 0 : star.Value;

    [RelayCommand]
    private async Task PickIssueAsync()
    {
        var choice = await _dialogs.ChooseAsync("Common problems", [.. DisplayNames.CommonPests]);
        if (choice is not null && choice != "Other")
            Issue = choice;
    }

    [RelayCommand]
    private async Task PickMethodAsync()
    {
        var choice = await _dialogs.ChooseAsync("Training method", [.. DisplayNames.TrainingMethods]);
        if (choice is not null && choice != "Other")
            Issue = choice;
    }

    [RelayCommand]
    private async Task AddDoseAsync()
    {
        var library = await _nutrients.GetAllAsync();
        const string custom = "✎ Custom product…";
        const string manage = "⚙ Manage nutrient library";
        var names = library.Select(n => string.IsNullOrWhiteSpace(n.Brand) ? n.Name : $"{n.Name} ({n.Brand})").ToList();
        var choice = await _dialogs.ChooseAsync("Add nutrient", [.. names, custom, manage]);
        if (choice is null)
            return;
        if (choice == manage)
        {
            await Routes.GoAsync(Routes.Nutrients);
            return;
        }
        if (choice == custom)
        {
            var name = await _dialogs.PromptAsync("Custom nutrient", "Product name", placeholder: "e.g. Bloom A");
            if (!string.IsNullOrWhiteSpace(name))
                Doses.Add(new DoseItemViewModel(null, name.Trim(), null, BatchMl));
        }
        else
        {
            var n = library[names.IndexOf(choice)];
            Doses.Add(new DoseItemViewModel(n.Id, n.Name, n.DefaultMlPerLiter, BatchMl));
        }
        UpdateDoseTotals();
    }

    [RelayCommand]
    private void RemoveDose(DoseItemViewModel dose) => Doses.Remove(dose);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_plant is null)
        {
            await _dialogs.AlertAsync("No plant", "This entry isn't linked to a plant.");
            return;
        }

        var errors = new List<string>();
        double? Num(string? text, string label, double min = 0, double max = double.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            var v = Numbers.Parse(text);
            if (v is null || v < min || v > max)
            {
                errors.Add(max == double.MaxValue ? $"{label} must be a number ≥ {min}" : $"{label} must be between {min} and {max}");
                return null;
            }
            return v;
        }
        double? TempNum(string? text, string label)
        {
            // Temperatures may be below zero.
            if (string.IsNullOrWhiteSpace(text))
                return null;
            var v = Numbers.Parse(text);
            if (v is null)
                errors.Add($"{label} must be a number");
            return v;
        }

        var log = _existing ?? new CareLog { PlantId = _plant.Id };
        var type = SelectedType;
        var keepStage = log.Type == LogType.Stage;
        var fresh = new CareLog { Id = log.Id, PlantId = log.PlantId, Type = type, CreatedAt = log.CreatedAt };
        if (keepStage)
        {
            fresh.FromStage = log.FromStage;
            fresh.ToStage = log.ToStage;
        }

        switch (type)
        {
            case LogType.Water:
            case LogType.Feed:
                fresh.AmountMl = Units.VolumeFromDisplay(Num(Amount, "Amount"));
                fresh.Ph = Num(Ph, "pH", 0, 14);
                fresh.Ec = Units.StrengthFromDisplay(Num(Strength, StrengthUnit));
                fresh.WaterTempC = Units.TemperatureFromDisplay(TempNum(WaterTemp, "Water temperature"));
                fresh.RunoffPh = Num(RunoffPh, "Runoff pH", 0, 14);
                fresh.RunoffEc = Units.StrengthFromDisplay(Num(RunoffStrength, $"Runoff {StrengthUnit}"));
                if (type == LogType.Feed)
                {
                    var doses = new List<NutrientDose>();
                    foreach (var d in Doses)
                    {
                        var ml = Num(d.MlPerLiterText, $"{d.Name} dose");
                        doses.Add(new NutrientDose { NutrientId = d.NutrientId, Name = d.Name, MlPerLiter = ml ?? 0 });
                    }
                    fresh.Doses = doses;
                }
                break;
            case LogType.Measure:
                fresh.HeightCm = Units.LengthFromDisplay(Num(Height, "Height"));
                fresh.WidthCm = Units.LengthFromDisplay(Num(Width, "Width"));
                fresh.NodeCount = (int?)Num(Nodes, "Nodes", 0, 10000);
                fresh.LeafCount = (int?)Num(Leaves, "Leaves", 0, 100000);
                if (fresh.HeightCm is null && fresh.WidthCm is null && fresh.NodeCount is null && fresh.LeafCount is null)
                    errors.Add("Enter at least one measurement");
                break;
            case LogType.Environment:
                fresh.TempC = Units.TemperatureFromDisplay(TempNum(Temp, "Temperature"));
                fresh.HumidityPct = Num(Humidity, "Humidity", 0, 100);
                fresh.LightHours = Num(LightHours, "Light hours", 0, 24);
                fresh.Ppfd = Num(Ppfd, "PPFD", 0, 3000);
                fresh.Co2Ppm = Num(Co2, "CO₂", 0, 10000);
                fresh.SoilMoisturePct = Num(SoilMoisture, "Soil moisture", 0, 100);
                break;
            case LogType.Pest:
                if (string.IsNullOrWhiteSpace(Issue))
                    errors.Add("Describe the problem (tap “Common” for a list)");
                fresh.Title = Issue?.Trim();
                fresh.Severity = (Severity)Math.Max(0, SeverityIndex);
                fresh.Treatment = string.IsNullOrWhiteSpace(Treatment) ? null : Treatment.Trim();
                break;
            case LogType.Training:
                if (string.IsNullOrWhiteSpace(Issue))
                    errors.Add("Choose or type a training method");
                fresh.Title = Issue?.Trim();
                break;
            case LogType.Transplant:
                fresh.ContainerLiters = Units.ContainerFromDisplay(Num(ContainerSize, "Container size"));
                fresh.Medium = TransplantMediumIndex > 0 ? (GrowMedium)(TransplantMediumIndex - 1) : null;
                break;
            case LogType.Harvest:
                fresh.WeightG = Units.WeightFromDisplay(Num(Weight, "Weight"));
                fresh.Quantity = (int?)Num(Quantity, "Quantity", 0, 1_000_000);
                fresh.Rating = Rating > 0 ? Rating : null;
                break;
        }

        if (type == LogType.Note && string.IsNullOrWhiteSpace(Notes))
            errors.Add("Write a note");

        if (errors.Count > 0)
        {
            await _dialogs.AlertAsync("Please check", string.Join("\n", errors.Distinct()));
            return;
        }

        fresh.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        fresh.Date = (Date ?? _time.GetLocalNow().Date).Date + (Time ?? TimeSpan.Zero);
        if (fresh.Date > _time.GetLocalNow().DateTime.AddMinutes(5))
        {
            await _dialogs.AlertAsync("Date", "Journal entries can't be in the future.");
            return;
        }

        IsBusy = true;
        try
        {
            await _logs.SaveAsync(fresh);
            await ApplySideEffectsAsync(fresh);
            await _dialogs.ToastAsync(IsExisting ? "Entry updated" : $"{DisplayNames.Verb(type)} ✓");
            await Routes.BackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplySideEffectsAsync(CareLog log)
    {
        if (_plant is null || IsExisting)
            return;
        if (log.Type == LogType.Transplant && (log.ContainerLiters is not null || log.Medium is not null))
        {
            _plant.ContainerLiters = log.ContainerLiters ?? _plant.ContainerLiters;
            _plant.Medium = log.Medium ?? _plant.Medium;
            await _plants.SaveAsync(_plant);
        }
        if (log.Type == LogType.Harvest && _plant.Status == PlantStatus.Active
            && await _dialogs.ConfirmAsync("Harvest complete?", $"Mark {_plant.Name} as harvested? You can keep it active for multiple pickings.", "Mark harvested", "Keep active"))
        {
            _plant.Status = PlantStatus.Harvested;
            _plant.EndedOn = log.Date.Date;
            await _plants.SaveAsync(_plant);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_existing is null)
            return;
        if (!await _dialogs.ConfirmAsync("Delete entry?", "This journal entry will be removed.", "Delete", "Keep"))
            return;
        await _logs.DeleteAsync(_existing.Id);
        await _dialogs.ToastAsync("Entry deleted");
        await Routes.BackAsync();
    }

    private static string ShortName(LogType type) => type switch
    {
        LogType.Water => "Water",
        LogType.Feed => "Feed",
        LogType.Measure => "Measure",
        LogType.Environment => "Climate",
        LogType.Pest => "Pest",
        LogType.Training => "Training",
        LogType.Transplant => "Transplant",
        LogType.Harvest => "Harvest",
        _ => "Note",
    };
}
