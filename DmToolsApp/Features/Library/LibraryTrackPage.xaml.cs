namespace DmToolsApp.Features.Library;

public partial class LibraryTrackPage : ContentPage
{
	public LibraryTrackPage(LibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}