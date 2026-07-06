using CommunityToolkit.Mvvm.ComponentModel;
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
        CurrentPage.DisplayAlertAsync(Loc["DialogDelete"], string.Format(Loc["DialogDeleteConfirm"], itemName), Loc["DialogYes"], Loc["DialogNo"]);

    protected Task<bool> ConfirmAsync(string title, string message) =>
        CurrentPage.DisplayAlertAsync(title, message, Loc["DialogYes"], Loc["DialogNo"]);

    protected Task ShowErrorAsync(Exception ex) =>
        CurrentPage.DisplayAlertAsync(Loc["ErrorTitle"], ex.Message, "OK");

    protected Task ShowInfoAsync(string title, string message) =>
        CurrentPage.DisplayAlertAsync(title, message, "OK");

    protected async Task<string?> ShowActionSheetAsync(string title, params string[] options)
    {
        var result = await CurrentPage.DisplayActionSheetAsync(title, Loc["BtnCancel"], null, options);
        return result == null || result == Loc["BtnCancel"] ? null : result;
    }
}
