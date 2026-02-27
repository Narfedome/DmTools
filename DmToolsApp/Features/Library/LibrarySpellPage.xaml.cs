namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
	public LibrarySpellPage(LibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}