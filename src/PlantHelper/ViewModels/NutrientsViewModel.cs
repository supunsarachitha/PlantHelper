using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

public sealed class NutrientItemViewModel(Nutrient nutrient)
{
    public Nutrient Nutrient { get; } = nutrient;
    public string Name => Nutrient.Name;
    public string Subtitle => string.Join(" · ", new[] { Nutrient.Brand, DisplayNames.Of(Nutrient.Type) }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public string Npk => Nutrient.NpkText;
    public bool HasNpk => Npk.Length > 0;
    public string DoseText => Nutrient.DefaultMlPerLiter is { } d ? $"{UnitFormatter.Num(d, 2)} ml/L" : string.Empty;
    public bool HasDose => DoseText.Length > 0;
    public Color Color => Visuals.NutrientColor(Nutrient.Type);
    public Color Soft => Visuals.Soft(Color);
    public string Initial => Nutrient.Name[..1].ToUpperInvariant();
}

public partial class NutrientsViewModel(NutrientRepository nutrients) : BaseViewModel
{
    public ObservableCollection<NutrientItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var all = await nutrients.GetAllAsync();
        Items.Clear();
        foreach (var n in all)
            Items.Add(new NutrientItemViewModel(n));
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private Task AddAsync() => Routes.GoAsync(Routes.NutrientEdit);

    [RelayCommand]
    private Task OpenAsync(NutrientItemViewModel item) => Routes.GoAsync($"{Routes.NutrientEdit}?id={item.Nutrient.Id}");
}

public partial class NutrientEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly NutrientRepository _nutrients;
    private readonly DialogService _dialogs;
    private Nutrient? _existing;

    public NutrientEditViewModel(NutrientRepository nutrients, DialogService dialogs)
    {
        _nutrients = nutrients;
        _dialogs = dialogs;
        Types = Enum.GetValues<NutrientType>()
            .Select(t => new SelectableOption((int)t, DisplayNames.Of(t), Icons.Flask, Visuals.NutrientColor(t)))
            .ToList();
        Types[0].IsSelected = true;
        Title = "New nutrient";
    }

    public IReadOnlyList<SelectableOption> Types { get; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string? Brand { get; set; }
    [ObservableProperty] public partial string? N { get; set; }
    [ObservableProperty] public partial string? P { get; set; }
    [ObservableProperty] public partial string? K { get; set; }
    [ObservableProperty] public partial string? Dose { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial bool IsExisting { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string nid)
            _ = SafeRunAsync(() => LoadAsync(nid));
    }

    private async Task LoadAsync(string id)
    {
        if (await _nutrients.GetAsync(id) is not { } n)
            return;
        _existing = n;
        IsExisting = true;
        Title = "Edit nutrient";
        Name = n.Name;
        Brand = n.Brand;
        N = Numbers.ToInput(n.N, 2);
        P = Numbers.ToInput(n.P, 2);
        K = Numbers.ToInput(n.K, 2);
        Dose = Numbers.ToInput(n.DefaultMlPerLiter, 2);
        Notes = n.Notes;
        foreach (var t in Types)
            t.IsSelected = t.Value == (int)n.Type;
    }

    [RelayCommand]
    private void SelectType(SelectableOption option)
    {
        foreach (var t in Types)
            t.IsSelected = t == option;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogs.AlertAsync("Name needed", "Please enter the product name.");
            return;
        }
        var values = new[] { N, P, K, Dose }.Select(Numbers.Parse).ToArray();
        var raw = new[] { N, P, K, Dose };
        for (var i = 0; i < raw.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(raw[i]) && values[i] is null or < 0)
            {
                await _dialogs.AlertAsync("Check numbers", "N, P, K and dose must be positive numbers.");
                return;
            }
        }

        var n = _existing ?? new Nutrient();
        n.Name = Name.Trim();
        n.Brand = string.IsNullOrWhiteSpace(Brand) ? null : Brand.Trim();
        n.Type = (NutrientType)Types.First(t => t.IsSelected).Value;
        (n.N, n.P, n.K, n.DefaultMlPerLiter) = (values[0], values[1], values[2], values[3]);
        n.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        await _nutrients.SaveAsync(n);
        await _dialogs.ToastAsync("Nutrient saved");
        await Routes.BackAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_existing is null || !await _dialogs.ConfirmAsync("Delete nutrient?", "Past feedings keep their recorded doses.", "Delete", "Keep"))
            return;
        await _nutrients.DeleteAsync(_existing.Id);
        await Routes.BackAsync();
    }
}
