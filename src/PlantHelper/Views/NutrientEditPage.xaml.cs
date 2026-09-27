using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class NutrientEditPage : ContentPage
{
	public NutrientEditPage(NutrientEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
