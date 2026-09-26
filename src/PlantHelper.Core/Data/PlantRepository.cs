using PlantHelper.Core.Models;

namespace PlantHelper.Core.Data;

public sealed class PlantRepository(AppDatabase database)
{
    public async Task<List<Plant>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Plant>().OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<List<Plant>> GetByStatusAsync(PlantStatus status)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Plant>().Where(p => p.Status == status).OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Plant?> GetAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        return await db.FindAsync<Plant>(id);
    }

    public async Task<int> CountAsync(PlantStatus status)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Plant>().Where(p => p.Status == status).CountAsync();
    }

    public async Task SaveAsync(Plant plant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plant.Name);
        plant.Name = plant.Name.Trim();
        plant.UpdatedAt = DateTime.UtcNow;
        var db = await database.GetConnectionAsync();
        await db.InsertOrReplaceAsync(plant);
    }

    /// <summary>Deletes the plant with all its logs and photo rows. Returns the photo file names to remove from disk.</summary>
    public async Task<List<string>> DeleteAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        var files = new List<string>();
        await db.RunInTransactionAsync(c =>
        {
            files.AddRange(c.Table<PlantPhoto>().Where(p => p.PlantId == id).ToList().Select(p => p.FileName));
            c.Execute("DELETE FROM CareLog WHERE PlantId = ?", id);
            c.Execute("DELETE FROM PlantPhoto WHERE PlantId = ?", id);
            c.Delete<Plant>(id);
        });
        return files;
    }
}
