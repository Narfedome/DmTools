using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Campaigns;

public partial class SceneTracksPage : ContentPage
{
    private readonly SceneTracksViewModel _vm;

    public SceneTracksPage(SceneTracksViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Cf. SessionListPage/SceneListPage : le rechargement passe par OnNavigatedTo (avec garde
        // popup) plutôt que par ApplyQueryAttributes, pour ne pas écraser la liste au retour d'une
        // popup avec un état de la BD antérieur à la sauvegarde en cours.
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        await _vm.ReloadAsync();
    }
}
