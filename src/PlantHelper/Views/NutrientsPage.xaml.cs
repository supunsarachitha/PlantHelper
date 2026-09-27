using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class NutrientsPage : ContentPage
{
	private readonly NutrientsViewModel _viewModel;

	public NutrientsPage(NutrientsViewModel viewModel)
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
