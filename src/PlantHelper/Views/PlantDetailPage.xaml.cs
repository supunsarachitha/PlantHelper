using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PlantDetailPage : ContentPage
{
	private readonly PlantDetailViewModel _viewModel;

	public PlantDetailPage(PlantDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	private void OnScrolled(object? sender, ScrolledEventArgs e)
	{
		// Hero is 320 tall: fade in before the hero title reaches the buttons.
		var t = Math.Clamp((e.ScrollY - 40) / 70.0, 0, 1);
		TopBarBackground.Opacity = t;
		TopBarTitle.Opacity = t;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await BaseViewModel.SafeRunAsync(_viewModel.LoadAsync);
	}
}
