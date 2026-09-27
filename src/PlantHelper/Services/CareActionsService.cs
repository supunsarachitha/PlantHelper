using PlantHelper.Core.Data;
using PlantHelper.Core.Models;

namespace PlantHelper.Services;

/// <summary>One-tap care logging used by "Done" buttons (dashboard, plant page).</summary>
public sealed class CareActionsService(CareLogRepository logs, TimeProvider time)
{
    /// <summary>Logs a watering/feeding now, repeating the amount / pH / EC / doses of the previous entry of that type.</summary>
    public async Task<CareLog> QuickLogAsync(Plant plant, CareTaskKind kind)
    {
        var type = kind == CareTaskKind.Water ? LogType.Water : LogType.Feed;
        var previous = await logs.GetLastAsync(plant.Id, type);
        var log = new CareLog
        {
            PlantId = plant.Id,
            Type = type,
            Date = time.GetLocalNow().DateTime,
            AmountMl = previous?.AmountMl,
            Ph = previous?.Ph,
            Ec = previous?.Ec,
            DosesJson = type == LogType.Feed ? previous?.DosesJson : null,
            Notes = "Quick log",
        };
        await logs.SaveAsync(log);
        return log;
    }
}
