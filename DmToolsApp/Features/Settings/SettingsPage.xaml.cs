using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Cf. LibraryTrackPage : les commandes Cleanup/DeleteAllTracks rafraîchissent déjà
        // StorageUsedText elles-mêmes après confirmation, pas besoin de le refaire ici.
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        if (BindingContext is SettingsViewModel vm)
            await vm.InitializeAsync();
    }
}
