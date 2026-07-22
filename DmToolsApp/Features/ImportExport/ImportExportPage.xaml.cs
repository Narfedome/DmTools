using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.ImportExport;

public partial class ImportExportPage : ContentPage
{
    public ImportExportPage(ImportExportViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        // Cf. SettingsPage.xaml.cs : plafonne/centre la colonne sur Desktop uniquement, en code plutôt
        // qu'en XAML (MaximumWidthRequest n'a pas de valeur sentinelle "pas de contrainte" du genre
        // WidthRequest=-1 - le vrai défaut, PositiveInfinity, ne doit pas être touché sur Android/iOS).
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Desktop)
        {
            ContentStack.MaximumWidthRequest = 560;
            ContentStack.HorizontalOptions = LayoutOptions.Center;
        }
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        if (BindingContext is ImportExportViewModel vm)
            await vm.InitializeAsync();
    }
}
