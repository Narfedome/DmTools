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

        // Cf. SettingsPage.xaml.cs : plafonne/centre la liste sur Desktop uniquement, en code plutôt
        // qu'en XAML (MaximumWidthRequest n'a pas de valeur sentinelle "pas de contrainte" du genre
        // WidthRequest=-1 - le vrai défaut, PositiveInfinity, ne doit pas être touché sur Android/iOS).
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Desktop)
        {
            CategoryCollection.MaximumWidthRequest = 560;
            CategoryCollection.HorizontalOptions = LayoutOptions.Center;
        }
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
