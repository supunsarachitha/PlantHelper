namespace PlantHelper.Controls;

/// <summary>Label + rounded input + optional unit suffix. Keeps long forms compact.</summary>
public class FormEntry : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FormEntry), string.Empty);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FormEntry), null, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormEntry), string.Empty);

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(nameof(Unit), typeof(string), typeof(FormEntry), string.Empty);

    public static readonly BindableProperty KeyboardProperty =
        BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(FormEntry), Keyboard.Numeric);

    public static readonly BindableProperty MaxLengthProperty =
        BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(FormEntry), 20);

    public FormEntry()
    {
        var entry = new Entry { ClearButtonVisibility = ClearButtonVisibility.Never };
        entry.SetBinding(Entry.TextProperty, static (FormEntry f) => f.Text, BindingMode.TwoWay, source: this);
        entry.SetBinding(Entry.PlaceholderProperty, static (FormEntry f) => f.Placeholder, source: this);
        entry.SetBinding(Entry.KeyboardProperty, static (FormEntry f) => f.Keyboard, source: this);
        entry.SetBinding(Entry.MaxLengthProperty, static (FormEntry f) => f.MaxLength, source: this);
        entry.SetBinding(SemanticProperties.DescriptionProperty, static (FormEntry f) => f.Label, source: this);

        var unit = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            FontFamily = "NunitoBold",
            FontSize = 13,
            Margin = new Thickness(4, 0, 0, 0),
        };
        unit.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, static (FormEntry f) => f.Unit, source: this);
        unit.SetAppThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, Color.FromArgb("#5B6F63"), Color.FromArgb("#9BB2A5"));

        var grid = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)] };
        grid.Add(entry, 0);
        grid.Add(unit, 1);

        var field = new Border { Content = grid };
        if (Application.Current?.Resources.TryGetValue("FormField", out var fieldStyle) == true)
            field.Style = (Style)fieldStyle;

        var title = new Label();
        if (Application.Current?.Resources.TryGetValue("FieldLabel", out var labelStyle) == true)
            title.Style = (Style)labelStyle;
        title.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, static (FormEntry f) => f.Label, source: this);
        // Tapping the caption focuses the input, like a native form.
        var focus = new TapGestureRecognizer();
        focus.Tapped += (_, _) => entry.Focus();
        title.GestureRecognizers.Add(focus);

        Content = new VerticalStackLayout { Children = { title, field } };
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
}
