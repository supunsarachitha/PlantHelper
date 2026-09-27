using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlantHelper.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public bool IsNotBusy => !IsBusy;

    /// <summary>Runs UI work and reports failures to the user instead of crashing (used from async void handlers).</summary>
    public static async Task SafeRunAsync(Func<Task> action, string failure = "Something went wrong.")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Shell.Current?.CurrentPage is { } page)
                await page.DisplayAlertAsync("Oops", $"{failure}\n\n{ex.Message}", "OK");
        }
    }
}
