using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class DliPage : ContentPage
{
	public DliPage(DliViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
