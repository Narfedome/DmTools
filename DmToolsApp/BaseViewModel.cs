using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Services;

namespace DmToolsApp;

public abstract partial class BaseViewModel : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;
    public ThemeService Theme => ThemeService.Instance;

    public LoadingService Loading =>
        IPlatformApplication.Current!.Services.GetRequiredService<LoadingService>();

    protected Task<bool> ConfirmDeleteAsync(string itemName) =>
        Shell.Current.DisplayAlertAsync(Loc["DialogDelete"], string.Format(Loc["DialogDeleteConfirm"], itemName), Loc["DialogYes"], Loc["DialogNo"]);

    protected Task<bool> ConfirmAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, Loc["DialogYes"], Loc["DialogNo"]);

    protected Task ShowErrorAsync(Exception ex) =>
        Shell.Current.DisplayAlertAsync(Loc["ErrorTitle"], ex.Message, "OK");

    protected async Task<string?> ShowActionSheetAsync(string title, params string[] options)
    {
        var result = await Shell.Current.DisplayActionSheetAsync(title, Loc["BtnCancel"], null, options);
        return result == null || result == Loc["BtnCancel"] ? null : result;
    }
}
