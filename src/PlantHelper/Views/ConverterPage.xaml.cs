using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class ConverterPage : ContentPage
{
	public ConverterPage(ConverterViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
