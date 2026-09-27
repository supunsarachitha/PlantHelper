using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class VpdPage : ContentPage
{
	public VpdPage(VpdViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
