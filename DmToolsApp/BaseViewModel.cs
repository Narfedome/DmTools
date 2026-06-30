using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Services;

namespace DmToolsApp;

public abstract partial class BaseViewModel : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;
    public ThemeService Theme => ThemeService.Instance;

    public LoadingService Loading =>
        IPlatformApplication.Current!.Services.GetRequiredService<LoadingService>();
}
