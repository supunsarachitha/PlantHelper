namespace PlantHelper.Core.Data;

/// <summary>Stores photo files in the app's private folder. Rows only keep the file name.</summary>
public sealed class PhotoStore
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic"];

    public PhotoStore(string photosDirectory)
    {
        PhotosDirectory = photosDirectory;
        Directory.CreateDirectory(photosDirectory);
    }

    public string PhotosDirectory { get; }

    public string GetPath(string fileName) => Path.Combine(PhotosDirectory, SafeName(fileName));

    public bool Exists(string fileName) => IsValidFileName(fileName) && File.Exists(GetPath(fileName));

    /// <summary>Copies an image stream into the store and returns the new file name.</summary>
    public async Task<string> SaveAsync(Stream source, string extension, CancellationToken ct = default)
    {
        extension = NormalizeExtension(extension);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await WriteAsync(fileName, source, ct).ConfigureAwait(false);
        return fileName;
    }

    /// <summary>Writes a file with a specific (validated) name — used when restoring backups.</summary>
    public async Task WriteAsync(string fileName, Stream source, CancellationToken ct = default)
    {
        var path = GetPath(fileName);
        var temp = path + ".tmp";
        await using (var target = File.Create(temp))
            await source.CopyToAsync(target, ct).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
    }

    public void Delete(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || !IsValidFileName(fileName))
            return;
        var path = GetPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public void DeleteAll()
    {
        if (!Directory.Exists(PhotosDirectory))
            return;
        foreach (var file in Directory.EnumerateFiles(PhotosDirectory))
            File.Delete(file);
    }

    /// <summary>Only plain "name.ext" with safe characters and an image extension is accepted (blocks path traversal).</summary>
    public static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 100)
            return false;
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            return false;
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            return false;
        return AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private static string SafeName(string fileName) =>
        IsValidFileName(fileName) ? fileName : throw new ArgumentException($"Invalid photo file name '{fileName}'.", nameof(fileName));

    private static string NormalizeExtension(string extension)
    {
        extension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension.Trim().ToLowerInvariant();
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return AllowedExtensions.Contains(extension) ? extension : ".jpg";
    }
}
