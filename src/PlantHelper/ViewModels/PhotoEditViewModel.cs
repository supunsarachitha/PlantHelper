using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;

namespace PlantHelper.ViewModels;

/// <summary>Caption, height and date for a growth photo.</summary>
public partial class PhotoEditViewModel(
    PhotoRepository photos, PlantRepository plants, PhotoStore store, PhotoService photoService,
    SettingsService settings, DialogService dialogs, TimeProvider time) : BaseViewModel, IQueryAttributable
{
    private PlantPhoto? _photo;
    private Plant? _plant;

    [ObservableProperty] public partial ImageSource? Image { get; set; }
    [ObservableProperty] public partial string? Caption { get; set; }
    [ObservableProperty] public partial string? Height { get; set; }
    [ObservableProperty] public partial DateTime? Date { get; set; }
    [ObservableProperty] public partial bool IsCover { get; set; }
    [ObservableProperty] public partial bool IsNew { get; set; }
    [ObservableProperty] public partial string PlantName { get; set; } = string.Empty;

    public string LengthUnit => settings.Formatter.LengthUnit;
    public string HeightHint => settings.UnitSystem == UnitSystem.Metric ? "e.g. 45" : "e.g. 18";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        IsNew = query.TryGetValue("new", out var n) && n is "1" or "true";
        if (query.TryGetValue("id", out var id) && id is string photoId)
            _ = SafeRunAsync(() => LoadAsync(photoId));
    }

    private async Task LoadAsync(string id)
    {
        _photo = await photos.GetAsync(id);
        if (_photo is null)
            return;
        _plant = await plants.GetAsync(_photo.PlantId);
        Title = IsNew ? "New photo" : "Photo details";
        PlantName = _plant?.Name ?? string.Empty;
        Image = ImageSource.FromFile(store.GetPath(_photo.FileName));
        Caption = _photo.Caption;
        Height = Numbers.ToInput(settings.Formatter.LengthToDisplay(_photo.HeightCm), 1);
        Date = _photo.TakenAt.Date;
        IsCover = _plant?.CoverPhotoId == _photo.Id;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_photo is null)
            return;
        var height = Numbers.Parse(Height);
        if (!string.IsNullOrWhiteSpace(Height) && height is null or < 0)
        {
            await dialogs.AlertAsync("Height", "Enter the height as a number, or leave it empty.");
            return;
        }
        var date = (Date ?? _photo.TakenAt).Date;
        if (date > time.GetLocalNow().Date)
        {
            await dialogs.AlertAsync("Date", "The photo date can't be in the future.");
            return;
        }

        _photo.Caption = string.IsNullOrWhiteSpace(Caption) ? null : Caption.Trim();
        _photo.HeightCm = settings.Formatter.LengthFromDisplay(height);
        _photo.TakenAt = date + _photo.TakenAt.TimeOfDay;
        await photos.SaveAsync(_photo);

        if (_plant is not null)
        {
            var wasCover = _plant.CoverPhotoId == _photo.Id;
            if (IsCover != wasCover)
            {
                _plant.CoverPhotoId = IsCover ? _photo.Id : null;
                await plants.SaveAsync(_plant);
            }
        }
        await dialogs.ToastAsync("Photo saved 📸");
        await Routes.BackAsync();
    }

    [RelayCommand]
    private Task SkipAsync() => Routes.BackAsync();

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_photo is null || !await dialogs.ConfirmAsync("Delete photo?", "The photo will be removed from this device.", "Delete", "Keep"))
            return;
        await photoService.DeleteAsync(_photo);
        await Routes.BackAsync();
    }
}
