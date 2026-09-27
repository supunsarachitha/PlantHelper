#if IOS || MACCATALYST
using Foundation;
using UserNotifications;

namespace PlantHelper.Services.Apple;

/// <summary>iOS / Mac: a repeating daily local notification.</summary>
public sealed class AppleReminderScheduler : IReminderScheduler
{
    private const string RequestId = "daily-care-reminder";

    public bool IsSupported => true;

    public async Task<bool> EnableAsync(TimeSpan timeOfDay)
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        if (!granted)
            return false;
        await ScheduleAsync(timeOfDay);
        return true;
    }

    public void Reschedule(TimeSpan timeOfDay) => _ = ScheduleAsync(timeOfDay);

    public void Disable() => UNUserNotificationCenter.Current.RemovePendingNotificationRequests([RequestId]);

    private static Task ScheduleAsync(TimeSpan timeOfDay)
    {
        var content = new UNMutableNotificationContent
        {
            Title = "Time to check your plants 🌱",
            Body = "Open PlantHelper to see today's watering and feeding tasks.",
            Sound = UNNotificationSound.Default,
        };
        var trigger = UNCalendarNotificationTrigger.CreateTrigger(
            new NSDateComponents { Hour = timeOfDay.Hours, Minute = timeOfDay.Minutes }, repeats: true);
        return UNUserNotificationCenter.Current.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(RequestId, content, trigger));
    }
}
#endif
