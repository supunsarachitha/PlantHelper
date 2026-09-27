using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class HomePage : ContentPage
{
	private readonly HomeViewModel _viewModel;

	public HomePage(HomeViewModel viewModel)
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
