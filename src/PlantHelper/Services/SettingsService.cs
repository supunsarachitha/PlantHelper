using System.Globalization;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Services;

public enum ThemeChoice
{
    System,
    Light,
    Dark,
}

/// <summary>User preferences, persisted with MAUI Preferences (on-device only).</summary>
public sealed class SettingsService
{
    private const string UnitSystemKey = "unit_system";
    private const string TemperatureKey = "temperature_unit";
    private const string ScaleKey = "nutrient_scale";
    private const string ThemeKey = "theme";
    private const string RemindersKey = "reminders_enabled";
    private const string ReminderTimeKey = "reminder_time_minutes";
    private const string GrowerNameKey = "grower_name";
    private const string OnboardedKey = "onboarding_done";

    private readonly IPreferences _prefs;
    private UnitFormatter? _formatter;

    public SettingsService(IPreferences preferences)
    {
        _prefs = preferences;
        if (!_prefs.ContainsKey(UnitSystemKey))
        {
            // First run: follow the device region (US etc. → imperial / °F).
            var metric = SafeIsMetric();
            _prefs.Set(UnitSystemKey, (int)(metric ? UnitSystem.Metric : UnitSystem.Imperial));
            _prefs.Set(TemperatureKey, (int)(metric ? TemperatureUnit.Celsius : TemperatureUnit.Fahrenheit));
        }
    }

    public event EventHandler? Changed;

    public UnitSystem UnitSystem
    {
        get => (UnitSystem)_prefs.Get(UnitSystemKey, (int)UnitSystem.Metric);
        set => SetInt(UnitSystemKey, (int)value);
    }

    public TemperatureUnit Temperature
    {
        get => (TemperatureUnit)_prefs.Get(TemperatureKey, (int)TemperatureUnit.Celsius);
        set => SetInt(TemperatureKey, (int)value);
    }

    public NutrientScale NutrientScale
    {
        get => (NutrientScale)_prefs.Get(ScaleKey, (int)NutrientScale.Ec);
        set => SetInt(ScaleKey, (int)value);
    }

    public ThemeChoice Theme
    {
        get => (ThemeChoice)_prefs.Get(ThemeKey, (int)ThemeChoice.System);
        set
        {
            SetInt(ThemeKey, (int)value);
            ApplyTheme();
        }
    }

    public bool RemindersEnabled
    {
        get => _prefs.Get(RemindersKey, false);
        set
        {
            _prefs.Set(RemindersKey, value);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Time of day for the daily care reminder.</summary>
    public TimeSpan ReminderTime
    {
        get => TimeSpan.FromMinutes(_prefs.Get(ReminderTimeKey, 8 * 60));
        set => SetInt(ReminderTimeKey, (int)value.TotalMinutes);
    }

    public string GrowerName
    {
        get => _prefs.Get(GrowerNameKey, string.Empty);
        set
        {
            _prefs.Set(GrowerNameKey, value?.Trim() ?? string.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool OnboardingDone
    {
        get => _prefs.Get(OnboardedKey, false);
        set => _prefs.Set(OnboardedKey, value);
    }

    public UnitPreferences Units => new(UnitSystem, Temperature, NutrientScale);

    public UnitFormatter Formatter => _formatter ??= new UnitFormatter(Units);

    public void ApplyTheme()
    {
        if (Application.Current is { } app)
        {
            app.UserAppTheme = Theme switch
            {
                ThemeChoice.Light => AppTheme.Light,
                ThemeChoice.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified,
            };
        }
    }

    /// <summary>Preferences included in backups.</summary>
    public Dictionary<string, string> Export() => new()
    {
        [UnitSystemKey] = ((int)UnitSystem).ToString(CultureInfo.InvariantCulture),
        [TemperatureKey] = ((int)Temperature).ToString(CultureInfo.InvariantCulture),
        [ScaleKey] = ((int)NutrientScale).ToString(CultureInfo.InvariantCulture),
        [ThemeKey] = ((int)Theme).ToString(CultureInfo.InvariantCulture),
        [GrowerNameKey] = GrowerName,
    };

    public void Import(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
            return;
        if (TryEnum<UnitSystem>(values, UnitSystemKey, out var us)) UnitSystem = us;
        if (TryEnum<TemperatureUnit>(values, TemperatureKey, out var tu)) Temperature = tu;
        if (TryEnum<NutrientScale>(values, ScaleKey, out var ns)) NutrientScale = ns;
        if (TryEnum<ThemeChoice>(values, ThemeKey, out var th)) Theme = th;
        if (values.TryGetValue(GrowerNameKey, out var name)) GrowerName = name;
    }

    private static bool TryEnum<T>(IReadOnlyDictionary<string, string> values, string key, out T result) where T : struct, Enum
    {
        if (values.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            && Enum.IsDefined(typeof(T), i))
        {
            result = (T)(object)i;
            return true;
        }
        result = default;
        return false;
    }

    private void SetInt(string key, int value)
    {
        _prefs.Set(key, value);
        _formatter = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static bool SafeIsMetric()
    {
        try { return RegionInfo.CurrentRegion.IsMetric; }
        catch (ArgumentException) { return true; }
    }
}
