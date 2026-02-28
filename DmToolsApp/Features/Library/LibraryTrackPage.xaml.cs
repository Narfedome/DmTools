using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibraryTrackPage : ContentPage
{
	public LibraryTrackPage(LibraryViewModel vm)
    {
        InitializeComponent();
        vm.CurrentLibraryType = typeof(Track);
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LibraryViewModel vm)
            await vm.InitializeAsync();
    }
}