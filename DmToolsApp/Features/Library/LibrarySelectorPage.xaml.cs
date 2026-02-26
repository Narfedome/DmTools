using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library;

public partial class LibrarySelectorPage : ContentPage
{
    public LibrarySelectorPage(LibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}