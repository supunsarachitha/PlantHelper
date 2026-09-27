using System.Windows.Input;
using PlantHelper.ViewModels;

namespace PlantHelper.Controls;

/// <summary>Wrapping row of colourful selectable chips bound to <see cref="SelectableOption"/> items.</summary>
public class ChipsView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<SelectableOption>), typeof(ChipsView), null,
            propertyChanged: (b, _, _) => ((ChipsView)b).Build());

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ChipsView));

    private readonly FlexLayout _flex = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };

    public ChipsView() => Content = _flex;

    public IEnumerable<SelectableOption>? ItemsSource { get => (IEnumerable<SelectableOption>?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private void Build()
    {
        _flex.Children.Clear();
        foreach (var option in ItemsSource ?? [])
        {
            var label = new Label { FontFamily = "NunitoBold", FontSize = 14, VerticalOptions = LayoutOptions.Center };
            label.SetBinding(Label.TextProperty, static (SelectableOption o) => o.Label);
            label.SetBinding(Label.TextColorProperty, static (SelectableOption o) => o.Foreground);

            var chip = new Border
            {
                BindingContext = option,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 0,
                Padding = new Thickness(14, 0),
                HeightRequest = 40,
                Margin = new Thickness(0, 0, 8, 8),
                Content = label,
            };
            chip.SetBinding(BackgroundColorProperty, static (SelectableOption o) => o.Background);
            SemanticProperties.SetDescription(chip, option.Label);
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (Command?.CanExecute(option) == true)
                    Command.Execute(option);
            };
            chip.GestureRecognizers.Add(tap);
            _flex.Children.Add(chip);
        }
    }
}
