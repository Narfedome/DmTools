using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.ImportExport;

public partial class ImportExportPage : ContentPage
{
    public ImportExportPage(ImportExportViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
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
