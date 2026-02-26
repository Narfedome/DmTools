using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library;

public partial class LibrarySelectorPage : ContentPage
{
    public LibrarySelectorPage(TaskCompletionSource<LibraryItem?> tcs)
    {
        InitializeComponent();

        var navigation = new LibraryPickerNavigationService(tcs);

        BindingContext = new LibraryViewModel(navigation);
    }
}