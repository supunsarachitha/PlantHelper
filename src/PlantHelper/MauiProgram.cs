using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PlantHelper.Core.Data;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.Views;

namespace PlantHelper;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
				fonts.AddFont("Nunito-SemiBold.ttf", "NunitoSemiBold");
				fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
				fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtraBold");
				fonts.AddFont("MaterialDesignIcons.ttf", "MDI");
			});

		// Data — everything stays on the device, inside the app's private storage.
		builder.Services.AddSingleton(TimeProvider.System);
		builder.Services.AddSingleton(Preferences.Default);
		builder.Services.AddSingleton(_ => new AppDatabase(Path.Combine(FileSystem.AppDataDirectory, "planthelper.db3")));
		builder.Services.AddSingleton(_ => new PhotoStore(Path.Combine(FileSystem.AppDataDirectory, "photos")));
		builder.Services.AddSingleton<PlantRepository>();
		builder.Services.AddSingleton<CareLogRepository>();
		builder.Services.AddSingleton<PhotoRepository>();
		builder.Services.AddSingleton<NutrientRepository>();
		builder.Services.AddSingleton<CareScheduleService>();
		builder.Services.AddSingleton<SettingsService>();

		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<PlantsPage>();
		builder.Services.AddTransient<GalleryPage>();
		builder.Services.AddTransient<ToolsPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
