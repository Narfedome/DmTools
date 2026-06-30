namespace DmToolsApp.Features.Library;

public partial class LibraryTrackSelectorPage : ContentPage
{
    private readonly LibraryTrackViewModel viewModel;

    public LibraryTrackSelectorPage(LibraryTrackViewModel vm)
    {
        InitializeComponent();
        viewModel = vm;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
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