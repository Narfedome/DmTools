namespace DmToolsApp.Features.Library;

public partial class LibraryTrackEditPage : ContentPage
{
	public LibraryTrackEditPage(LibraryTrackEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop any playing audio when leaving the library page via the ViewModel (use DI)
        if (BindingContext is LibraryTrackEditViewModel vm)
        {
            vm.StopAudio();
        }
    }
}