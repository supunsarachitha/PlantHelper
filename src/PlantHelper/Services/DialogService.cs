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

    public async Task<string?> ChooseAsync(string title, string destructive, params string[] options)
    {
        var result = await CurrentPage.DisplayActionSheetAsync(title, "Cancel", destructive, options);
        return result is null or "Cancel" ? null : result;
    }

    public Task<string?> PromptAsync(string title, string message, string initial = "", string placeholder = "", Keyboard? keyboard = null, int maxLength = 60) =>
        CurrentPage.DisplayPromptAsync(title, message, "OK", "Cancel", placeholder, maxLength, keyboard ?? Keyboard.Text, initial);

    public Task ToastAsync(string message) =>
        Toast.Make(message, ToastDuration.Short, 15).Show();
}
