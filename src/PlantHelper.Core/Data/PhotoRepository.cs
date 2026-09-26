using PlantHelper.Core.Models;

namespace PlantHelper.Core.Data;

public sealed class PhotoRepository(AppDatabase database)
{
    /// <summary>Photos of one plant, oldest first (timeline order).</summary>
    public async Task<List<PlantPhoto>> GetForPlantAsync(string plantId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PlantPhoto>().Where(p => p.PlantId == plantId).OrderBy(p => p.TakenAt).ToListAsync();
    }

    /// <summary>All photos, newest first.</summary>
    public async Task<List<PlantPhoto>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PlantPhoto>().OrderByDescending(p => p.TakenAt).ToListAsync();
    }

    public async Task<PlantPhoto?> GetAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        return await db.FindAsync<PlantPhoto>(id);
    }

    public async Task<int> CountAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PlantPhoto>().CountAsync();
    }

    public async Task SaveAsync(PlantPhoto photo)
    {
        if (string.IsNullOrEmpty(photo.PlantId) || string.IsNullOrEmpty(photo.FileName))
            throw new ArgumentException("A photo needs a plant and a file.", nameof(photo));
        photo.UpdatedAt = DateTime.UtcNow;
        var db = await database.GetConnectionAsync();
        await db.InsertOrReplaceAsync(photo);
    }

    /// <summary>Deletes the row and clears it as cover photo. Returns the file name to remove from disk.</summary>
    public async Task<string?> DeleteAsync(string id)
    {
        var db = await database.GetConnectionAsync();
        string? file = null;
        await db.RunInTransactionAsync(c =>
        {
            var photo = c.Find<PlantPhoto>(id);
            if (photo is null)
                return;
            file = photo.FileName;
            c.Delete<PlantPhoto>(id);
            c.Execute("UPDATE Plant SET CoverPhotoId = NULL WHERE CoverPhotoId = ?", id);
        });
        return file;
    }
}
