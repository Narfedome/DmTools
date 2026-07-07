using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Components.Dialogs;
using DmToolsApp.Services;

namespace DmToolsApp;

public abstract partial class BaseViewModel : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;
    public ThemeService Theme => ThemeService.Instance;

    public LoadingService Loading =>
        IPlatformApplication.Current!.Services.GetRequiredService<LoadingService>();

    // Shell.Current est null tant que la fenêtre n'affiche pas encore l'AppShell (ex: pendant
    // l'onboarding, où la page active est l'OnboardingPage) : on cible la page réellement affichée.
    private static Page CurrentPage => Application.Current!.Windows[0].Page!;

    protected Task<bool> ConfirmDeleteAsync(string itemName) =>
        ConfirmAsync(Loc["DialogDelete"], string.Format(Loc["DialogDeleteConfirm"], itemName));

    protected async Task<bool> ConfirmAsync(string title, string message)
    {
        var popup = new ConfirmDialog(title, message, Loc["DialogYes"], Loc["DialogNo"]);
        var result = await CurrentPage.ShowPopupAsync<bool>(popup, PopupOptions.Empty, CancellationToken.None);
        return result.Result is true;
    }

    protected Task ShowErrorAsync(Exception ex)
    {
        var popup = new ConfirmDialog(Loc["ErrorTitle"], ex.Message, Loc["DialogOk"], null);
        return CurrentPage.ShowPopupAsync<bool>(popup, PopupOptions.Empty, CancellationToken.None);
    }

    protected Task ShowInfoAsync(string title, string message)
    {
        var popup = new ConfirmDialog(title, message, Loc["DialogOk"], null);
        return CurrentPage.ShowPopupAsync<bool>(popup, PopupOptions.Empty, CancellationToken.None);
    }

    protected async Task<string?> ShowPromptAsync(string title, string message, string placeholder = "", string initialValue = "")
    {
        var popup = new PromptDialog(title, message, placeholder, initialValue, Loc["DialogYes"], Loc["DialogNo"]);
        var result = await CurrentPage.ShowPopupAsync<string?>(popup, PopupOptions.Empty, CancellationToken.None);
        return result.Result;
    }

    protected async Task<string?> ShowActionSheetAsync(string title, params string[] options)
    {
        var popup = new ActionSheetDialog(title, options, Loc["BtnCancel"]);
        var result = await CurrentPage.ShowPopupAsync<string?>(popup, PopupOptions.Empty, CancellationToken.None);
        return result.Result;
    }
}
