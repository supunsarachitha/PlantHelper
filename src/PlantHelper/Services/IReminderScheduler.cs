namespace PlantHelper.Services;

/// <summary>Daily care reminder (local notification, no server involved).</summary>
public interface IReminderScheduler
{
    bool IsSupported { get; }

    /// <summary>Asks for notification permission if needed and schedules the daily reminder. False if permission was denied.</summary>
    Task<bool> EnableAsync(TimeSpan timeOfDay);

    /// <summary>Re-applies an already-enabled reminder (app start) without prompting.</summary>
    void Reschedule(TimeSpan timeOfDay);

    void Disable();
}

public sealed class NoReminderScheduler : IReminderScheduler
{
    public bool IsSupported => false;
    public Task<bool> EnableAsync(TimeSpan timeOfDay) => Task.FromResult(false);
    public void Reschedule(TimeSpan timeOfDay) { }
    public void Disable() { }
}
