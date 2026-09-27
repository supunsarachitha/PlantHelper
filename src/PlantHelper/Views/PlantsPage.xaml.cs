using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PlantsPage : ContentPage
{
	private readonly PlantsViewModel _viewModel;

	public PlantsPage(PlantsViewModel viewModel)
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
