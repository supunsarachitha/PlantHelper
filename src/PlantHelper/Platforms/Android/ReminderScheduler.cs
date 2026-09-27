using Android.App;
using Android.Content;
using Android.Util;
using AndroidX.Core.App;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;
using PlantHelper.Services;

namespace PlantHelper.Platforms.Android;

/// <summary>
/// Daily reminder using a one-shot *windowed* alarm that re-arms itself each day.
/// Windowed alarms need no special permission (Google Play restricts SCHEDULE_EXACT_ALARM to
/// alarm/calendar apps) yet fire within ~30 minutes of the chosen time, unlike inexact repeating
/// alarms, which may drift by many hours.
/// </summary>
public sealed class AndroidReminderScheduler : IReminderScheduler
{
    public const string ChannelId = "care_reminders";
    private const int AlarmRequestCode = 1001;

    public bool IsSupported => true;

    public async Task<bool> EnableAsync(TimeSpan timeOfDay)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                return false;
        }
        Schedule(global::Android.App.Application.Context, timeOfDay);
        return true;
    }

    public void Reschedule(TimeSpan timeOfDay) => Schedule(global::Android.App.Application.Context, timeOfDay);

    public void Disable()
    {
        var context = global::Android.App.Application.Context;
        AlarmManagerFor(context).Cancel(ReminderIntent(context));
    }

    private const long DeliveryWindowMs = 30 * 60 * 1000;

    public static void Schedule(Context context, TimeSpan timeOfDay)
    {
        EnsureChannel(context);
        var next = DateTime.Today.Add(timeOfDay);
        if (next <= DateTime.Now.AddMinutes(1))
            next = next.AddDays(1);
        var triggerAt = new DateTimeOffset(next).ToUnixTimeMilliseconds();
        AlarmManagerFor(context).SetWindow(AlarmType.RtcWakeup, triggerAt, DeliveryWindowMs, ReminderIntent(context));
    }

    public static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.CreateNotificationChannel(new NotificationChannel(ChannelId, "Care reminders", NotificationImportance.Default)
        {
            Description = "A daily summary of plants that need watering or feeding",
        });
    }

    private static AlarmManager AlarmManagerFor(Context context) =>
        (AlarmManager)context.GetSystemService(Context.AlarmService)!;

    private static PendingIntent ReminderIntent(Context context) =>
        PendingIntent.GetBroadcast(context, AlarmRequestCode, new Intent(context, typeof(ReminderReceiver)),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
}

/// <summary>Fires once a day; posts a summary notification only when something is due.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    private const int NotificationId = 2001;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
            return;
        // One-shot alarm: arm tomorrow's reminder first.
        var settings = new SettingsService(Preferences.Default);
        if (settings.RemindersEnabled)
            AndroidReminderScheduler.Schedule(context, settings.ReminderTime);
        var pending = GoAsync();
        Task.Run(async () =>
        {
            try
            {
                await NotifyAsync(context);
            }
            catch (Exception ex)
            {
                Log.Warn("PlantHelper", $"Reminder failed: {ex}");
            }
            finally
            {
                pending?.Finish();
            }
        });
    }

    public static async Task NotifyAsync(Context context)
    {
        var schedule = IPlatformApplication.Current?.Services.GetService<CareScheduleService>();
        var manager = NotificationManagerCompat.From(context);
        if (schedule is null || manager is null || !manager.AreNotificationsEnabled())
            return;

        var tasks = await schedule.GetTasksAsync();
        if (tasks.Count == 0)
            return;

        var water = tasks.Where(t => t.Kind == CareTaskKind.Water).Select(t => t.Plant.Name).Distinct().ToList();
        var feed = tasks.Where(t => t.Kind == CareTaskKind.Feed).Select(t => t.Plant.Name).Distinct().ToList();
        var plantCount = tasks.Select(t => t.Plant.Id).Distinct().Count();
        var title = plantCount == 1 ? "1 plant needs care today 🌱" : $"{plantCount} plants need care today 🌱";
        var lines = new List<string>();
        if (water.Count > 0) lines.Add($"💧 Water: {string.Join(", ", water)}");
        if (feed.Count > 0) lines.Add($"🧪 Feed: {string.Join(", ", feed)}");
        var text = string.Join("\n", lines);

        var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var open = launch is null ? null : PendingIntent.GetActivity(context, 0, launch, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        AndroidReminderScheduler.EnsureChannel(context);
        var builder = new NotificationCompat.Builder(context, AndroidReminderScheduler.ChannelId);
        builder.SetSmallIcon(Resource.Drawable.ic_stat_sprout);
        builder.SetColor(unchecked((int)0xFF0E9F6E));
        builder.SetContentTitle(title);
        builder.SetContentText(text.Replace("\n", " · "));
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(text));
        builder.SetAutoCancel(true);
        builder.SetContentIntent(open);
        manager.Notify(NotificationId, builder.Build());
    }
}

/// <summary>Alarms are cleared on reboot; restore the reminder if the user enabled it.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent?.Action != Intent.ActionBootCompleted)
            return;
        var settings = new SettingsService(Preferences.Default);
        if (settings.RemindersEnabled)
            AndroidReminderScheduler.Schedule(context, settings.ReminderTime);
    }
}
