using PlantHelper.Core.Data;
using PlantHelper.Core.Models;

namespace PlantHelper.Services;

/// <summary>Captures / picks growth photos and stores them privately inside the app.</summary>
public sealed class PhotoService(PhotoStore store, PhotoRepository photos, DialogService dialogs, TimeProvider time)
{
    // Big enough for a sharp full-screen view, small enough to keep backups light (~300-600 KB each).
    private static MediaPickerOptions Options(int limit = 1) => new()
    {
        Title = "Growth photo",
        MaximumWidth = 1920,
        MaximumHeight = 1920,
        CompressionQuality = 82,
        RotateImage = true,
        PreserveMetaData = false, // strip GPS / device EXIF from stored copies
        SelectionLimit = limit,
    };

    public const string TakePhoto = "📷 Take a photo";
    public const string ChooseFromGallery = "🖼 Choose from gallery";

    /// <summary>Asks camera vs gallery, then adds the photo(s). Returns what was added.</summary>
    public async Task<List<PlantPhoto>> AddInteractiveAsync(Plant plant)
    {
        var choice = await dialogs.ChooseAsync($"Add photo of {plant.Name}", TakePhoto, ChooseFromGallery);
        return choice switch
        {
            TakePhoto => await CaptureAsync(plant),
            ChooseFromGallery => await PickAsync(plant),
            _ => [],
        };
    }

    public async Task<List<PlantPhoto>> CaptureAsync(Plant plant)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await dialogs.AlertAsync("No camera", "This device can't take photos. Choose one from your gallery instead.");
            return [];
        }
        try
        {
            var file = await MediaPicker.Default.CapturePhotoAsync(Options());
            return file is null ? [] : [await SaveAsync(plant, file)];
        }
        catch (PermissionException)
        {
            if (await dialogs.ConfirmAsync("Camera access needed",
                    "PlantHelper uses the camera only to take growth photos, which stay on your device. Allow camera access in Settings?",
                    "Open settings", "Not now"))
                AppInfo.Current.ShowSettingsUI();
            return [];
        }
    }

    public async Task<List<PlantPhoto>> PickAsync(Plant plant)
    {
        // Uses the system photo picker on Android: no storage permission required.
        var files = await MediaPicker.Default.PickPhotosAsync(Options(limit: 10));
        var added = new List<PlantPhoto>();
        foreach (var file in files ?? [])
            added.Add(await SaveAsync(plant, file));
        return added;
    }

    private async Task<PlantPhoto> SaveAsync(Plant plant, FileResult file)
    {
        await using var stream = await file.OpenReadAsync();
        var fileName = await store.SaveAsync(stream, Path.GetExtension(file.FileName));
        var photo = new PlantPhoto
        {
            PlantId = plant.Id,
            FileName = fileName,
            TakenAt = time.GetLocalNow().DateTime,
            Stage = plant.Stage,
        };
        await photos.SaveAsync(photo);
        return photo;
    }

    public async Task DeleteAsync(PlantPhoto photo)
    {
        var file = await photos.DeleteAsync(photo.Id);
        store.Delete(file);
    }

    public Task ShareAsync(PlantPhoto photo, string title) =>
        Share.Default.RequestAsync(new ShareFileRequest(title, new ShareFile(store.GetPath(photo.FileName), "image/jpeg")));
}
