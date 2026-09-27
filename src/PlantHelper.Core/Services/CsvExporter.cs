using System.Globalization;
using System.Text;
using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

/// <summary>Journal export for spreadsheets. Values are metric with units in the header.</summary>
public static class CsvExporter
{
    private static readonly string[] Header =
    [
        "Date", "Time", "Plant", "Variety", "Type", "Summary", "Amount (ml)", "pH", "EC (mS/cm)", "Water temp (°C)",
        "Runoff pH", "Runoff EC", "Nutrients (ml/L)", "Height (cm)", "Width (cm)", "Nodes", "Leaves", "Air temp (°C)",
        "Humidity (%)", "Light (h)", "PPFD", "CO2 (ppm)", "Soil moisture (%)", "Issue / method", "Severity", "Treatment",
        "Container (L)", "Medium", "Harvest weight (g)", "Quantity", "Rating", "Stage from", "Stage to", "Notes",
    ];

    public static async Task WriteLogsAsync(TextWriter writer, IEnumerable<Plant> plants, IEnumerable<CareLog> logs, CancellationToken ct = default)
    {
        var byId = plants.ToDictionary(p => p.Id);
        var describer = new LogDescriber(new UnitFormatter(new UnitPreferences()));
        await writer.WriteLineAsync(string.Join(",", Header.Select(Text)));
        foreach (var log in logs.OrderBy(l => l.Date))
        {
            ct.ThrowIfCancellationRequested();
            byId.TryGetValue(log.PlantId, out var plant);
            string[] row =
            [
                log.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                log.Date.ToString("HH:mm", CultureInfo.InvariantCulture),
                Text(plant?.Name),
                Text(plant?.Variety),
                Text(DisplayNames.Of(log.Type)),
                Text(describer.Details(log)),
                N(log.AmountMl), N(log.Ph), N(log.Ec), N(log.WaterTempC), N(log.RunoffPh), N(log.RunoffEc),
                Text(string.Join("; ", log.Doses.Select(d => $"{d.Name} {d.MlPerLiter.ToString(CultureInfo.InvariantCulture)}"))),
                N(log.HeightCm), N(log.WidthCm), N(log.NodeCount), N(log.LeafCount), N(log.TempC), N(log.HumidityPct),
                N(log.LightHours), N(log.Ppfd), N(log.Co2Ppm), N(log.SoilMoisturePct),
                Text(log.Title), Text(log.Severity?.ToString()), Text(log.Treatment),
                N(log.ContainerLiters), Text(log.Medium is { } m ? DisplayNames.Of(m) : null),
                N(log.WeightG), N(log.Quantity), N(log.Rating),
                Text(log.FromStage?.ToString()), Text(log.ToStage?.ToString()), Text(log.Notes),
            ];
            await writer.WriteLineAsync(string.Join(",", row));
        }
    }

    private static string N(double? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string N(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>RFC 4180 quoting, plus a leading apostrophe on text that a spreadsheet would run as a formula.</summary>
    internal static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        var needsQuotes = value.IndexOfAny([',', '"', '\n', '\r']) >= 0 || value != value.Trim();
        return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    public static Encoding FileEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true); // BOM so Excel detects UTF-8
}
