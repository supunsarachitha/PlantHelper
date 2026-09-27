using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PlantHelper.Core.Data;
using PlantHelper.Core.Services;
using PlantHelper.Services;
using PlantHelper.ViewModels;
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
		builder.Services.AddSingleton(sp => new BackupService(
			sp.GetRequiredService<AppDatabase>(), sp.GetRequiredService<PhotoStore>(), FileSystem.CacheDirectory));
		builder.Services.AddSingleton(CommunityToolkit.Maui.Storage.FileSaver.Default);
		builder.Services.AddSingleton<SettingsService>();
		builder.Services.AddSingleton<DialogService>();
		builder.Services.AddSingleton<CareActionsService>();
		builder.Services.AddSingleton<PhotoService>();
		builder.Services.AddSingleton<DataTransferService>();
#if ANDROID
		builder.Services.AddSingleton<IReminderScheduler, Platforms.Android.AndroidReminderScheduler>();
#elif IOS || MACCATALYST
		builder.Services.AddSingleton<IReminderScheduler, Services.Apple.AppleReminderScheduler>();
#else
		builder.Services.AddSingleton<IReminderScheduler, NoReminderScheduler>();
#endif

		// View models
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<PlantsViewModel>();
		builder.Services.AddTransient<PlantEditViewModel>();
		builder.Services.AddTransient<PlantDetailViewModel>();
		builder.Services.AddTransient<LogEditViewModel>();
		builder.Services.AddTransient<NutrientsViewModel>();
		builder.Services.AddTransient<NutrientEditViewModel>();
		builder.Services.AddTransient<GalleryViewModel>();
		builder.Services.AddTransient<PhotoEditViewModel>();
		builder.Services.AddTransient<PhotoViewerViewModel>();
		builder.Services.AddTransient<PhotoCompareViewModel>();
		builder.Services.AddTransient<ToolsViewModel>();
		builder.Services.AddTransient<VpdViewModel>();
		builder.Services.AddTransient<DliViewModel>();
		builder.Services.AddTransient<PpmEcViewModel>();
		builder.Services.AddTransient<MixerViewModel>();
		builder.Services.AddTransient<ConverterViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<PlantsPage>();
		builder.Services.AddTransient<GalleryPage>();
		builder.Services.AddTransient<ToolsPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<PlantDetailPage>();
		builder.Services.AddTransient<PlantEditPage>();
		builder.Services.AddTransient<LogEditPage>();
		builder.Services.AddTransient<NutrientsPage>();
		builder.Services.AddTransient<NutrientEditPage>();
		builder.Services.AddTransient<PhotoEditPage>();
		builder.Services.AddTransient<PhotoViewerPage>();
		builder.Services.AddTransient<PhotoComparePage>();
		builder.Services.AddTransient<VpdPage>();
		builder.Services.AddTransient<DliPage>();
		builder.Services.AddTransient<PpmEcPage>();
		builder.Services.AddTransient<MixerPage>();
		builder.Services.AddTransient<ConverterPage>();

		RemoveNativeUnderlines();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	/// <summary>Our inputs sit inside rounded borders, so drop Android's default underline.</summary>
	private static void RemoveNativeUnderlines()
	{
#if ANDROID
		static void Clear(Android.Views.View view) =>
			view.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => Clear(h.PlatformView));
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => Clear(h.PlatformView));
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => Clear(h.PlatformView));
		Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => Clear(h.PlatformView));
		Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => Clear(h.PlatformView));
#endif
	}
}
