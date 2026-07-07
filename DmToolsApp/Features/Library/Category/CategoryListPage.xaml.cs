using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Library;

public partial class CategoryListPage : ContentPage
{
    private readonly CategoryListViewModel _vm;

    public CategoryListPage(CategoryListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Cf. LibraryTrackPage/CampaignPage : une popup thémée qui se ferme redéclenche la navigation,
        // mais le ViewModel a déjà mis à jour CategoryNames localement (Rename/Delete).
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        await _vm.InitializeAsync();
    }
}
