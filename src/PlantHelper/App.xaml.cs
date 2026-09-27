using PlantHelper.Services;

namespace PlantHelper;

public partial class App : Application
{
	public App(SettingsService settings, IReminderScheduler reminders)
	{
		InitializeComponent();
		settings.ApplyTheme();
		// Alarms can be cleared by app updates / force-stop; re-apply an enabled reminder on every start.
		if (settings.RemindersEnabled && reminders.IsSupported)
			reminders.Reschedule(settings.ReminderTime);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
