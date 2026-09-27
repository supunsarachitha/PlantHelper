using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PhotoEditPage : ContentPage
{
	public PhotoEditPage(PhotoEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
