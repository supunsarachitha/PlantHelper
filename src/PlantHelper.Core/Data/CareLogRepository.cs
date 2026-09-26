using PlantHelper.Core.Models;

namespace PlantHelper.Core.Data;

public sealed class CareLogRepository(AppDatabase database)
{
    public async Task<List<CareLog>> GetForPlantAsync(string plantId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>().Where(l => l.PlantId == plantId).OrderByDescending(l => l.Date).ToListAsync();
    }

    public async Task<List<CareLog>> GetRecentAsync(int take)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>().OrderByDescending(l => l.Date).Take(take).ToListAsync();
    }

    public async Task<List<CareLog>> GetSinceAsync(DateTime since)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>().Where(l => l.Date >= since).OrderByDescending(l => l.Date).ToListAsync();
    }

    public async Task<List<CareLog>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>().OrderBy(l => l.Date).ToListAsync();
    }

    public async Task<CareLog?> GetAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        return await db.FindAsync<CareLog>(id);
    }

    public async Task<CareLog?> GetLastAsync(string plantId, LogType type)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>()
            .Where(l => l.PlantId == plantId && l.Type == type)
            .OrderByDescending(l => l.Date)
            .FirstOrDefaultAsync();
    }

    /// <summary>Latest log date per plant and type — used to compute watering/feeding schedules in one query.</summary>
    public async Task<Dictionary<(string PlantId, LogType Type), DateTime>> GetLastDatesAsync()
    {
        var db = await database.GetConnectionAsync();
        var rows = await db.QueryAsync<LastDateRow>(
            "SELECT PlantId, Type, MAX(Date) AS Date FROM CareLog GROUP BY PlantId, Type");
        return rows.ToDictionary(r => (r.PlantId, r.Type), r => r.Date);
    }

    public async Task SaveAsync(CareLog log)
    {
        if (string.IsNullOrEmpty(log.PlantId))
            throw new ArgumentException("A log must belong to a plant.", nameof(log));
        log.UpdatedAt = DateTime.UtcNow;
        var db = await database.GetConnectionAsync();
        await db.InsertOrReplaceAsync(log);
    }

    public async Task DeleteAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        await db.DeleteAsync<CareLog>(id);
    }

    public async Task<int> CountSinceAsync(DateTime since)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<CareLog>().Where(l => l.Date >= since).CountAsync();
    }

    private sealed class LastDateRow
    {
        public string PlantId { get; set; } = string.Empty;
        public LogType Type { get; set; }
        public DateTime Date { get; set; }
    }
}
