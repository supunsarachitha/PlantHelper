using PlantHelper.Core.Models;

namespace PlantHelper.Core.Data;

public sealed class NutrientRepository(AppDatabase database)
{
    public async Task<List<Nutrient>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Nutrient>().OrderBy(n => n.Name).ToListAsync();
    }

    public async Task<Nutrient?> GetAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        return await db.FindAsync<Nutrient>(id);
    }

    public async Task SaveAsync(Nutrient nutrient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nutrient.Name);
        nutrient.Name = nutrient.Name.Trim();
        nutrient.UpdatedAt = DateTime.UtcNow;
        var db = await database.GetConnectionAsync();
        await db.InsertOrReplaceAsync(nutrient);
    }

    public async Task DeleteAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        await db.DeleteAsync<Nutrient>(id);
    }
}
