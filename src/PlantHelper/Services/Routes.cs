using PlantHelper.Views;

namespace PlantHelper.Services;

public static class Routes
{
    public const string PlantDetail = "plant";
    public const string PlantEdit = "plantedit";
    public const string LogEdit = "logedit";
    public const string PhotoViewer = "photo";
    public const string PhotoCompare = "compare";
    public const string PhotoEdit = "photoedit";
    public const string Nutrients = "nutrients";
    public const string NutrientEdit = "nutrientedit";
    public const string Vpd = "vpd";
    public const string Dli = "dli";
    public const string PpmEc = "ppmec";
    public const string Dilution = "dilution";
    public const string Converter = "converter";
    public const string Privacy = "privacy";

    public static void Register()
    {
        Routing.RegisterRoute(PlantDetail, typeof(PlantDetailPage));
        Routing.RegisterRoute(PlantEdit, typeof(PlantEditPage));
        Routing.RegisterRoute(LogEdit, typeof(LogEditPage));
        Routing.RegisterRoute(Nutrients, typeof(NutrientsPage));
        Routing.RegisterRoute(NutrientEdit, typeof(NutrientEditPage));
        Routing.RegisterRoute(PhotoViewer, typeof(PhotoViewerPage));
        Routing.RegisterRoute(PhotoEdit, typeof(PhotoEditPage));
        Routing.RegisterRoute(PhotoCompare, typeof(PhotoComparePage));
    }

    public static Task GoAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, parameters);

    public static Task BackAsync() => Shell.Current.GoToAsync("..");
}
