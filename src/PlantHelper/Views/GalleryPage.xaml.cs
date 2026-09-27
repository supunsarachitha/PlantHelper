using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class GalleryPage : ContentPage
{
	private readonly GalleryViewModel _viewModel;

	public GalleryPage(GalleryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await BaseViewModel.SafeRunAsync(_viewModel.LoadAsync);
	}
}
