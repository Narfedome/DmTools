namespace DmToolsApp.Features.Library;

public partial class LibraryPage : ContentPage
{
    public LibraryPage(LibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}