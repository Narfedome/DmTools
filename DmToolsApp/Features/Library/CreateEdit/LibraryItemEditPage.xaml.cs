namespace DmToolsApp.Features.Library;

public partial class LibraryItemEditPage : ContentPage
{
	public LibraryItemEditPage(LibraryItemEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}