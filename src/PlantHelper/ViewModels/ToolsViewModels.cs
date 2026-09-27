using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public sealed record ToolCard(string Title, string Subtitle, string Icon, Brush Background, string Route);

public sealed record ResultRow(string Label, string Value);

public partial class ToolsViewModel : BaseViewModel
{
    private static Brush Gradient(string a, string b) =>
        new LinearGradientBrush([new GradientStop(Color.FromArgb(a), 0), new GradientStop(Color.FromArgb(b), 1)], new Point(0, 0), new Point(1, 1));

    public IReadOnlyList<ToolCard> Tools { get; } =
    [
        new("VPD", "Vapour pressure deficit from temperature & humidity", Icons.WaterPercent, Gradient("#0891B2", "#22D3EE"), Routes.Vpd),
        new("Light (DLI)", "Daily light integral from PPFD & hours", Icons.WhiteBalanceSunny, Gradient("#F59E0B", "#FDE047"), Routes.Dli),
        new("PPM ↔ EC", "Convert between EC, CF and every PPM scale", Icons.Flask, Gradient("#7C3AED", "#C084FC"), Routes.PpmEc),
        new("Nutrient mixer", "How many ml of each product for your tank", Icons.Beaker, Gradient("#059669", "#34D399"), Routes.Dilution),
        new("Unit converter", "Length, volume, temperature & weight", Icons.SwapHorizontal, Gradient("#2563EB", "#60A5FA"), Routes.Converter),
        new("Nutrient library", "Your fertilisers, additives and label doses", Icons.BottleTonicPlus, Gradient("#DB2777", "#F472B6"), Routes.Nutrients),
    ];

    [RelayCommand]
    private Task OpenAsync(ToolCard tool) => Routes.GoAsync(tool.Route);
}

/// <summary>Shared helper: a small row of selectable chips.</summary>
public static class Chips
{
    public static List<SelectableOption> Make(Color color, params string[] labels) =>
        labels.Select((l, i) => new SelectableOption(i, l, string.Empty, color) { IsSelected = i == 0 }).ToList();

    public static void Select(IEnumerable<SelectableOption> chips, SelectableOption chosen)
    {
        foreach (var c in chips)
            c.IsSelected = c == chosen;
    }
}

public partial class VpdViewModel : BaseViewModel
{
    private static readonly GrowthStage[] StageMap = [GrowthStage.Seedling, GrowthStage.Vegetative, GrowthStage.Flowering];
    private readonly UnitFormatter _fmt;
    private readonly bool _celsius;

    public VpdViewModel(SettingsService settings)
    {
        _fmt = settings.Formatter;
        _celsius = settings.Temperature == TemperatureUnit.Celsius;
        Stages = Chips.Make(Color.FromArgb("#0891B2"), "Seedling / clone", "Vegetative", "Flower / fruit");
        Stages[1].IsSelected = true;
        Stages[0].IsSelected = false;
        Temp = _celsius ? "25" : "77";
        Humidity = "60";
        LeafOffset = _celsius ? "-2" : "-3.6";
        Recalculate();
    }

    public IReadOnlyList<SelectableOption> Stages { get; }
    public string TempUnit => _fmt.TempUnit;

    [ObservableProperty] public partial string? Temp { get; set; }
    [ObservableProperty] public partial string? Humidity { get; set; }
    [ObservableProperty] public partial string? LeafOffset { get; set; }
    [ObservableProperty] public partial string VpdText { get; set; } = "–";
    [ObservableProperty] public partial string ZoneText { get; set; } = string.Empty;
    [ObservableProperty] public partial Color ZoneColor { get; set; } = Visuals.Primary;
    [ObservableProperty] public partial string RangeText { get; set; } = string.Empty;
    [ObservableProperty] public partial string AdviceText { get; set; } = string.Empty;

    partial void OnTempChanged(string? value) => Recalculate();
    partial void OnHumidityChanged(string? value) => Recalculate();
    partial void OnLeafOffsetChanged(string? value) => Recalculate();

    [RelayCommand]
    private void SelectStage(SelectableOption stage)
    {
        Chips.Select(Stages, stage);
        Recalculate();
    }

    private void Recalculate()
    {
        var t = _fmt.TemperatureFromDisplay(Numbers.Parse(Temp));
        var rh = Numbers.Parse(Humidity);
        var offsetDisplay = Numbers.Parse(LeafOffset) ?? 0;
        var offsetC = _celsius ? offsetDisplay : offsetDisplay * 5 / 9; // a temperature *difference*
        if (t is null || rh is null or < 0 or > 100)
        {
            VpdText = "–";
            ZoneText = "Enter temperature and humidity (0–100%)";
            ZoneColor = Visuals.Muted;
            RangeText = AdviceText = string.Empty;
            return;
        }
        var stage = StageMap[Stages.First(s => s.IsSelected).Value];
        var r = GrowCalculators.EvaluateVpd(t.Value, rh.Value, stage, offsetC);
        VpdText = r.Vpd.ToString("0.00");
        (ZoneText, ZoneColor) = r.Zone switch
        {
            VpdZone.TooLow => ("Too humid", Visuals.Info),
            VpdZone.TooHigh => ("Too dry", Visuals.Danger),
            _ => ("In the sweet spot", Visuals.Primary),
        };
        RangeText = $"Ideal for this stage: {r.IdealLow:0.0}–{r.IdealHigh:0.0} kPa";
        var rhLow = GrowCalculators.HumidityForVpd(t.Value, r.IdealHigh, offsetC);
        var rhHigh = GrowCalculators.HumidityForVpd(t.Value, r.IdealLow, offsetC);
        var target = $"At {_fmt.FormatTemperature(t)} aim for {rhLow:0}–{rhHigh:0}% humidity.";
        AdviceText = r.Zone switch
        {
            VpdZone.TooLow => $"Transpiration is slow and mould risk is higher. Lower humidity or raise temperature. {target}",
            VpdZone.TooHigh => $"Plants may close stomata and stress. Raise humidity or lower temperature. {target}",
            _ => $"Great conditions for transpiration and nutrient uptake. {target}",
        };
    }
}

public partial class DliViewModel : BaseViewModel
{
    private static readonly GrowthStage[] StageMap = [GrowthStage.Seedling, GrowthStage.Vegetative, GrowthStage.Flowering, GrowthStage.Mature];

    public DliViewModel()
    {
        Stages = Chips.Make(Color.FromArgb("#F59E0B"), "Seedling", "Vegetative", "Flower / fruit", "Houseplant");
        Stages[0].IsSelected = false;
        Stages[1].IsSelected = true;
        Ppfd = "600";
        Hours = "18";
        Recalculate();
    }

    public IReadOnlyList<SelectableOption> Stages { get; }

    [ObservableProperty] public partial string? Ppfd { get; set; }
    [ObservableProperty] public partial string? Hours { get; set; }
    [ObservableProperty] public partial string DliText { get; set; } = "–";
    [ObservableProperty] public partial string VerdictText { get; set; } = string.Empty;
    [ObservableProperty] public partial Color VerdictColor { get; set; } = Visuals.Primary;
    [ObservableProperty] public partial string RangeText { get; set; } = string.Empty;
    [ObservableProperty] public partial string AdviceText { get; set; } = string.Empty;

    partial void OnPpfdChanged(string? value) => Recalculate();
    partial void OnHoursChanged(string? value) => Recalculate();

    [RelayCommand]
    private void SelectStage(SelectableOption stage)
    {
        Chips.Select(Stages, stage);
        Recalculate();
    }

    private void Recalculate()
    {
        var ppfd = Numbers.Parse(Ppfd);
        var hours = Numbers.Parse(Hours);
        if (ppfd is null or < 0 || hours is null or < 0 or > 24)
        {
            DliText = "–";
            VerdictText = "Enter PPFD and hours (0–24)";
            VerdictColor = Visuals.Muted;
            RangeText = AdviceText = string.Empty;
            return;
        }
        var stage = StageMap[Stages.First(s => s.IsSelected).Value];
        var r = GrowCalculators.EvaluateDli(ppfd.Value, hours.Value, stage);
        DliText = r.Dli.ToString("0.0");
        (VerdictText, VerdictColor) = r.Dli < r.RecommendedLow ? ("Not enough light", Visuals.Info)
            : r.Dli > r.RecommendedHigh ? ("More than needed", Visuals.Danger)
            : ("Right on target", Visuals.Primary);
        RangeText = $"Recommended: {r.RecommendedLow:0}–{r.RecommendedHigh:0} mol/m²/day";
        AdviceText = hours > 0
            ? $"For {hours:0.#} h of light, set PPFD to about {GrowCalculators.PpfdForDli(r.RecommendedLow, hours.Value):0}–{GrowCalculators.PpfdForDli(r.RecommendedHigh, hours.Value):0} µmol/m²/s."
            : "Add light hours to get a PPFD target.";
    }
}

public partial class PpmEcViewModel : BaseViewModel
{
    // Index → factor to convert the input into EC (mS/cm)
    private static readonly (string Label, double PerEc)[] Scales =
    [
        ("EC (mS/cm)", 1), ("CF", 10), ("PPM 500", 500), ("PPM 640", 640), ("PPM 700", 700),
    ];

    public PpmEcViewModel()
    {
        Sources = Chips.Make(Color.FromArgb("#7C3AED"), [.. Scales.Select(s => s.Label)]);
        Value = "1.6";
        Recalculate();
    }

    public IReadOnlyList<SelectableOption> Sources { get; }
    public ObservableCollection<ResultRow> Results { get; } = [];

    [ObservableProperty] public partial string? Value { get; set; }

    partial void OnValueChanged(string? value) => Recalculate();

    [RelayCommand]
    private void SelectSource(SelectableOption source)
    {
        Chips.Select(Sources, source);
        Recalculate();
    }

    private void Recalculate()
    {
        Results.Clear();
        if (Numbers.Parse(Value) is not { } v || v < 0)
            return;
        var from = Scales[Sources.First(s => s.IsSelected).Value];
        var ec = v / from.PerEc;
        foreach (var (label, perEc) in Scales)
        {
            var converted = ec * perEc;
            Results.Add(new ResultRow(label, perEc >= 500 ? converted.ToString("0") : converted.ToString(perEc == 10 ? "0.0" : "0.00")));
        }
    }
}

public partial class MixRowViewModel(string name, double? mlPerLiter, Action changed) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty] public partial string DoseText { get; set; } = Numbers.ToInput(mlPerLiter, 2);
    [ObservableProperty] public partial string ResultText { get; set; } = "–";

    partial void OnDoseTextChanged(string value) => changed();
}

public partial class MixerViewModel : BaseViewModel
{
    private readonly NutrientRepository _nutrients;
    private readonly DialogService _dialogs;
    private bool _loaded;

    public MixerViewModel(NutrientRepository nutrients, DialogService dialogs, SettingsService settings)
    {
        _nutrients = nutrients;
        _dialogs = dialogs;
        VolumeUnits = Chips.Make(Color.FromArgb("#059669"), "Litres", "US gallons");
        if (settings.UnitSystem == UnitSystem.Imperial)
        {
            VolumeUnits[0].IsSelected = false;
            VolumeUnits[1].IsSelected = true;
        }
        Water = "10";
    }

    public IReadOnlyList<SelectableOption> VolumeUnits { get; }
    public ObservableCollection<MixRowViewModel> Rows { get; } = [];

    [ObservableProperty] public partial string? Water { get; set; }
    [ObservableProperty] public partial double Strength { get; set; } = 100;
    [ObservableProperty] public partial string StrengthText { get; set; } = "100% of label dose";
    [ObservableProperty] public partial string TotalText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasRows { get; set; }

    partial void OnWaterChanged(string? value) => Recalculate();

    partial void OnStrengthChanged(double value)
    {
        StrengthText = $"{Math.Round(value / 5) * 5:0}% of label dose";
        Recalculate();
    }

    public async Task LoadAsync()
    {
        if (_loaded)
            return;
        _loaded = true;
        foreach (var n in (await _nutrients.GetAllAsync()).Where(n => n.DefaultMlPerLiter is > 0))
            Rows.Add(new MixRowViewModel(n.Name, n.DefaultMlPerLiter, Recalculate));
        Recalculate();
    }

    [RelayCommand]
    private void SelectVolumeUnit(SelectableOption unit)
    {
        Chips.Select(VolumeUnits, unit);
        Recalculate();
    }

    [RelayCommand]
    private async Task AddRowAsync()
    {
        var name = await _dialogs.PromptAsync("Add product", "Name of the nutrient or additive", placeholder: "e.g. Bloom B");
        if (string.IsNullOrWhiteSpace(name))
            return;
        Rows.Add(new MixRowViewModel(name.Trim(), null, Recalculate));
        Recalculate();
    }

    [RelayCommand]
    private void RemoveRow(MixRowViewModel row)
    {
        Rows.Remove(row);
        Recalculate();
    }

    private void Recalculate()
    {
        HasRows = Rows.Count > 0;
        var water = Numbers.Parse(Water);
        var gallons = VolumeUnits[1].IsSelected;
        var liters = water is null ? (double?)null : gallons ? Units.GalToL(water.Value) : water.Value;
        var strength = Math.Round(Strength / 5) * 5;
        double total = 0;
        foreach (var row in Rows)
        {
            var dose = Numbers.Parse(row.DoseText);
            if (liters is null or < 0 || dose is null or < 0)
            {
                row.ResultText = "–";
                continue;
            }
            var ml = GrowCalculators.NutrientDoseMl(dose.Value, liters.Value, strength);
            total += ml;
            row.ResultText = $"{ml:0.#} ml";
        }
        TotalText = liters is null ? string.Empty : $"Total concentrate: {total:0.#} ml for {water:0.##} {(gallons ? "gal" : "L")} ({liters:0.##} L)";
    }
}

public partial class ConverterViewModel : BaseViewModel
{
    public ConverterViewModel()
    {
        Categories = Chips.Make(Color.FromArgb("#2563EB"), "Length", "Volume", "Temperature", "Weight");
        Value = "1";
        LoadUnits();
    }

    public IReadOnlyList<SelectableOption> Categories { get; }
    public ObservableCollection<string> UnitNames { get; } = [];
    public ObservableCollection<ResultRow> Results { get; } = [];
    private List<UnitDefinition> _units = [];

    [ObservableProperty] public partial string? Value { get; set; }
    [ObservableProperty] public partial int FromIndex { get; set; }

    partial void OnValueChanged(string? value) => Recalculate();
    partial void OnFromIndexChanged(int value) => Recalculate();

    [RelayCommand]
    private void SelectCategory(SelectableOption category)
    {
        Chips.Select(Categories, category);
        LoadUnits();
    }

    private void LoadUnits()
    {
        var category = (UnitCategory)Categories.First(c => c.IsSelected).Value;
        _units = UnitConversions.In(category).ToList();
        UnitNames.Clear();
        foreach (var u in _units)
            UnitNames.Add($"{u.Name} ({u.Symbol})");
        FromIndex = 0;
        Recalculate();
    }

    private void Recalculate()
    {
        Results.Clear();
        if (Numbers.Parse(Value) is not { } v || FromIndex < 0 || FromIndex >= _units.Count)
            return;
        var from = _units[FromIndex];
        foreach (var to in _units.Where(u => u != from))
        {
            var r = UnitConversions.Convert(v, from, to);
            Results.Add(new ResultRow(to.Name, $"{UnitFormatter.Num(r, Math.Abs(r) >= 100 ? 1 : 3)} {to.Symbol}"));
        }
    }
}
