using CommunityToolkit.Maui.Storage;
using PlantHelper.Core.Data;
using PlantHelper.Core.Services;

namespace PlantHelper.Services;

/// <summary>User-facing backup, restore, CSV export and data deletion.</summary>
public sealed class DataTransferService(
    BackupService backup, AppDatabase database, PhotoStore photoStore, PlantRepository plants, CareLogRepository logs,
    SettingsService settings, DialogService dialogs, IFileSaver fileSaver)
{
    private const string SaveOption = "💾 Save to device / SD / Drive";
    private const string ShareOption = "📤 Share (email, messaging…)";

    private static readonly FilePickerFileType ZipFiles = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["application/zip", "application/x-zip-compressed", "application/octet-stream"],
        [DevicePlatform.iOS] = ["public.zip-archive"],
        [DevicePlatform.MacCatalyst] = ["public.zip-archive"],
        [DevicePlatform.WinUI] = [".zip"],
    });

    public async Task ExportBackupAsync()
    {
        var name = $"PlantHelper-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.zip";
        var path = PrepareTempFile(name);
        BackupSummary summary;
        await using (var file = File.Create(path))
            summary = await backup.ExportAsync(file, AppInfo.Current.VersionString, settings.Export());

        var sizeMb = new FileInfo(path).Length / 1024d / 1024d;
        var choice = await dialogs.ChooseAsync(
            $"Backup ready · {summary.Plants} plants, {summary.Logs} entries, {summary.PhotoFiles} photos ({sizeMb:0.#} MB)",
            SaveOption, ShareOption);
        await DeliverAsync(path, name, "application/zip", choice, "Backup saved ✓");
    }

    public async Task ExportCsvAsync()
    {
        var name = $"PlantHelper-journal-{DateTime.Now:yyyy-MM-dd}.csv";
        var path = PrepareTempFile(name);
        await using (var writer = new StreamWriter(path, false, CsvExporter.FileEncoding))
            await CsvExporter.WriteLogsAsync(writer, await plants.GetAllAsync(), await logs.GetAllAsync());
        var choice = await dialogs.ChooseAsync("Journal exported as CSV (opens in Excel / Sheets)", SaveOption, ShareOption);
        await DeliverAsync(path, name, "text/csv", choice, "Journal saved ✓");
    }

    /// <summary>Returns true when data changed.</summary>
    public async Task<bool> ImportBackupAsync()
    {
        var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a PlantHelper backup (.zip)", FileTypes = ZipFiles });
        if (picked is null)
            return false;

        // Copy to a seekable private temp file first (content URIs are forward-only).
        var path = PrepareTempFile("import.zip");
        await using (var input = await picked.OpenReadAsync())
        await using (var output = File.Create(path))
            await input.CopyToAsync(output);

        try
        {
            BackupSummary summary;
            await using (var file = File.OpenRead(path))
                summary = await backup.ReadSummaryAsync(file);

            var what = $"Made {summary.ExportedAtUtc.ToLocalTime():d MMM yyyy, HH:mm}\n\n" +
                       $"🌱 {summary.Plants} plants\n📓 {summary.Logs} journal entries\n📷 {summary.PhotoFiles} photos\n🧪 {summary.Nutrients} nutrients";
            if (!await dialogs.ConfirmAsync("Restore this backup?", what, "Continue", "Cancel"))
                return false;
            const string merge = "Merge with my data (keeps newest of each)";
            const string replace = "Replace all my data";
            var choice = await dialogs.ChooseAsync("How should it be restored?", merge, replace);
            if (choice is null)
                return false;
            var mode = choice == replace ? ImportMode.Replace : ImportMode.Merge;
            if (mode == ImportMode.Replace && !await dialogs.ConfirmAsync(
                    "Replace everything?",
                    "All plants, journal entries and photos currently on this device will be deleted and replaced by the backup. This can't be undone.",
                    "Replace", "Cancel"))
                return false;

            ImportResult result;
            await using (var file = File.OpenRead(path))
                result = await backup.ImportAsync(file, mode);
            if (mode == ImportMode.Replace)
                settings.Import(result.Settings);

            var details = $"{result.Added} added · {result.Updated} updated · {result.Unchanged} already up to date · {result.PhotoFilesRestored} photos restored";
            if (result.Skipped > 0)
                details += $"\n{result.Skipped} damaged or incomplete records were skipped.";
            await dialogs.AlertAsync("Backup restored 🌱", details);
            return true;
        }
        catch (BackupException ex)
        {
            await dialogs.AlertAsync("Can't restore this file", ex.Message);
            return false;
        }
        finally
        {
            TryDelete(path);
        }
    }

    /// <summary>Returns true when everything was deleted.</summary>
    public async Task<bool> DeleteAllDataAsync()
    {
        if (!await dialogs.ConfirmAsync("Delete all data?",
                "Every plant, journal entry, photo and nutrient on this device will be permanently deleted. Export a backup first if you might need it.",
                "Continue", "Cancel"))
            return false;
        var typed = await dialogs.PromptAsync("Are you sure?", "Type DELETE to confirm.", placeholder: "DELETE", maxLength: 10);
        if (!string.Equals(typed?.Trim(), "DELETE", StringComparison.Ordinal))
        {
            if (typed is not null)
                await dialogs.ToastAsync("Nothing was deleted");
            return false;
        }
        await database.DeleteAllAsync();
        photoStore.DeleteAll();
        await dialogs.ToastAsync("All data deleted");
        return true;
    }

    private async Task DeliverAsync(string path, string name, string mime, string? choice, string savedMessage)
    {
        if (choice == SaveOption)
        {
            await using var stream = File.OpenRead(path);
            var result = await fileSaver.SaveAsync(name, stream, CancellationToken.None);
            if (result.IsSuccessful)
                await dialogs.ToastAsync(savedMessage);
            else if (result.Exception is not null and not OperationCanceledException && !IsCancel(result.Exception))
                await dialogs.AlertAsync("Couldn't save", result.Exception.Message);
            TryDelete(path);
        }
        else if (choice == ShareOption)
        {
            // The share target reads the file later, so it stays in the cache until the next export.
            await Share.Default.RequestAsync(new ShareFileRequest(name, new ShareFile(path, mime)));
        }
        else
        {
            TryDelete(path);
        }
    }

    private static bool IsCancel(Exception ex) => ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase);

    private static string PrepareTempFile(string name)
    {
        var dir = Path.Combine(FileSystem.CacheDirectory, "exports");
        if (Directory.Exists(dir))
        {
            foreach (var old in Directory.EnumerateFiles(dir))
                TryDelete(old);
        }
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
