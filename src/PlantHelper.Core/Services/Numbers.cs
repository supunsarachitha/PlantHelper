using System.Globalization;

namespace PlantHelper.Core.Services;

public static class Numbers
{
    /// <summary>Parses user input in the current culture, falling back to invariant ("1.5" and "1,5" both work).</summary>
    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        text = text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) && double.IsFinite(value))
            return value;
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value))
            return value;
        return null;
    }

    public static int? ParseInt(string? text) => Parse(text) is { } d ? (int)Math.Round(d) : null;

    /// <summary>Formats a value for an input box (no thousands separators, trimmed decimals).</summary>
    public static string ToInput(double? value, int decimals = 2) =>
        value is null ? string.Empty : Math.Round(value.Value, decimals).ToString("0.##", CultureInfo.CurrentCulture);
}
