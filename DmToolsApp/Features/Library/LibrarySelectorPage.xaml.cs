using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library;

public partial class LibrarySelectorPage : ContentPage
{
    private readonly LibraryViewModel viewModel;

    public Type? LibraryType { get; set; }
    public LibrarySelectorPage(LibraryViewModel vm)
    {
        InitializeComponent();
        viewModel = vm;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (LibraryType != null)
            viewModel.CurrentLibraryType = LibraryType;

        await viewModel.InitializeAsync();
    }
}