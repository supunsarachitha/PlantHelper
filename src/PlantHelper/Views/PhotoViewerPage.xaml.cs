using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PhotoViewerPage : ContentPage
{
	private readonly PhotoViewerViewModel _viewModel;

	public PhotoViewerPage(PhotoViewerViewModel viewModel)
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
