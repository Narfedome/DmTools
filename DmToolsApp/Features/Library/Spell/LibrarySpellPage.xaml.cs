using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
    private readonly LibraryTrackViewModel viewModel;

    public LibrarySpellPage(LibraryTrackViewModel vm)
    {
        InitializeComponent();
        
        viewModel = vm;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LibraryTrackViewModel vm)
            await vm.InitializeAsync();
    }
    
}