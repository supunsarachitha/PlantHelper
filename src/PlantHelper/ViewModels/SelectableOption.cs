using CommunityToolkit.Mvvm.ComponentModel;

namespace PlantHelper.ViewModels;

/// <summary>A colourful selectable chip (category, stage, log type…).</summary>
public partial class SelectableOption(int value, string label, string icon, Color color) : ObservableObject
{
    public int Value { get; } = value;
    public string Label { get; } = label;
    public string Icon { get; } = icon;
    public Color Color { get; } = color;
    public Color SoftColor { get; } = color.WithAlpha(0.14f);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(Foreground))]
    public partial bool IsSelected { get; set; }

    public Color Background => IsSelected ? Color : SoftColor;
    public Color Foreground => IsSelected ? Colors.White : Color;
}
