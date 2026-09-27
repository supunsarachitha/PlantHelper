using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantHelper.Core.Data;
using PlantHelper.Core.Models;
using PlantHelper.Services;

namespace PlantHelper.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settings;
    private readonly IReminderScheduler _reminders;
    private readonly DataTransferService _transfer;
    private readonly PlantRepository _plants;
    private readonly CareLogRepository _logs;
    private readonly PhotoRepository _photos;
    private readonly PhotoStore _store;
    private readonly DialogService _dialogs;
    private bool _loading;

    public SettingsViewModel(SettingsService settings, IReminderScheduler reminders, DataTransferService transfer,
        PlantRepository plants, CareLogRepository logs, PhotoRepository photos, PhotoStore store, DialogService dialogs)
    {
        _settings = settings;
        _reminders = reminders;
        _transfer = transfer;
        _plants = plants;
        _logs = logs;
        _photos = photos;
        _store = store;
        _dialogs = dialogs;

        var green = Color.FromArgb("#0E9F6E");
        UnitSystems = Chips.Make(green, "Metric (cm, ml, g)", "Imperial (in, fl oz, oz)");
        Temperatures = Chips.Make(Color.FromArgb("#F97316"), "°C", "°F");
        Scales = Chips.Make(Color.FromArgb("#8B5CF6"), "EC", "PPM 500", "PPM 640", "PPM 700");
        Themes = Chips.Make(Color.FromArgb("#0891B2"), "System", "Light", "Dark");
    }

    public IReadOnlyList<SelectableOption> UnitSystems { get; }
    public IReadOnlyList<SelectableOption> Temperatures { get; }
    public IReadOnlyList<SelectableOption> Scales { get; }
    public IReadOnlyList<SelectableOption> Themes { get; }

    public bool RemindersSupported => _reminders.IsSupported;
    public string VersionText => $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    [ObservableProperty] public partial string? GrowerName { get; set; }
    [ObservableProperty] public partial bool RemindersEnabled { get; set; }
    [ObservableProperty] public partial TimeSpan? ReminderTime { get; set; }
    [ObservableProperty] public partial string StorageText { get; set; } = string.Empty;

#if DEBUG
    public bool IsDebug => true;
#else
    public bool IsDebug => false;
#endif

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            GrowerName = _settings.GrowerName;
            Select(UnitSystems, (int)_settings.UnitSystem);
            Select(Temperatures, (int)_settings.Temperature);
            Select(Scales, (int)_settings.NutrientScale);
            Select(Themes, (int)_settings.Theme);
            RemindersEnabled = _settings.RemindersEnabled;
            ReminderTime = _settings.ReminderTime;
            await RefreshStorageAsync();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RefreshStorageAsync()
    {
        var plantCount = (await _plants.GetAllAsync()).Count;
        var logCount = (await _logs.GetAllAsync()).Count;
        var photoCount = await _photos.CountAsync();
        long bytes = 0;
        try
        {
            bytes = Directory.EnumerateFiles(_store.PhotosDirectory).Sum(f => new FileInfo(f).Length);
            var db = Path.Combine(FileSystem.AppDataDirectory, "planthelper.db3");
            if (File.Exists(db))
                bytes += new FileInfo(db).Length;
        }
        catch (IOException) { }
        StorageText = $"{plantCount} plants · {logCount} entries · {photoCount} photos · {bytes / 1024d / 1024d:0.0} MB on this device";
    }

    partial void OnGrowerNameChanged(string? value)
    {
        if (!_loading)
            _settings.GrowerName = value ?? string.Empty;
    }

    partial void OnReminderTimeChanged(TimeSpan? value)
    {
        if (_loading || value is null)
            return;
        _settings.ReminderTime = value.Value;
        if (_settings.RemindersEnabled)
            _reminders.Reschedule(value.Value);
    }

    partial void OnRemindersEnabledChanged(bool value)
    {
        if (!_loading)
            _ = SafeRunAsync(() => ApplyRemindersAsync(value));
    }

    private async Task ApplyRemindersAsync(bool enable)
    {
        if (enable)
        {
            var ok = await _reminders.EnableAsync(_settings.ReminderTime);
            if (!ok)
            {
                _loading = true;
                RemindersEnabled = false;
                _loading = false;
                await _dialogs.AlertAsync("Notifications are off",
                    "Allow notifications for PlantHelper in your phone's settings to get daily care reminders.");
                return;
            }
            _settings.RemindersEnabled = true;
            await _dialogs.ToastAsync($"Daily reminder at {DateTime.Today.Add(_settings.ReminderTime):t} 🔔");
        }
        else
        {
            _reminders.Disable();
            _settings.RemindersEnabled = false;
        }
    }

    [RelayCommand]
    private void SelectUnitSystem(SelectableOption o)
    {
        Chips.Select(UnitSystems, o);
        _settings.UnitSystem = (UnitSystem)o.Value;
        // Switching the system usually means switching temperature too.
        _settings.Temperature = o.Value == 0 ? TemperatureUnit.Celsius : TemperatureUnit.Fahrenheit;
        Select(Temperatures, (int)_settings.Temperature);
    }

    [RelayCommand]
    private void SelectTemperature(SelectableOption o)
    {
        Chips.Select(Temperatures, o);
        _settings.Temperature = (TemperatureUnit)o.Value;
    }

    [RelayCommand]
    private void SelectScale(SelectableOption o)
    {
        Chips.Select(Scales, o);
        _settings.NutrientScale = (NutrientScale)o.Value;
    }

    [RelayCommand]
    private void SelectTheme(SelectableOption o)
    {
        Chips.Select(Themes, o);
        _settings.Theme = (ThemeChoice)o.Value;
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        IsBusy = true;
        try { await _transfer.ExportBackupAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        IsBusy = true;
        try
        {
            if (await _transfer.ImportBackupAsync())
                await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task ExportCsvAsync() => _transfer.ExportCsvAsync();

    [RelayCommand]
    private async Task DeleteAllAsync()
    {
        if (await _transfer.DeleteAllDataAsync())
            await RefreshStorageAsync();
    }

    [RelayCommand]
    private Task PrivacyAsync() => Routes.GoAsync(Routes.Privacy);

    [RelayCommand]
    private Task NutrientsAsync() => Routes.GoAsync(Routes.Nutrients);

    /// <summary>Debug builds only (the button is hidden in Release): fire the reminder pipeline immediately.</summary>
    [RelayCommand]
    private async Task TestReminderAsync()
    {
#if DEBUG && ANDROID
        await Platforms.Android.ReminderReceiver.NotifyAsync(Platform.AppContext);
        await _dialogs.ToastAsync("Reminder check ran");
#else
        await Task.CompletedTask;
#endif
    }

    private static void Select(IEnumerable<SelectableOption> options, int value)
    {
        foreach (var o in options)
            o.IsSelected = o.Value == value;
    }
}
