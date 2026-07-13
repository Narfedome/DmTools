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

    // Mis en cache par instance de ViewModel (LoadingService est enregistré en Transient) : chaque page
    // a son propre indicateur de chargement, pour qu'un chargement long sur un onglet n'affiche pas un
    // spinner sur un autre onglet consulté entre-temps (LoadingService était auparavant un singleton
    // partagé par toute l'appli).
    private LoadingService? _loading;
    public LoadingService Loading =>
        _loading ??= IPlatformApplication.Current!.Services.GetRequiredService<LoadingService>();

    // Shell.Current est null tant que la fenêtre n'affiche pas encore l'AppShell (ex: pendant
    // l'onboarding, où la page active est l'OnboardingPage) : on cible la page réellement affichée.
    private static Page CurrentPage => Application.Current!.Windows[0].Page!;

    protected Task<bool> ConfirmDeleteAsync(string itemName) =>
        ConfirmAsync(Loc["DialogDelete"], string.Format(Loc["DialogDeleteConfirm"], itemName));

    protected async Task<bool> ConfirmAsync(string title, string message)
    {
        var popup = new ConfirmDialog(title, message, Loc["DialogYes"], Loc["DialogNo"]);
        var result = await CurrentPage.ShowPopupAsync<bool>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
        return result.Result is true;
    }

    protected Task ShowErrorAsync(Exception ex)
    {
        var popup = new ConfirmDialog(Loc["ErrorTitle"], ex.Message, Loc["DialogOk"], null);
        return CurrentPage.ShowPopupAsync<bool>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
    }

    protected Task ShowInfoAsync(string title, string message)
    {
        var popup = new ConfirmDialog(title, message, Loc["DialogOk"], null);
        return CurrentPage.ShowPopupAsync<bool>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
    }

    protected async Task<string?> ShowPromptAsync(string title, string message, string placeholder = "", string initialValue = "")
    {
        var popup = new PromptDialog(title, message, placeholder, initialValue, Loc["DialogYes"], Loc["DialogNo"]);
        var result = await CurrentPage.ShowPopupAsync<string?>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
        return result.Result;
    }

    /// <summary>
    /// Affiche une popup typée quelconque (dialogs "métier" comme ChannelSettingsDialog) et retourne
    /// son résultat — default(TResult) si elle est fermée en tapant à côté.
    /// </summary>
    protected async Task<TResult?> ShowDialogAsync<TResult>(Popup<TResult> popup)
    {
        var result = await CurrentPage.ShowPopupAsync<TResult>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
        return result.Result;
    }

    protected async Task<string?> ShowActionSheetAsync(string title, params string[] options)
    {
        var popup = new ActionSheetDialog(title, options, Loc["BtnCancel"]);
        var result = await CurrentPage.ShowPopupAsync<string?>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
        return result.Result;
    }
}
