using PlantHelper.ViewModels;

namespace PlantHelper.Views;

public partial class PlantEditPage : ContentPage
{
	private readonly PlantEditViewModel _viewModel;

	public PlantEditPage(PlantEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	// Chips share one template, so route the tap to the right list.
	private void OnChipTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not BindableObject { BindingContext: SelectableOption option })
			return;
		if (_viewModel.Categories.Contains(option))
			_viewModel.SelectCategoryCommand.Execute(option);
		else
			_viewModel.SelectStageCommand.Execute(option);
	}
}
