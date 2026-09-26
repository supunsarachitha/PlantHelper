using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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
