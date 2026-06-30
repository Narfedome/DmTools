namespace DmToolsApp.Features.Library;

public partial class LibraryTrackPage : ContentPage
{
	public LibraryTrackPage(LibraryTrackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LibraryTrackViewModel vm)
            await vm.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop any playing audio when leaving the library page via the ViewModel (use DI)
        if (BindingContext is LibraryTrackViewModel vm)
        {
            vm.StopAudio();
        }
    }
}