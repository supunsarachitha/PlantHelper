using System.Globalization;

namespace PlantHelper.Converters;

/// <summary>Turns an accent colour into its translucent "soft" background version.</summary>
public sealed class SoftColorConverter : IValueConverter
{
    public float Alpha { get; set; } = 0.15f;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color c ? c.WithAlpha(Alpha) : Colors.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
