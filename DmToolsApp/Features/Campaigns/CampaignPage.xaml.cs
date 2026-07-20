using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Campaigns;

public partial class CampaignPage : ContentPage
{
    private readonly CampaignViewModel _vm;
    private bool _initialized;

    public CampaignPage(CampaignViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Une popup thémée qui se ferme redéclenche la navigation, mais le ViewModel a déjà mis à
        // jour Rows localement (Create/Rename/Delete) — pas besoin de tout recharger. De même, revenir
        // du Mixer (changement d'onglet) ne doit pas réinitialiser l'arborescence dépliée : seul le
        // tout premier affichage charge les campagnes.
        if (args.WasPreviousPageACommunityToolkitPopupPage() || _initialized)
            return;

        _initialized = true;
        await _vm.InitializeAsync();
    }
}
