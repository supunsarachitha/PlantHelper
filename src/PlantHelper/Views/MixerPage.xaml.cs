using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class MixerPage : ContentPage
{
	private readonly MixerViewModel _viewModel;

	public MixerPage(MixerViewModel viewModel)
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
