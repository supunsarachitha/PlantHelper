using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PpmEcPage : ContentPage
{
	public PpmEcPage(PpmEcViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
