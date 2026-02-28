using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
    private readonly LibraryViewModel viewModel;

    public LibrarySpellPage(LibraryViewModel vm)
    {
        InitializeComponent();

        vm.CurrentLibraryType = typeof(Spell);

        viewModel = vm;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LibraryViewModel vm)
            await vm.InitializeAsync();
    }
    
}