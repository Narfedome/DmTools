using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Campaigns;

public partial class CampaignPage : ContentPage
{
    private readonly CampaignViewModel _vm;

    public CampaignPage(CampaignViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Cf. LibraryTrackPage : une popup thémée qui se ferme redéclenche la navigation, mais le
        // ViewModel a déjà mis à jour Campaigns localement (Create/Rename/Delete) — pas besoin de tout recharger.
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        await _vm.InitializeAsync();
    }
}
