using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class ToolsPage : ContentPage
{
	public ToolsPage(ToolsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
