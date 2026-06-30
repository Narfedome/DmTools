namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
    public LibrarySpellPage(LibrarySpellViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LibrarySpellViewModel vm)
            await vm.InitializeAsync();
    }
}