using System.IO.Compression;
using System.Text.Json;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;

namespace PlantHelper.Core.Services;

public sealed class BackupManifest
{
    public string Format { get; set; } = BackupService.FormatId;
    public int Version { get; set; } = BackupService.FormatVersion;
    public string? AppVersion { get; set; }
    public DateTime ExportedAtUtc { get; set; }
    public int Plants { get; set; }
    public int Logs { get; set; }
    public int Photos { get; set; }
    public int Nutrients { get; set; }
    public int PhotoFiles { get; set; }
}

public sealed class BackupData
{
    public List<Plant> Plants { get; set; } = [];
    public List<CareLog> Logs { get; set; } = [];
    public List<PlantPhoto> Photos { get; set; } = [];
    public List<Nutrient> Nutrients { get; set; } = [];
    public Dictionary<string, string>? Settings { get; set; }
}

public enum ImportMode
{
    /// <summary>Add new records; for records that exist on both sides keep the most recently edited one.</summary>
    Merge,

    /// <summary>Delete everything on this device and restore the backup exactly.</summary>
    Replace,
}

public sealed record BackupSummary(int Plants, int Logs, int Photos, int Nutrients, int PhotoFiles, DateTime ExportedAtUtc, string? AppVersion);

public sealed record ImportResult(
    int Added,
    int Updated,
    int Unchanged,
    int Skipped,
    int PhotoFilesRestored,
    IReadOnlyDictionary<string, string>? Settings);

public sealed class BackupException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Portable, private backups: a zip with manifest.json, data.json and photos/.
/// Imports are validated and staged before anything on the device is changed.
/// </summary>
public sealed class BackupService(AppDatabase database, PhotoStore photos, string stagingRoot)
{
    public const string FormatId = "PlantHelper.Backup";
    public const int FormatVersion = 1;
    private const string ManifestEntry = "manifest.json";
    private const string DataEntry = "data.json";
    private const string PhotoFolder = "photos/";

    /// <summary>Upper bound for data.json after decompression (zip-bomb guard).</summary>
    internal long MaxDataBytes { get; init; } = 64L * 1024 * 1024;

    /// <summary>Upper bound for a single photo after decompression.</summary>
    internal long MaxPhotoBytes { get; init; } = 40L * 1024 * 1024;

    public async Task<BackupSummary> ExportAsync(Stream destination, string? appVersion, IReadOnlyDictionary<string, string>? settings, CancellationToken ct = default)
    {
        var db = await database.GetConnectionAsync();
        var data = new BackupData
        {
            Plants = await db.Table<Plant>().ToListAsync(),
            Logs = await db.Table<CareLog>().ToListAsync(),
            Photos = await db.Table<PlantPhoto>().ToListAsync(),
            Nutrients = await db.Table<Nutrient>().ToListAsync(),
            Settings = settings?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        var files = 0;
        using (var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            var dataEntry = zip.CreateEntry(DataEntry, CompressionLevel.Optimal);
            await using (var s = await dataEntry.OpenAsync(ct))
                await JsonSerializer.SerializeAsync(s, data, CoreJsonContext.Default.BackupData, ct);

            foreach (var photo in data.Photos)
            {
                ct.ThrowIfCancellationRequested();
                if (!photos.Exists(photo.FileName))
                    continue;
                // JPEGs are already compressed; storing them is faster and just as small.
                var entry = zip.CreateEntry(PhotoFolder + photo.FileName, CompressionLevel.NoCompression);
                await using var target = await entry.OpenAsync(ct);
                await using var source = File.OpenRead(photos.GetPath(photo.FileName));
                await source.CopyToAsync(target, ct);
                files++;
            }

            var manifest = new BackupManifest
            {
                AppVersion = appVersion,
                ExportedAtUtc = DateTime.UtcNow,
                Plants = data.Plants.Count,
                Logs = data.Logs.Count,
                Photos = data.Photos.Count,
                Nutrients = data.Nutrients.Count,
                PhotoFiles = files,
            };
            var manifestEntry = zip.CreateEntry(ManifestEntry, CompressionLevel.Optimal);
            await using (var s = await manifestEntry.OpenAsync(ct))
                await JsonSerializer.SerializeAsync(s, manifest, CoreJsonContext.Default.BackupManifest, ct);

            return new BackupSummary(manifest.Plants, manifest.Logs, manifest.Photos, manifest.Nutrients, files, manifest.ExportedAtUtc, appVersion);
        }
    }

    /// <summary>Validates a backup and describes it, without changing anything.</summary>
    public async Task<BackupSummary> ReadSummaryAsync(Stream source, CancellationToken ct = default)
    {
        using var zip = OpenZip(source);
        var manifest = await ReadManifestAsync(zip, ct);
        var data = await ReadDataAsync(zip, ct);
        return new BackupSummary(data.Plants.Count, data.Logs.Count, data.Photos.Count, data.Nutrients.Count,
            zip.Entries.Count(e => e.FullName.StartsWith(PhotoFolder, StringComparison.Ordinal) && PhotoStore.IsValidFileName(e.Name)),
            manifest.ExportedAtUtc, manifest.AppVersion);
    }

    public async Task<ImportResult> ImportAsync(Stream source, ImportMode mode, CancellationToken ct = default)
    {
        using var zip = OpenZip(source);
        await ReadManifestAsync(zip, ct);
        var data = await ReadDataAsync(zip, ct);

        var db = await database.GetConnectionAsync();
        var knownPlantIds = data.Plants.Select(p => p.Id).ToHashSet();
        if (mode == ImportMode.Merge)
            knownPlantIds.UnionWith((await db.Table<Plant>().ToListAsync()).Select(p => p.Id));

        // 1. Validate and drop anything unusable.
        var skipped = 0;
        var plants = Keep(data.Plants, p => !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name), ref skipped);
        var nutrients = Keep(data.Nutrients, n => !string.IsNullOrWhiteSpace(n.Id) && !string.IsNullOrWhiteSpace(n.Name), ref skipped);
        var logs = Keep(data.Logs, l => !string.IsNullOrWhiteSpace(l.Id) && knownPlantIds.Contains(l.PlantId), ref skipped);
        var photoRows = Keep(data.Photos, p => !string.IsNullOrWhiteSpace(p.Id) && knownPlantIds.Contains(p.PlantId) && PhotoStore.IsValidFileName(p.FileName), ref skipped);

        // 2. Stage photo files (nothing on the device has changed yet).
        var staging = Path.Combine(stagingRoot, "import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var wanted = photoRows.Select(p => p.FileName).ToHashSet(StringComparer.Ordinal);
            var staged = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (!entry.FullName.StartsWith(PhotoFolder, StringComparison.Ordinal))
                    continue;
                var name = entry.FullName[PhotoFolder.Length..];
                if (!PhotoStore.IsValidFileName(name) || !wanted.Contains(name))
                    continue;
                if (entry.Length > MaxPhotoBytes)
                    throw new BackupException($"Photo {name} is too large.");
                await using var input = await entry.OpenAsync(ct);
                await using var output = File.Create(Path.Combine(staging, name));
                await CopyLimitedAsync(input, output, MaxPhotoBytes, ct);
                staged.Add(name);
            }

            // 3. Database changes in one transaction.
            int added = 0, updated = 0, unchanged = 0;
            await db.RunInTransactionAsync(c =>
            {
                if (mode == ImportMode.Replace)
                {
                    c.DeleteAll<CareLog>();
                    c.DeleteAll<PlantPhoto>();
                    c.DeleteAll<Plant>();
                    c.DeleteAll<Nutrient>();
                }
                Upsert(c, plants, p => p.Id, p => p.UpdatedAt, ref added, ref updated, ref unchanged);
                Upsert(c, nutrients, n => n.Id, n => n.UpdatedAt, ref added, ref updated, ref unchanged);
                Upsert(c, logs, l => l.Id, l => l.UpdatedAt, ref added, ref updated, ref unchanged);
                foreach (var photo in photoRows)
                {
                    var existing = c.Find<PlantPhoto>(photo.Id);
                    if (existing is null)
                    {
                        c.Insert(photo);
                        added++;
                    }
                    else if (photo.UpdatedAt > existing.UpdatedAt)
                    {
                        c.Update(photo);
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }
            });

            // 4. Files: replace drops every old photo; merge only adds files this device doesn't have yet.
            if (mode == ImportMode.Replace)
                photos.DeleteAll();
            var restored = 0;
            foreach (var name in staged)
            {
                if (photos.Exists(name))
                    continue;
                await using var stagedFile = File.OpenRead(Path.Combine(staging, name));
                await photos.WriteAsync(name, stagedFile, ct);
                restored++;
            }

            return new ImportResult(added, updated, unchanged, skipped, restored, data.Settings);
        }
        finally
        {
            try { Directory.Delete(staging, recursive: true); } catch (IOException) { }
        }
    }

    private static List<T> Keep<T>(IEnumerable<T> items, Func<T, bool> valid, ref int skipped)
    {
        var list = new List<T>();
        foreach (var item in items)
        {
            if (valid(item))
                list.Add(item);
            else
                skipped++;
        }
        return list;
    }

    private static void Upsert<T>(SQLite.SQLiteConnection c, IEnumerable<T> items, Func<T, string> id, Func<T, DateTime> updatedAt,
        ref int added, ref int updated, ref int unchanged) where T : new()
    {
        foreach (var item in items)
        {
            var existing = c.Find<T>(id(item));
            if (existing is null)
            {
                c.Insert(item);
                added++;
            }
            else if (updatedAt(item) > updatedAt(existing))
            {
                c.Update(item);
                updated++;
            }
            else
            {
                unchanged++;
            }
        }
    }

    private static ZipArchive OpenZip(Stream source)
    {
        try
        {
            return new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new BackupException("This file isn't a PlantHelper backup (not a valid zip).", ex);
        }
    }

    private async Task<BackupManifest> ReadManifestAsync(ZipArchive zip, CancellationToken ct)
    {
        var entry = zip.GetEntry(ManifestEntry) ?? throw new BackupException("This file isn't a PlantHelper backup (manifest missing).");
        var manifest = await ReadJsonAsync(entry, CoreJsonContext.Default.BackupManifest, ct);
        if (manifest.Format != FormatId)
            throw new BackupException("This file isn't a PlantHelper backup.");
        if (manifest.Version > FormatVersion)
            throw new BackupException("This backup was made by a newer version of PlantHelper. Please update the app first.");
        return manifest;
    }

    private async Task<BackupData> ReadDataAsync(ZipArchive zip, CancellationToken ct)
    {
        var entry = zip.GetEntry(DataEntry) ?? throw new BackupException("The backup is incomplete (data missing).");
        return await ReadJsonAsync(entry, CoreJsonContext.Default.BackupData, ct);
    }

    private async Task<T> ReadJsonAsync<T>(ZipArchiveEntry entry, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type, CancellationToken ct)
    {
        if (entry.Length > MaxDataBytes)
            throw new BackupException("The backup data is too large.");
        try
        {
            await using var raw = await entry.OpenAsync(ct);
            using var buffer = new MemoryStream();
            await CopyLimitedAsync(raw, buffer, MaxDataBytes, ct);
            buffer.Position = 0;
            return await JsonSerializer.DeserializeAsync(buffer, type, ct) ?? throw new BackupException("The backup is empty.");
        }
        catch (JsonException ex)
        {
            throw new BackupException("The backup is damaged and can't be read.", ex);
        }
        catch (InvalidDataException ex)
        {
            throw new BackupException("The backup is damaged and can't be read.", ex);
        }
    }

    /// <summary>Copies at most <paramref name="limit"/> bytes, whatever the zip header claims (zip-bomb guard).</summary>
    private static async Task CopyLimitedAsync(Stream input, Stream output, long limit, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > limit)
                throw new BackupException("A file in the backup is too large.");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }
}
