using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class LogEditPage : ContentPage
{
	public LogEditPage(LogEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
