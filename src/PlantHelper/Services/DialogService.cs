using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace PlantHelper.Services;

public sealed class DialogService
{
    private static Page CurrentPage =>
        Shell.Current?.CurrentPage
        ?? Application.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException("No page is available to show a dialog.");

    public Task AlertAsync(string title, string message, string ok = "OK") =>
        CurrentPage.DisplayAlertAsync(title, message, ok);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel);

    public async Task<string?> ChooseAsync(string title, params string[] options)
    {
        var result = await CurrentPage.DisplayActionSheetAsync(title, "Cancel", null, options);
        return result is null or "Cancel" ? null : result;
    }

    /// <summary>Action sheet over items; duplicate labels get a " (2)" suffix so the choice is never ambiguous.</summary>
    public async Task<T?> ChooseItemAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> label) where T : class
    {
        var labels = new List<string>();
        foreach (var item in items)
        {
            var text = label(item);
            var candidate = text;
            for (var n = 2; labels.Contains(candidate); n++)
                candidate = $"{text} ({n})";
            labels.Add(candidate);
        }
        var choice = await ChooseAsync(title, [.. labels]);
        var index = choice is null ? -1 : labels.IndexOf(choice);
        return index >= 0 ? items[index] : null;
    }

    public Task<string?> PromptAsync(string title, string message, string initial = "", string placeholder = "", Keyboard? keyboard = null, int maxLength = 60) =>
        CurrentPage.DisplayPromptAsync(title, message, "OK", "Cancel", placeholder, maxLength, keyboard ?? Keyboard.Text, initial);

    public Task ToastAsync(string message) =>
        Toast.Make(message, ToastDuration.Short, 15).Show();
}
