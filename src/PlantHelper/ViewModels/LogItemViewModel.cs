using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Theme;

namespace PlantHelper.ViewModels;

/// <summary>A journal entry row.</summary>
public sealed class LogItemViewModel(CareLog log, LogDescriber describer, string? plantName = null)
{
    public CareLog Log { get; } = log;
    public string Id => Log.Id;
    public string Title { get; } = describer.Title(log);
    public string Details { get; } = describer.Details(log);
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);
    public string? Notes { get; } = log.Type == LogType.Note || string.IsNullOrWhiteSpace(log.Notes) || log.Notes == "Quick log" ? null : log.Notes;
    public bool HasNotes => Notes is not null;
    public string Icon { get; } = Visuals.LogIcon(log.Type);
    public Color Color { get; } = Visuals.LogColor(log.Type);
    public Color Soft => Visuals.Soft(Color);
    public string? PlantName { get; } = plantName;
    public bool HasPlantName => PlantName is not null;
    public string DateText => Log.Date.ToString("ddd d MMM · HH:mm");
    public string DayText => Log.Date.ToString("d MMM");
}
