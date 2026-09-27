using System.Globalization;
using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

/// <summary>Turns a journal entry into a short, human readable line for lists.</summary>
public sealed class LogDescriber(UnitFormatter units)
{
    public string Title(CareLog log) => log.Type switch
    {
        LogType.Pest when !string.IsNullOrWhiteSpace(log.Title) => log.Title!,
        LogType.Training when !string.IsNullOrWhiteSpace(log.Title) => log.Title!,
        LogType.Stage when log.ToStage is { } to => $"Moved to {DisplayNames.Of(to)}",
        _ => DisplayNames.Verb(log.Type),
    };

    public string Details(CareLog log)
    {
        var parts = new List<string>();
        void Add(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text);
        }

        switch (log.Type)
        {
            case LogType.Water:
            case LogType.Feed:
                if (log.AmountMl is not null) Add(units.FormatVolume(log.AmountMl));
                if (log.Type == LogType.Feed)
                {
                    var doses = log.Doses;
                    if (doses.Count > 0)
                        Add(string.Join(", ", doses.Select(d => $"{d.Name} {UnitFormatter.Num(d.MlPerLiter, 2)} ml/L")));
                }
                if (log.Ph is not null) Add($"pH {F(log.Ph, 1)}");
                if (log.Ec is not null) Add(units.FormatStrength(log.Ec));
                if (log.WaterTempC is not null) Add($"water {units.FormatTemperature(log.WaterTempC)}");
                if (log.RunoffPh is not null) Add($"runoff pH {F(log.RunoffPh, 1)}");
                if (log.RunoffEc is not null) Add($"runoff {units.FormatStrength(log.RunoffEc)}");
                break;
            case LogType.Measure:
                if (log.HeightCm is not null) Add($"Height {units.FormatLength(log.HeightCm)}");
                if (log.WidthCm is not null) Add($"Width {units.FormatLength(log.WidthCm)}");
                if (log.NodeCount is not null) Add($"{log.NodeCount} nodes");
                if (log.LeafCount is not null) Add($"{log.LeafCount} leaves");
                break;
            case LogType.Environment:
                if (log.TempC is not null) Add(units.FormatTemperature(log.TempC));
                if (log.HumidityPct is not null) Add($"{F(log.HumidityPct, 0)}% RH");
                if (log.TempC is not null && log.HumidityPct is not null)
                    Add($"VPD {F(GrowCalculators.Vpd(log.TempC.Value, log.HumidityPct.Value), 2)} kPa");
                if (log.LightHours is not null) Add($"{F(log.LightHours, 1)} h light");
                if (log.Ppfd is not null) Add($"PPFD {F(log.Ppfd, 0)}");
                if (log.Co2Ppm is not null) Add($"CO₂ {F(log.Co2Ppm, 0)} ppm");
                if (log.SoilMoisturePct is not null) Add($"soil {F(log.SoilMoisturePct, 0)}%");
                break;
            case LogType.Pest:
                if (log.Severity is { } s) Add($"{DisplayNames.Of(s)} severity");
                if (!string.IsNullOrWhiteSpace(log.Treatment)) Add($"Treated: {log.Treatment}");
                break;
            case LogType.Transplant:
                if (log.ContainerLiters is not null) Add($"into {units.FormatContainer(log.ContainerLiters)}");
                if (log.Medium is { } m) Add(DisplayNames.Of(m));
                if (!string.IsNullOrWhiteSpace(log.Title)) Add(log.Title);
                break;
            case LogType.Harvest:
                if (log.WeightG is not null) Add(units.FormatWeight(log.WeightG));
                if (log.Quantity is not null) Add($"{log.Quantity} pcs");
                if (log.Rating is > 0) Add(new string('★', log.Rating.Value) + new string('☆', 5 - log.Rating.Value));
                break;
            case LogType.Stage:
                if (log.FromStage is { } from && log.ToStage is { } to)
                    Add($"{DisplayNames.Of(from)} → {DisplayNames.Of(to)}");
                break;
        }

        if (parts.Count == 0 || log.Type == LogType.Note)
            Add(log.Notes);
        return string.Join(" · ", parts);
    }

    private static string F(double? value, int decimals) =>
        value is null ? string.Empty : Math.Round(value.Value, decimals).ToString(decimals == 0 ? "0" : "0.0", CultureInfo.CurrentCulture);
}
