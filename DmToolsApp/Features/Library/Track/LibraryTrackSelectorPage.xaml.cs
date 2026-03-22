using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library;

public partial class LibraryTrackSelectorPage : ContentPage
{
    private readonly LibraryTrackViewModel viewModel;

    public Type? LibraryType { get; set; }
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
}