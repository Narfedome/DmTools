using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Campaigns;

public partial class SceneListPage : ContentPage
{
    private readonly SceneListViewModel _vm;

    public SceneListPage(SceneListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Cf. CampaignPage/CategoryListPage : une popup thémée qui se ferme redéclenche la navigation,
        // mais le ViewModel a déjà mis à jour Scenes localement (Create/Rename/Delete) — pas besoin de
        // tout recharger. Sans ce recours à OnNavigatedTo, ApplyQueryAttributes ne se redéclenche pas au
        // retour depuis SceneTracksPage, laissant la liste figée sur son état du premier chargement.
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        await _vm.ReloadAsync();
    }
}
