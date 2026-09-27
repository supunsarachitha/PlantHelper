using CommunityToolkit.Mvvm.ComponentModel;
using PlantHelper.Core.Services;

namespace PlantHelper.ViewModels;

/// <summary>One nutrient in a feeding (ml per litre), with the total for the batch.</summary>
public partial class DoseItemViewModel(string? nutrientId, string name, double? mlPerLiter, Func<double?> batchMl) : ObservableObject
{
    public string? NutrientId { get; } = nutrientId;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial string MlPerLiterText { get; set; } = Numbers.ToInput(mlPerLiter, 2);

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    public double? MlPerLiter => Numbers.Parse(MlPerLiterText);

    partial void OnMlPerLiterTextChanged(string value) => UpdateTotal();

    public void UpdateTotal()
    {
        TotalText = MlPerLiter is { } dose && batchMl() is > 0 and var batch
            ? $"= {UnitFormatter.Num(GrowCalculators.NutrientDoseMl(dose, batch / 1000), 1)} ml in this batch"
            : string.Empty;
    }
}
