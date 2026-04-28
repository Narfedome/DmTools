using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
    private readonly LibrarySpellViewModel viewModel;

    public LibrarySpellPage(LibrarySpellViewModel vm)
    {
        InitializeComponent();
        
        viewModel = vm;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LibrarySpellViewModel vm)
            await vm.InitializeAsync();
    }
    
}