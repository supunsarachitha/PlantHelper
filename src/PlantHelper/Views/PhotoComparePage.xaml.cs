using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PhotoComparePage : ContentPage
{
	private readonly PhotoCompareViewModel _viewModel;

	public PhotoComparePage(PhotoCompareViewModel viewModel)
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
